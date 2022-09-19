using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Managers.Service;

public interface IServiceModel {

    public static string ZeroId = JsonConvert.SerializeObject( new Dictionary<string, object>{ {"id", 0} });

    
    Task<Func<Task>> Handle(object model, EntityState state, Query query, DynamicDbContext context);
    ServiceConfiguration Configuration { get; }

}

public abstract class ServiceModel<T> : IServiceModel where T : class {

    public virtual Task OnGet(T model, Query query) => Task.CompletedTask;
    
    public virtual Task OnCreated(T archivo, Query query) => Task.CompletedTask;
    public virtual Task<bool> OnCreating(T model, Query query) => Task.FromResult(true);
    

    public virtual Task OnUpdated(T model, T oldModel, Query query) => Task.CompletedTask;
    public virtual Task<bool> OnUpdating(T model, T oldModel, Query query) => Task.FromResult(true);
    
    public virtual Task OnDeleted(T model, Query query) => Task.CompletedTask;
    public virtual Task<bool> OnDeleting(T model, Query query) => Task.FromResult(true);


    public async Task<Func<Task>> Handle(object model, EntityState state, Query query, DynamicDbContext context) {
        var typpedModel = (T)model;
        switch (state) {
            case EntityState.Added: {
                if(!Configuration.OnCreating) return null;
                var onCreating = await OnCreating(typpedModel, query);
                if(!onCreating || !Configuration.OnCreated) return null;
                return async () => {
                    await OnCreated(typpedModel, query);
                };
            }
            case EntityState.Modified: {
                if(!Configuration.OnUpdating) return null;
                var oldModel = context.Entry(model).OriginalValues.ToObject() as T;
                JsonConvert.PopulateObject(IServiceModel.ZeroId, oldModel!);
                await context.AddAsync(oldModel);
                var onUpdating = await OnUpdating(typpedModel, oldModel, query);
                context.Entry(oldModel).State = EntityState.Detached;
                if(!onUpdating || !Configuration.OnUpdated) return null;
                return async () => {
                    await context.AddAsync(oldModel);
                    await OnUpdated(typpedModel, oldModel, query);
                    context.Entry(oldModel).State = EntityState.Detached;
                    context.Update(model);
                    await context.SaveChangesWithOutHandle();
                };
            }
            case EntityState.Deleted: {
                if(!Configuration.OnDeleting) return null;
                var onDeleting = await OnDeleting(typpedModel, query);
                if(!onDeleting || !Configuration.OnDeleted) return null;
                return async () => {
                    await OnDeleted(typpedModel, query);
                };
            }
            default:
                return null;
        }
    }

    public abstract ServiceConfiguration Configuration { get; }
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
