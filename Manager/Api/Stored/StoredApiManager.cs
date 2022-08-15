using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Siian_Office_V2.Models;

namespace Siian_Office_V2.Manager.Api.Stored;

public class StoredApiManager<T, TService> : IApiManager where T : class where TService : StoredModelService<T>{
    private Func<ApplicationDbContext, DbSet<T>> DbSetReference { get; }
    private Func<ApplicationDbContext, T> NewInstanceReference { get; }
    public string Route{ get; set; }

    public StoredApiManager(string route, Func<ApplicationDbContext, DbSet<T>> dbSetReference){
        Route = "/api/"+route;
        DbSetReference = dbSetReference;
        NewInstanceReference = x => DbSetReference(x).CreateProxy();
    }

    public void Init(WebApplication app) {
        app.MapGet(Route, async (DataSourceLoadOptions dataSourceLoadOptions, ApplicationDbContext db, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var loadResult = await DataSourceLoader.LoadAsync(DbSetReference(db), dataSourceLoadOptions, CancellationToken.None);
                try{
                    var loadResultData = (IEnumerable<T>)loadResult.data;
                    await service.OnFetchedAll(loadResultData.ToList());
                } catch (Exception e){ }
                return loadResult;
            });
        });

        app.MapPost(Route, async (HttpContext httpContext, ApplicationDbContext db, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var newInstance = NewInstanceReference(db);
                var values = httpContext.Request.Form["values"];
                JsonConvert.PopulateObject(values, newInstance);
                await service.OnCreating(newInstance);
                var addedEntity = await dbSet.AddAsync(newInstance);
                await db.SaveChangesAsync();
                await service.OnCreated(addedEntity.Entity);
                return addedEntity.Entity;
            });
        });
        
        app.MapPut(Route, async (HttpContext httpContext, ApplicationDbContext db, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var key = int.Parse(httpContext.Request.Form["key"].ToString().Replace("\"", ""));
                var values = httpContext.Request.Form["values"];
                var instance = await dbSet.FindAsync(key);
                if(instance == null)
                    throw new Exception("No se encontro el registro...");
                var prevObj = (T) db.Entry(instance).CurrentValues.ToObject();
                JsonConvert.PopulateObject(values, instance);
                await service.OnUpdating(instance, prevObj);
                await db.SaveChangesAsync();
                await service.OnUpdated(instance, prevObj);
                return instance;
            });
        });
        
        app.MapDelete(Route, async (HttpContext httpContext, ApplicationDbContext db, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var key = int.Parse(httpContext.Request.Form["key"].ToString().Replace("\"", ""));
                var instance = await dbSet.FindAsync(key);
                if(instance == null)
                    throw new Exception("Serializable not found");
                await service.OnDeleting(instance);
                dbSet.Remove(instance);
                await db.SaveChangesAsync();
                await service.OnDeleted(instance);
                return instance;
            });
        });
        Console.WriteLine("Auto created route: /"+Route);
    }

    public Type GetServiceType() => typeof(TService);
}

public class StoredServicelessApiManager<T> : IApiManager where T : class {
    private Func<ApplicationDbContext, DbSet<T>> DbSetReference { get; }
    private Func<ApplicationDbContext, T> NewInstanceReference { get; }
    public string Route{ get; set; }

    public StoredServicelessApiManager(string route, Func<ApplicationDbContext, DbSet<T>> dbSetReference){
        Route = "/api/"+route;
        DbSetReference = dbSetReference;
        NewInstanceReference = x => DbSetReference(x).CreateProxy();
    }

    public void Init(WebApplication app) {
        app.MapGet(Route, async (DataSourceLoadOptions dataSourceLoadOptions, ApplicationDbContext db) => {
            return await ApiUtils.Result(async () => {
                var loadResult = await DataSourceLoader.LoadAsync(DbSetReference(db), dataSourceLoadOptions, CancellationToken.None);
                return loadResult;
            });
        });

        app.MapPost(Route, async (HttpContext httpContext, ApplicationDbContext db) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var newInstance = NewInstanceReference(db);
                var values = httpContext.Request.Form["values"];
                JsonConvert.PopulateObject(values, newInstance);
                var addedEntity = await dbSet.AddAsync(newInstance);
                await db.SaveChangesAsync();
                return addedEntity.Entity;
            });
        });
        
        app.MapPut(Route, async (HttpContext httpContext, ApplicationDbContext db) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var key = int.Parse(httpContext.Request.Form["key"].ToString().Replace("\"", ""));
                var values = httpContext.Request.Form["values"];
                var instance = await dbSet.FindAsync(key);
                if(instance == null)
                    throw new Exception("No se encontro el registro...");
                JsonConvert.PopulateObject(values, instance);
                await db.SaveChangesAsync();
                return instance;
            });
        });
        
        app.MapDelete(Route, async (HttpContext httpContext, ApplicationDbContext db) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var key = int.Parse(httpContext.Request.Form["key"].ToString().Replace("\"", ""));
                var instance = await dbSet.FindAsync(key);
                if(instance == null)
                    throw new Exception("Serializable not found");
                dbSet.Remove(instance);
                await db.SaveChangesAsync();
                return instance;
            });
        });
        Console.WriteLine("Auto created route: /"+Route);
    }

    public Type GetServiceType() => null;
}