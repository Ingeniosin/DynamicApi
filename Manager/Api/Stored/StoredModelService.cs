namespace DynamicApi.Manager.Api.Stored;

public  class StoredModelService<T> : IBusiness<T> where T : class{
    
    public  static StoredModelService<T> Get<T>() where T : class => new();

    public  virtual bool IsValid(T obj) => true;

    public  virtual async Task<T> OnCreating(T obj) => null;

    public  virtual Task<T>  OnCreating(T obj, HttpContext context) => OnCreating(obj);

    
    public  virtual Task<T> OnCreated(T obj) => Task.FromResult(obj);

    public  virtual Task<T> OnCreated(T obj, HttpContext context) => OnCreated(obj);

    public  virtual Task OnUpdating(T obj, T prevObj) => Task.CompletedTask;

    public  virtual async Task<T>  OnUpdated(T obj, T prevObj) => null;

    public  virtual Task OnDeleting(T obj) => Task.CompletedTask;

    public  virtual Task OnDeleted(T obj) => Task.CompletedTask;

    public  virtual Task OnFetching(T obj) => Task.CompletedTask;

    public  virtual Task OnFetched(T obj) => Task.CompletedTask;

    public  virtual Task OnFetchedAll(List<T> obj) => Task.CompletedTask;
}