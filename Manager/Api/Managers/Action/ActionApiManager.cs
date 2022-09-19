using DynamicApi.Manager.Api.Routes;

namespace DynamicApi.Manager.Api.Managers.Action;

public class ActionApiManager<TIn, TService> : IApiManager where TIn : class where TService  : ActionService<TIn> {
    public string Route{ get; set; }
    public bool IsService => false;

    public ActionApiManager(string route){
        Route = "/api/"+route;
    }

    public void Init(WebApplication app){
        var actionApiRoutes = new ActionApiRoutes<TIn, TService>();
        app.MapPost(Route, actionApiRoutes.Post);
    }

    public Type GetServiceType() => typeof(TService);
    public Type GetModelType() => null;

    public bool IsScoped { get; set; }
}