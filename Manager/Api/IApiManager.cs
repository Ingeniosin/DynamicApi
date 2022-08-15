namespace DynamicApi.Manager.Api;

public interface IApiManager{
    
    public string Route { get; set; }
    
    public void Init(WebApplication app);
    
    public Type GetServiceType();
}