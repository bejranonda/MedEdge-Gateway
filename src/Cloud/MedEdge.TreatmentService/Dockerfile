FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/Shared/MedEdge.Core/MedEdge.Core.csproj", "src/Shared/MedEdge.Core/"]
COPY ["src/Cloud/MedEdge.FhirApi/MedEdge.FhirApi.csproj", "src/Cloud/MedEdge.FhirApi/"]
COPY ["src/Cloud/MedEdge.TreatmentService/MedEdge.TreatmentService.csproj", "src/Cloud/MedEdge.TreatmentService/"]

RUN dotnet restore "src/Cloud/MedEdge.TreatmentService/MedEdge.TreatmentService.csproj"

COPY . .

WORKDIR "/src/src/Cloud/MedEdge.TreatmentService"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MedEdge.TreatmentService.dll"]
