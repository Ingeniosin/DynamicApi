using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Siian_Office_V2.Manager.Api.Static;

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

public class StaticApiManager<TIn, TService> : IApiManager where TIn : class where TService  : StaticModelService<TIn> {
    public string Route{ get; set; }

    public StaticApiManager(string route){
        Route = "/api/"+route;
    }

    public void Init(WebApplication app){
        app.MapPost(Route, async (HttpContext httpContext, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var values = httpContext.Request.Form["values"];
                var newInstance = service.GetInstance();
                JsonConvert.PopulateObject(values, newInstance);
                return await service.OnQuery(newInstance, httpContext);
            });
        });
    }

    public Type GetServiceType() => typeof(TService);
}