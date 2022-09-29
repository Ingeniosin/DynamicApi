using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Managers.Service;

public interface IServiceModel {

    public static string ZeroId = JsonConvert.SerializeObject( new Dictionary<string, object>{ {"id", 0} });

    Task<Func<Task>> Handle(object model, EntityState state, Query query, DynamicDbContext context);

    ServiceConfiguration Configuration { get; }

}

//where t is class or interface
public abstract class ServiceModel<T, TDbContext> : IServiceModel  where T : class where TDbContext : DynamicDbContext {

    public virtual Task OnGet(T model, Query query, TDbContext db) => Task.CompletedTask;
    
    public virtual Task OnCreated(T archivo, Query query, TDbContext db) => Task.CompletedTask;
    public virtual Task<bool> OnCreating(T model, Query query, TDbContext db) => Task.FromResult(true);
    

    // NO SE NECESITA GUARDAR
    public virtual Task<bool> OnUpdated(T model, Func<bool, Task<T>> getOldModel, Query query, TDbContext db) => Task.FromResult(false); //true si se desea guardar el modelo, await context.AddAsync(oldModel);,  entryOldModel.State = EntityState.Detached;
    public virtual Task<bool> OnUpdating(T model, Func<bool, Task<T>> getOldModel, Query query, TDbContext db) => Task.FromResult(true); //true si se desea continuar con OnUpdated si hubiera
    
    public virtual Task OnDeleted(T model, Query query, TDbContext db) => Task.CompletedTask;
    public virtual Task<bool> OnDeleting(T model, Query query, TDbContext db) => Task.FromResult(true);


    public async Task<Func<Task>> Handle(object model, EntityState state, Query query, DynamicDbContext context) {
        var typpedModel = (T)model;
        var dynamicDbContext = (TDbContext)context;
        switch (state) {
            case EntityState.Added: {
                if(!Configuration.OnCreated && !Configuration.OnCreating) return null;
                var onCreating = Configuration.OnCreating && await OnCreating(typpedModel, query, dynamicDbContext);
                if((onCreating || !Configuration.OnCreating) && Configuration.OnCreated) {
                    return async () => await OnCreated(typpedModel, query,  dynamicDbContext);
                }
                return null;
            }
            case EntityState.Modified: {
                if(!Configuration.OnUpdating && !Configuration.OnUpdated) return null;
                var onPreSave = () => {};

                async Task<T> GetOldModel(bool withRelations) {
                    var oldModel = context.Entry(model).OriginalValues.ToObject() as T ;
                    JsonConvert.PopulateObject(IServiceModel.ZeroId, oldModel!);
                    await context.AddAsync(oldModel);
                    var entry = context.Entry(oldModel);
                    onPreSave = () => entry.State = EntityState.Detached;
                    return oldModel;
                }

                var onUpdating = Configuration.OnUpdating && await OnUpdating(typpedModel, GetOldModel, query,  dynamicDbContext);
                onPreSave();

                if((onUpdating || !Configuration.OnUpdating) && Configuration.OnUpdated) {
                    var entry = context.Entry(typpedModel);
                    return async () => {
                        entry.State = EntityState.Detached;
                        context.Update(typpedModel);
                        var saveOnUpdated = await OnUpdated(typpedModel, GetOldModel, query,  dynamicDbContext);
                        if(saveOnUpdated) {
                            onPreSave();
                            await context.SaveChangesWithOutHandle();
                        }
                    };
                }
                return null;
            }
            case EntityState.Deleted: {
                if(!Configuration.OnDeleting && !Configuration.OnDeleted) return null;
                var onDeleting = Configuration.OnDeleting && await OnDeleting(typpedModel, query,  dynamicDbContext);
                if((onDeleting || !Configuration.OnDeleting) && Configuration.OnDeleted) {
                    return async () => await OnDeleted(typpedModel, query,  dynamicDbContext);
                }
                return null;
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
