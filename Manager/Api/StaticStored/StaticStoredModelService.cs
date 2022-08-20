using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DynamicApi.Manager.Api.StaticStored; 

public abstract class StaticStoredModelService<T> {

    public abstract Task<List<T>> GetDataSource(DataSourceLoadOptions dataSourceLoadOptions);
    
    public virtual Task OnPost(T entity, HttpContext httpContext) => Task.CompletedTask;
    
    public virtual Task OnPut(T entity, T prevObj, HttpContext httpContext) => Task.CompletedTask;
    
    public virtual Task OnDelete(T entity, HttpContext httpContext) => Task.CompletedTask;

    public int GetValue(DataSourceLoadOptions dataSourceLoadOptions, string key) {
        var filter = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(dataSourceLoadOptions.Filter));
        return GetValue(filter, key);
    }
    
    private static int GetValue(List<object> list, string key) {
        foreach (var item in list) {
            switch (item) {
                case JArray objects:
                    return GetValue(objects.Select(x => x as object).ToList(), key);
                case string or JValue when item.ToString() == key:
                    return int.Parse(list[2].ToString() ?? "0");
            }
        }
        return 0;
    }
    
}