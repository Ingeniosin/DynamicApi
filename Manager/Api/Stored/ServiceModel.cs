namespace DynamicApi.Manager.Api.Stored; 

public abstract class ServiceModel<T> {

    public virtual Task OnGet(T model, Query query) => Task.CompletedTask;
    
    public virtual Task OnCreated(T archivo, Query query) => Task.CompletedTask;
    public virtual Task<bool> OnCreating(T model, Query query) => Task.FromResult(true);
    

    public virtual Task OnUpdated(T model, T oldModel, Query query) => Task.CompletedTask;
    public virtual Task<bool> OnUpdating(T model, T oldModel, Query query) => Task.FromResult(true);
    
    public virtual Task OnDeleted(T archivo, Query query) => Task.CompletedTask;
    public virtual Task<bool> OnDeleting(T model, Query query) => Task.FromResult(true);

}



public class Query {
    
    public DataSourceLoadOptions LoadOptions { get; set; }
    public HttpContext Context { get; set; }

    public Query(DataSourceLoadOptions loadOptions, HttpContext context) {
        LoadOptions = loadOptions;
        Context = context;
    }

    public IFormFileCollection GetFiles() => Context.Request.Form.Files;

}
