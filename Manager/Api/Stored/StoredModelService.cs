namespace Siian_Office_V2.Manager.Api.Stored;

public  class StoredModelService<T> : IBusiness<T> where T : class{
    public  static StoredModelService<T> Get<T>() where T : class => new();

    public  virtual bool IsValid(T obj) => true;

    public  virtual Task OnCreating(T obj) => Task.CompletedTask;

    public  virtual Task OnCreated(T obj) => Task.CompletedTask;

    public  virtual Task OnUpdating(T obj, T prevObj) => Task.CompletedTask;

    public  virtual Task OnUpdated(T obj, T prevObj) => Task.CompletedTask;

    public  virtual Task OnDeleting(T obj) => Task.CompletedTask;

    public  virtual Task OnDeleted(T obj) => Task.CompletedTask;

    public  virtual Task OnFetching(T obj) => Task.CompletedTask;

    public  virtual Task OnFetched(T obj) => Task.CompletedTask;

    public  virtual Task OnFetchedAll(List<T> obj) => Task.CompletedTask;
}