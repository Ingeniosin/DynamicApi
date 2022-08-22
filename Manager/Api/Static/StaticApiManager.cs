using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Static;

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
                
                /*var validationResults = newInstance.Validate();
                if (validationResults.Any()) throw new CustomValidationException(validationResults);*/
                
                return await service.OnQuery(newInstance, httpContext);
            });
        });
        
        Console.WriteLine("Auto created route: /"+Route);
    }

    public Type GetServiceType() => typeof(TService);
    public bool IsScoped { get; set; }
}