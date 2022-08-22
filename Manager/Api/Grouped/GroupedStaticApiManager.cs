namespace DynamicApi.Manager.Api.Grouped;

public class GroupedStaticApiManager : IApiManager{
    
    public List<IApiManager> ApiManagers { get; set; }
    public string Route{ get; set; }

    public GroupedStaticApiManager(string route, List<IApiManager> apiManagers){
        Route = route;
        ApiManagers = apiManagers;
    }

    public void Init(WebApplication app){
        ApiManagers.ForEach(x => {
            x.Route = x.Route.Replace("/api/", $"/api/{Route}/");
            x.Init(app);
        });
    }

    public Type GetServiceType() => throw new NotImplementedException();
    public bool IsScoped { get; set; }

    public List<Type> GetServiceTypes(){
        return ApiManagers.Select(x => x.GetServiceType()).ToList();
    }
}