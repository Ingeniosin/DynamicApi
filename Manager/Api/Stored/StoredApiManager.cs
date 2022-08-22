using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Stored; 

public class StoredApiManager<T, TService, TDbContext> : IApiManager  where T : class where TService : ServiceModel<T> where TDbContext : DbContext {

    public string Route { get; set; }
    public ManagerConfiguration Configuration { get; set; }
    private Func<TDbContext, DbSet<T>> DbSetReference { get; }
    private Func<TDbContext, T> NewInstanceReference { get; }
    public ServiceConfiguration ServiceConfiguration { get; set; }
    
    public StoredApiManager(string route, ManagerConfiguration configuration, Func<TDbContext, DbSet<T>> dbSetReference, ServiceConfiguration serviceConfiguration) {
        Route = "/api/"+route;
        Configuration = configuration;
        DbSetReference = dbSetReference;
        ServiceConfiguration = serviceConfiguration;
        NewInstanceReference = x => DbSetReference(x).CreateProxy();
    }
    
    public StoredApiManager(string route, Func<TDbContext, DbSet<T>> dbSetReference, ServiceConfiguration serviceConfiguration) : this(route, new ManagerConfiguration(), dbSetReference, serviceConfiguration) { }

    public void Init(WebApplication app) {

        if(Configuration.AllowGetRute) {

            if(ServiceConfiguration.OnGet) {
                app.MapGet(Route, async (DataSourceLoadOptions dataSourceLoadOptions, HttpContext context, TDbContext db, [FromServices] TService service) => {
                    return await ApiUtils.Result(async () => {
                        var dbSet = DbSetReference(db);
                        var loadResult = await DataSourceLoader.LoadAsync(dbSet, dataSourceLoadOptions);
                        var isValid = ServiceConfiguration.OnGet && loadResult.data is IEnumerable<T>;
                        if(isValid) {
                            var loadResultData = loadResult.data as IEnumerable<T>;
                            var query = new Query(dataSourceLoadOptions, context);
                            foreach (var model in loadResultData!) {
                                await service.OnGet(model, query);
                                dbSet.Update(model);
                            }
                            await db.SaveChangesAsync();
                        }
                        return loadResult;
                    });
                });
            } else {
                app.MapGet(Route, async (DataSourceLoadOptions dataSourceLoadOptions, TDbContext db) => {
                    return await ApiUtils.Result(async () => await DataSourceLoader.LoadAsync(DbSetReference(db), dataSourceLoadOptions));
                });
            }
            
           
        }

        if(Configuration.AllowPostRute) {
            var postService = ServiceConfiguration.OnCreating || ServiceConfiguration.OnCreated;
            if(postService) {
                app.MapPost(Route, async (HttpContext context, TDbContext db, [FromServices] TService service) => {
                    return await ApiUtils.Result(async () => {
                        var model = NewInstanceReference(db);
                        var dbSet = DbSetReference(db);
                        var values = context.Request.Form["values"];
                        JsonConvert.PopulateObject(values, model);
                        await dbSet.AddAsync(model);
                        var query = new Query(null, context);

                        if(ServiceConfiguration.OnCreating) 
                            await service.OnCreating(model, query);

                        await db.SaveChangesAsync();
                        if(ServiceConfiguration.OnCreated) {
                            await service.OnCreated(model, query);
                            db.Update(model);
                            await db.SaveChangesAsync();
                        }
                        return true;
                    });
                });
            } else {
                app.MapPost(Route, async (HttpContext context, TDbContext db) => {
                    return await ApiUtils.Result(async () => {
                        var model = NewInstanceReference(db);
                        var dbSet = DbSetReference(db);
                        var values = context.Request.Form["values"];
                        JsonConvert.PopulateObject(values, model);
                        await dbSet.AddAsync(model);
                        await db.SaveChangesAsync();
                        return true;
                    });
                });
            }
          
        }

        if(Configuration.AllowPutRute) {
            var putService = ServiceConfiguration.OnUpdating || ServiceConfiguration.OnUpdated;
            if(putService) {
                app.MapPut(Route, async (HttpContext context, TDbContext db, [FromServices] TService service) => {
                    return await ApiUtils.Result(async () => {
                        var dbSet = DbSetReference(db);
                        var key = int.Parse(context.Request.Form["key"].ToString().Replace("\"", ""));
                        var values = context.Request.Form["values"];

                        var model = await dbSet.FindAsync(key);
                        if(model == null)
                            throw new Exception("Model not found.");


                        var query = new Query(null, context);
                        var previousModel = db.Entry(model).CurrentValues.ToObject() as T;
                    
                        JsonConvert.PopulateObject(values, model);
                    
                        db.Update(model);
                        if(ServiceConfiguration.OnUpdating) {
                            if(!await service.OnUpdating(model, previousModel, query))
                                return false;
                        }
                    
                        await db.SaveChangesAsync();
                    
                        if(ServiceConfiguration.OnUpdated) {
                            await service.OnUpdated(model, previousModel, query);
                            db.Update(model);
                            await db.SaveChangesAsync();
                        }
                        return true;
                    });
                });
            } else {
                app.MapPut(Route, async (HttpContext context, TDbContext db) => {
                    return await ApiUtils.Result(async () => {
                        var dbSet = DbSetReference(db);
                        var key = int.Parse(context.Request.Form["key"].ToString().Replace("\"", ""));
                        var values = context.Request.Form["values"];
                        var model = await dbSet.FindAsync(key);
                        if(model == null)
                            throw new Exception("Model not found.");
                        JsonConvert.PopulateObject(values, model);
                        db.Update(model);
                        await db.SaveChangesAsync();
                        return true;
                    });
                });
            }
        }
        
        if(Configuration.AllowDeleteRute) {
            var deleteService = ServiceConfiguration.OnDeleting || ServiceConfiguration.OnDeleted;
            if(deleteService) {
                app.MapDelete(Route, async (HttpContext context, TDbContext db, [FromServices] TService service) => {
                    return await ApiUtils.Result(async () => {
                        var dbSet = DbSetReference(db);
                        var key = int.Parse(context.Request.Form["key"].ToString().Replace("\"", ""));
                        var model = await dbSet.FindAsync(key);
                        if(model == null)
                            throw new Exception("Model not found.");

                        var query = new Query(null, context);

                        if(ServiceConfiguration.OnDeleting) {
                            if(!await service.OnDeleting(model, query)) {
                                dbSet.Update(model);
                                await db.SaveChangesAsync();
                                return false;
                            }
                        }
                    
                        dbSet.Remove(model);
                        await db.SaveChangesAsync();

                        if(ServiceConfiguration.OnDeleted) 
                            await service.OnDeleted(model, query);
                        return true;
                    });
                });
            } else {
                app.MapDelete(Route, async (HttpContext context, TDbContext db) => {
                    return await ApiUtils.Result(async () => {
                        var dbSet = DbSetReference(db);
                        var key = int.Parse(context.Request.Form["key"].ToString().Replace("\"", ""));
                        var model = await dbSet.FindAsync(key);
                        if(model == null)
                            throw new Exception("Model not found.");
                        dbSet.Remove(model);
                        await db.SaveChangesAsync();
                        return true;
                    });
                });
            }
        }

    }

    public Type GetServiceType() => typeof(TService);
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