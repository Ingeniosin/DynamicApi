using DevExtreme.AspNet.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Siian_Office_V2.Manager;

namespace DynamicApi.Manager.Api.Stored;

public class StoredApiManager<T, TService, TDbContext> : IApiManager where T : class where TService : StoredModelService<T> where TDbContext : DbContext{
    private Func<TDbContext, DbSet<T>> DbSetReference { get; }
    private Func<TDbContext, T> NewInstanceReference { get; }
    public string Route{ get; set; }

    public StoredApiManager(string route, Func<TDbContext, DbSet<T>> dbSetReference){
        Route = "/api/"+route;
        DbSetReference = dbSetReference;
        NewInstanceReference = x => DbSetReference(x).CreateProxy();
    }

    public void Init(WebApplication app) {
        app.MapGet(Route, async (DataSourceLoadOptions dataSourceLoadOptions, TDbContext db, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var loadResult = await DataSourceLoader.LoadAsync(DbSetReference(db), dataSourceLoadOptions, CancellationToken.None);
                try{
                    var loadResultData = (IEnumerable<T>)loadResult.data;
                    await service.OnFetchedAll(loadResultData.ToList());
                } catch (Exception e){ // ignored
                }

                return loadResult;
            });
        });

        app.MapPost(Route, async (HttpContext httpContext, TDbContext db, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var newInstance = NewInstanceReference(db);
                var values = httpContext.Request.Form["values"];
                JsonConvert.PopulateObject(values, newInstance);
                
                var validationResults = newInstance.Validate();
                if (validationResults.Any()) throw new CustomValidationException(validationResults);
                
                await service.OnCreating(newInstance, httpContext);
                var addedEntity = await dbSet.AddAsync(newInstance);
                await db.SaveChangesAsync();
                try {
                    var onCreated = await service.OnCreated(addedEntity.Entity, httpContext);
                    if(onCreated != null) {
                        dbSet.Update(addedEntity.Entity);
                        await db.SaveChangesAsync();
                    }
                } catch (Exception e){ 
                    dbSet.Remove(addedEntity.Entity);
                    await db.SaveChangesAsync();
                    throw;
                }
                return addedEntity.Entity;
            });
        });
        
        app.MapPut(Route, async (HttpContext httpContext, TDbContext db, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var key = int.Parse(httpContext.Request.Form["key"].ToString().Replace("\"", ""));
                var values = httpContext.Request.Form["values"];
                var instance = await dbSet.FindAsync(key);
                if(instance == null)
                    throw new Exception("No se encontro el registro...");
                var prevObj = (T) db.Entry(instance).CurrentValues.ToObject();
                JsonConvert.PopulateObject(values, instance);
                
                var validationResults = instance.Validate();
                if (validationResults.Any()) throw new CustomValidationException(validationResults);
                
                await service.OnUpdating(instance, prevObj);
                await db.SaveChangesAsync();

                try {
                    var onUpdated = await service.OnUpdated(instance, prevObj);
                    if(onUpdated != null) {
                        dbSet.Update(onUpdated);
                        await db.SaveChangesAsync();
                    }
                } catch (Exception e){
                    db.Entry(instance).CurrentValues.SetValues(prevObj);
                    await db.SaveChangesAsync();
                    throw;
                }

                return instance;
            });
        });
        
        app.MapDelete(Route, async (HttpContext httpContext, TDbContext db, [FromServices] TService service) => {
            return await ApiUtils.Result(async () => {
                var dbSet = DbSetReference(db);
                var key = int.Parse(httpContext.Request.Form["key"].ToString().Replace("\"", ""));
                var instance = await dbSet.FindAsync(key);
                if(instance == null)
                    throw new Exception("Serializable not found");
                await service.OnDeleting(instance);
                dbSet.Remove(instance);
                await db.SaveChangesAsync();
                try {
                    await service.OnDeleted(instance);
                } catch (Exception e){
                    dbSet.Add(instance);
                    await db.SaveChangesAsync();
                    throw;
                }
                return instance;
            });
        });
        Console.WriteLine("Auto created route: /"+Route);
    }

    public Type GetServiceType() => typeof(TService);
}

