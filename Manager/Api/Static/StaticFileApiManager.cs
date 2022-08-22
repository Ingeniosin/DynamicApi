using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Static;

public class StaticFileApiManager<TIn, TService> : IApiManager where TIn : class where TService  : StaticModelService<TIn> {
    public string Route{ get; set; }

    public StaticFileApiManager(string route){
        Route = "/api/"+route;
    }

    public void Init(WebApplication app){
        app.MapPost(Route, async (HttpContext httpContext, [FromServices] TService service) => {
            
            var values = httpContext.Request.Form["values"];
            var newInstance = service.GetInstance();
            JsonConvert.PopulateObject(values, newInstance);

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
        
        Console.WriteLine("Auto created route: /"+Route);
    }

    public Type GetServiceType() => typeof(TService);
    public bool IsScoped { get; set; }
}