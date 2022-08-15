namespace Siian_Office_V2.Manager.Api.Static;

public abstract class StaticModelService<T> where T : class {

    public virtual Task<object> OnQuery(T model){
        return null;
    }

    public virtual Task<object> OnQuery(T model, HttpContext httpContext){
        return OnQuery(model);
    }

    public abstract T GetInstance();
    
}