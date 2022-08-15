namespace Siian_Office_V2.Manager.Api;

public interface IApiManager{
    
    public string Route { get; set; }
    
    public void Init(WebApplication app);
    
    public Type GetServiceType();
}