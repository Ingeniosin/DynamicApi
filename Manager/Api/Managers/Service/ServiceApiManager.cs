using DynamicApi.Manager.Api.Routes;
using Microsoft.EntityFrameworkCore;

namespace DynamicApi.Manager.Api.Managers.Service; 

public class ServiceApiManager<T, TService, TDbContext> : IApiManager  where T : class where TService : ServiceModel<T> where TDbContext : DynamicDbContext {

    public string Route { get; set; }
    public bool IsService => true;
    public ManagerConfiguration Configuration { get; set; }
    private Func<TDbContext, DbSet<T>> DbSetReference { get; }
    private Func<TDbContext, T> NewInstanceReference { get; }
    
    public ServiceApiManager(string route, Func<TDbContext, DbSet<T>> dbSetReference, ManagerConfiguration configuration = null) {
        Route = "/api/"+route;
        Configuration = configuration ?? new ManagerConfiguration();
        DbSetReference = dbSetReference;
        NewInstanceReference = x => DbSetReference(x).CreateProxy();
    }
    
    public void Init(WebApplication app) {
        using var scope = DynamicApi.ServiceProvider.CreateScope();
        var serviceTemp = (ServiceModel<T>) scope.ServiceProvider.GetService(GetServiceType());
        if(serviceTemp == null) {
            throw new Exception("Service not found...");
        }
        
        var serviceConfiguration = serviceTemp.Configuration;
        
        var service = new ServiceApiRoutes<T, TService, TDbContext>(DbSetReference, NewInstanceReference, serviceConfiguration);
        var nonService = new NonServiceApiRoutes<T, TDbContext>(DbSetReference, NewInstanceReference);
        
        if(Configuration.AllowGetRute) {
            app.MapGet(Route, serviceConfiguration.OnGet ? service.Get : nonService.Get);
        }

        if(Configuration.AllowPostRute) {
            var postService = serviceConfiguration.OnCreating || serviceConfiguration.OnCreated;
            app.MapPost(Route, postService ? service.Post : nonService.Post);
        }

        if(Configuration.AllowPutRute) {
            var putService = serviceConfiguration.OnUpdating || serviceConfiguration.OnUpdated;
            app.MapPut(Route, putService ? service.Put : nonService.Put);
        }
        
        if(Configuration.AllowDeleteRute) {
            var deleteService = serviceConfiguration.OnDeleting || serviceConfiguration.OnDeleted;
            app.MapDelete(Route, deleteService ? service.Delete : nonService.Delete);
        }
    }

    public Type GetServiceType() => typeof(TService);
    public Type GetModelType() => typeof(T);

    public bool IsScoped { get; set; }
}

public class ServiceConfiguration {
    
    public bool OnGet { get; set; }
    public bool OnCreated { get; set; }
    public bool OnCreating { get; set; }
    public bool OnUpdated { get; set; }
    public bool OnUpdating { get; set; }
    public bool OnDeleted { get; set; }
    public bool OnDeleting { get; set; }
    
}


public class ManagerConfiguration {

    public bool AllowGetRute { get; set; } = true;
    public bool AllowPostRute { get; set; } = true;
    public bool AllowPutRute { get; set; } = true;
    public bool AllowDeleteRute { get; set; } = true;

}