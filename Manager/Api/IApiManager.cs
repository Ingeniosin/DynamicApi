namespace DynamicApi.Manager.Api;

public interface IApiManager{
    
    public string Route { get; set; }
    public bool IsService { get; }
    
    public void Init(WebApplication app);
    
    public Type GetServiceType();
    public Type GetModelType();

    public bool IsScoped { get; set; }
}