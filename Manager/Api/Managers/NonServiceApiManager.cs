using DynamicApi.Manager.Api.Managers.Service;
using DynamicApi.Manager.Api.Routes;
using Microsoft.EntityFrameworkCore;

namespace DynamicApi.Manager.Api.Managers;

public class NonServiceApiManager<T, TDbContext> : IApiManager where T : class where TDbContext : DynamicDbContext {

    public string Route { get; set; }
    private readonly ManagerConfiguration _configuration;
    private readonly Func<TDbContext, DbSet<T>> _dbSetReference;
    private readonly Func<TDbContext, T> _newInstanceReference;
    public bool IsService => false;

    public NonServiceApiManager(string route, Func<TDbContext, DbSet<T>> dbSetReference, ManagerConfiguration configuration) {
        Route = "/api/"+route;
        _configuration = configuration;
        _dbSetReference = dbSetReference;
        _newInstanceReference = x => _dbSetReference(x).CreateProxy();
    }
    
    public NonServiceApiManager(string route, Func<TDbContext, DbSet<T>> dbSetReference) : this(route, dbSetReference, new ManagerConfiguration()) { }

    
    public void Init(WebApplication app) {
        var nonService = new NonServiceApiRoutes<T, TDbContext>(_dbSetReference, _newInstanceReference);

        if(_configuration.AllowGetRute) {
            app.MapGet(Route, nonService.Get);
        }

        if(_configuration.AllowPostRute) {
            app.MapPost(Route, nonService.Post);
        }

        if(_configuration.AllowPutRute) {
            app.MapPut(Route, nonService.Put);
        }

        if(_configuration.AllowDeleteRute) {
            app.MapDelete(Route, nonService.Delete);
        }
    }

    public Type GetServiceType() => null;
    public Type GetModelType() => typeof(T);

    public bool IsScoped { get; set; }
}