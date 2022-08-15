namespace Siian_Office_V2.Manager;

public interface IBusiness<T> where T : class{

    public bool IsValid(T obj);

    public Task OnCreating(T obj);
    public Task OnCreated(T obj);
    
    public Task OnUpdating(T obj, T prevObj);
    public Task OnUpdated(T obj, T prevObj);
    
    public Task OnDeleting(T obj);
    public Task OnDeleted(T obj);
    
    public Task OnFetching(T obj);
    public Task OnFetched(T obj);
    
    public Task OnFetchedAll(List<T> obj);

}