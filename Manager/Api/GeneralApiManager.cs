using DynamicApi.Manager.Api.Managers.Service;

namespace DynamicApi.Manager.Api; 

public class GeneralApiManager<T, TService, TDbContext> : IApiManager  where T : class where TService : ServiceModel<T, TDbContext> where TDbContext : DynamicDbContext {

    public string Route { get; set; }
    public bool IsService => true;
    
    public void Init(WebApplication app) {
    }

    public Type GetServiceType() => typeof(TService);

    public Type GetModelType() => typeof(T);

    public bool IsScoped { get; set; }
}