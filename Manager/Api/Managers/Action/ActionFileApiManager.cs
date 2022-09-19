using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Managers.Action;

public class ActionFileApiManager<TIn, TService> : IApiManager where TIn : class where TService  : ActionService<TIn> {
    public string Route{ get; set; }
    public bool IsService => false;

    public ActionFileApiManager(string route){
        Route = "/api/"+route;
    }

    public void Init(WebApplication app){
        app.MapPost(Route, async (HttpContext httpContext, [FromServices] TService service) => {
            var values = httpContext.Request.Form["values"];
            var newInstance = service.GetInstance();
            JsonConvert.PopulateObject(values, newInstance, ApiUtils.PostOrPutSettings);
            var file = (FileInfo)await service.OnQuery(newInstance, httpContext);
            if(!file.Exists)
                throw new FileNotFoundException("File not found");
            var stream = file.OpenRead();
            var fileName = file.Name;
            if(file.Name[3] == ' ') {
                fileName = file.Name[4..];
            }
            return Results.File(stream, "application/force-download", fileName, file.LastWriteTime);
        });
    }

    public Type GetServiceType() => typeof(TService);
    public Type GetModelType() => null;

    public bool IsScoped { get; set; }
}