using Microsoft.EntityFrameworkCore;

namespace DynamicApi.Manager;

public interface IGlobalService {

    public Task Execute(object model, EntityState state);
    
    public Type GetServiceType();

}

public abstract class GlobalService<T>  : IGlobalService where T : class {

    public virtual async Task OnCreating(T entity){}
    public virtual async Task OnUpdating(T entity){}
    public virtual async Task OnDeleting(T entity){}

    public async Task Execute(object model, EntityState state) {
        var castedModel = (T) model;
        switch (state) {
            case EntityState.Added:
                await OnCreating(castedModel);
                break;
            case EntityState.Modified:
                await OnUpdating(castedModel);
                break;
            case EntityState.Deleted:
                await OnDeleting(castedModel);
                break;
            case EntityState.Detached:
                break;
            case EntityState.Unchanged:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    public Type GetServiceType() => GetType();
}