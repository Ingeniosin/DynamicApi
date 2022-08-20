using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.StaticStored; 

public class StaticStoredApiManager<T, TService> : IApiManager where T : StaticStoredModel where TService : StaticStoredModelService<T>  {

    public string Route { get; set; }
    private readonly Func<T> _createModel = () => (T)Activator.CreateInstance(typeof(T));
    private readonly Dictionary<int, T> _modelCache = new();

    public StaticStoredApiManager(string route){
        Route = "/api/"+route;
    }
    
    public void Init(WebApplication app) {
        app.MapGet(Route, async (DataSourceLoadOptions dataSourceLoadOptions, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var dataSource = await service.GetDataSource(dataSourceLoadOptions);
                dataSource.ForEach(x => {
                    x.Id = _modelCache.Count + 1;
                    _modelCache.Add(x.Id, x);
                });
                var loadResult = DataSourceLoader.Load(dataSource, dataSourceLoadOptions);
                return loadResult;
            });
        });
        
        app.MapPost(Route, async (HttpContext httpContext, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var newInstance = _createModel();
                var values = httpContext.Request.Form["values"];
                JsonConvert.PopulateObject(values, newInstance);
                
                var validationResults = newInstance.Validate();
                if (validationResults.Any()) throw new CustomValidationException(validationResults);
                
                await service.OnPost(newInstance, httpContext);
                return newInstance;
            });
        });
        
        app.MapPut(Route, async (HttpContext httpContext, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var key = int.Parse(httpContext.Request.Form["key"].ToString().Replace("\"", ""));
                var values = httpContext.Request.Form["values"];
                var instance = _modelCache[key];
                if(instance == null)
                    throw new Exception("No se encontro el registro...");
                var prevObj = _createModel();
                JsonConvert.PopulateObject(JsonConvert.SerializeObject(instance), prevObj);
                JsonConvert.PopulateObject(values, instance);
                
                var validationResults = instance.Validate();
                if (validationResults.Any()) throw new CustomValidationException(validationResults);
                
                await service.OnPut(instance, prevObj, httpContext);
                _modelCache[key] = instance;
                return instance;
            });
        });
        
        app.MapDelete(Route, async (HttpContext httpContext,[FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var key = int.Parse(httpContext.Request.Form["key"].ToString().Replace("\"", ""));
                var instance = _modelCache[key];
                if(instance == null)
                    throw new Exception("Serializable not found");
                await service.OnDelete(instance, httpContext);
                _modelCache.Remove(key);
                return instance;
            });
        });
        
    }

    public Type GetServiceType() => typeof(TService);
}