using DynamicApi.Manager.Api.Managers.Action;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Routes; 

public class ActionApiRoutes<TIn, TService>  where TIn : class where TService : ActionService<TIn> {
    
    public async Task<IResult> Post(HttpContext httpContext, [FromServices] TService service) {
        return await ApiUtils.Result(async () => {
            var values = httpContext.Request.Form["values"];
            var newInstance = service.GetInstance();
            JsonConvert.PopulateObject(values, newInstance, ApiUtils.PostOrPutSettings);
            return await service.OnQuery(newInstance, httpContext);
        }, ApiUtils.PostOrPutSettings);
    }
    
}