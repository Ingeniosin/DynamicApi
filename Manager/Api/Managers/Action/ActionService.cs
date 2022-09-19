namespace DynamicApi.Manager.Api.Managers.Action;

public abstract class ActionService<T> where T : class {

    public virtual Task<object> OnQuery(T model){
        return null;
    }

    public virtual Task<object> OnQuery(T model, HttpContext httpContext){
        return OnQuery(model);
    }

    public abstract T GetInstance();
    
}