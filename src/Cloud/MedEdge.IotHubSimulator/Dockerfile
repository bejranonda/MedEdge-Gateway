# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Cloud/MedEdge.IotHubSimulator/MedEdge.IotHubSimulator.csproj", "src/Cloud/MedEdge.IotHubSimulator/"]
COPY ["src/Shared/MedEdge.Core/MedEdge.Core.csproj", "src/Shared/MedEdge.Core/"]
RUN dotnet restore "src/Cloud/MedEdge.IotHubSimulator/MedEdge.IotHubSimulator.csproj"

COPY . .
WORKDIR "/src/src/Cloud/MedEdge.IotHubSimulator"
RUN dotnet build "MedEdge.IotHubSimulator.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "MedEdge.IotHubSimulator.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MedEdge.IotHubSimulator.dll"]
