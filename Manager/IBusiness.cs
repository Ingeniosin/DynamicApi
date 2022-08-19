namespace DynamicApi.Manager;

public interface IBusiness<T> where T : class{

    public bool IsValid(T obj);

    public Task<T> OnCreating(T obj);
    public Task<T> OnCreated(T obj);
    
    public Task OnUpdating(T obj, T prevObj);
    public Task<T> OnUpdated(T obj, T prevObj);
    
    public Task OnDeleting(T obj);
    public Task OnDeleted(T obj);
    
    public Task OnFetching(T obj);
    public Task OnFetched(T obj);
    
    public Task OnFetchedAll(List<T> obj);

}