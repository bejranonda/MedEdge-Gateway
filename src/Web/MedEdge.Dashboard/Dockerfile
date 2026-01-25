# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["Directory.Build.props", "./"]
COPY ["src/Shared/MedEdge.Core/MedEdge.Core.csproj", "Shared/MedEdge.Core/"]
COPY ["src/Web/MedEdge.Dashboard/MedEdge.Dashboard.csproj", "Web/MedEdge.Dashboard/"]

# Restore dependencies
RUN dotnet restore "Web/MedEdge.Dashboard/MedEdge.Dashboard.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/Web/MedEdge.Dashboard"
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Copy MudBlazor static assets to the correct location
RUN mkdir -p /app/publish/wwwroot/_content/MudBlazor && \
    find /root/.nuget/packages/mudblazor.* -name "MudBlazor.min.css" 2>/dev/null | head -1 | xargs -I {} cp {} /app/publish/wwwroot/_content/MudBlazor/ || \
    find /app/publish -name "MudBlazor.min.css" 2>/dev/null | head -1 | xargs -I {} cp {} /app/publish/wwwroot/_content/MudBlazor/ || \
    echo "Warning: Could not find MudBlazor.min.css"

# Runtime stage with Nginx
FROM nginx:alpine AS final
WORKDIR /app

# Copy Nginx configuration
COPY src/Web/MedEdge.Dashboard/nginx.conf /etc/nginx/nginx.conf

# Copy published Blazor WASM app
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html

# Create environment configuration file from build args
ARG DASHBOARD_USERNAME=guest
ARG DASHBOARD_PASSWORD=changeme
RUN echo "window.MedEdgeConfig = {" > /usr/share/nginx/html/config-env.js && \
    echo "  dashboardUsername: '${DASHBOARD_USERNAME}'," >> /usr/share/nginx/html/config-env.js && \
    echo "  dashboardPassword: '${DASHBOARD_PASSWORD}'" >> /usr/share/nginx/html/config-env.js && \
    echo "};" >> /usr/share/nginx/html/config-env.js

# Expose port
EXPOSE 8080

# Start Nginx
CMD ["nginx", "-g", "daemon off;"]
