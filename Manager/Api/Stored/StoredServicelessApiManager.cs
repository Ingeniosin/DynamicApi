using System.ComponentModel.DataAnnotations;
using DevExtreme.AspNet.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Stored;

public class StoredServicelessApiManager<T, TDbContext> : IApiManager where T : class where TDbContext : DbContext{
    private Func<TDbContext, DbSet<T>> DbSetReference { get; }
    private Func<TDbContext, T> NewInstanceReference { get; }
    public string Route{ get; set; }

    public StoredServicelessApiManager(string route, Func<TDbContext, DbSet<T>> dbSetReference){
        Route = "/api/"+route;
        DbSetReference = dbSetReference;
        NewInstanceReference = x => DbSetReference(x).CreateProxy();
    }

    public void Init(WebApplication app) {
        app.MapGet(Route, async (DataSourceLoadOptions dataSourceLoadOptions, TDbContext db) => {
            return await ApiUtils.Result(async () => {
                var loadResult = await DataSourceLoader.LoadAsync(DbSetReference(db), dataSourceLoadOptions);
                return loadResult;
            });
        });

        app.MapPost(Route, async (HttpContext httpContext, TDbContext db) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var newInstance = NewInstanceReference(db);
                var values = httpContext.Request.Form["values"];
                JsonConvert.PopulateObject(values, newInstance);

                var validationResults = newInstance.Validate();
                if (validationResults.Any()) throw new CustomValidationException(validationResults);
                

                var addedEntity = await dbSet.AddAsync(newInstance);
                await db.SaveChangesAsync();
                return addedEntity.Entity;
            });
        });
        
        app.MapPut(Route, async (HttpContext httpContext, TDbContext db) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var key = int.Parse(httpContext.Request.Form["key"].ToString().Replace("\"", ""));
                var values = httpContext.Request.Form["values"];
                var instance = await dbSet.FindAsync(key);
                if(instance == null)
                    throw new Exception("No se encontro el registro...");
                JsonConvert.PopulateObject(values, instance);
                
                var validationResults = instance.Validate();
                if (validationResults.Any()) throw new CustomValidationException(validationResults);
                
                await db.SaveChangesAsync();
                return instance;
            });
        });
        
        app.MapDelete(Route, async (HttpContext httpContext, TDbContext db) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var key = int.Parse(httpContext.Request.Form["key"].ToString().Replace("\"", ""));
                var instance = await dbSet.FindAsync(key);
                if(instance == null)
                    throw new Exception("No se encontro el registro...");
                dbSet.Remove(instance);
                await db.SaveChangesAsync();
                return true;
            });
        });
        Console.WriteLine("Auto created route: /"+Route);
    }

    public Type GetServiceType() => null;
}