namespace DynamicApi.Manager.Api.Grouped;

public class GroupedStaticApiManager : IApiManager{
    
    private List<IApiManager> _apiManagers;
    public string Route{ get; set; }

    public GroupedStaticApiManager(string route, List<IApiManager> apiManagers){
        Route = route;
        _apiManagers = apiManagers;
    }

    public void Init(WebApplication app){
        _apiManagers.ForEach(x => {
            x.Route = x.Route.Replace("/api/", $"/api/{Route}/");
            x.Init(app);
        });
    }

    public Type GetServiceType() => throw new NotImplementedException();
    
    public List<Type> GetServiceTypes(){
        return _apiManagers.Select(x => x.GetServiceType()).ToList();
    }
}