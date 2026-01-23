namespace MedEdge.Dashboard.Services;

public interface IAppConfigurationService
{
    AppConfiguration Configuration { get; }
    Task<AppConfiguration> LoadConfigurationAsync();
}