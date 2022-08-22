using DevExtreme.AspNet.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Stored;

public class ServicelessStoredApiManager<T, TDbContext> : IApiManager where T : class where TDbContext : DbContext{

    public string Route { get; set; }
    public ManagerConfiguration Configuration { get; set; }
    private Func<TDbContext, DbSet<T>> DbSetReference { get; }
    private Func<TDbContext, T> NewInstanceReference { get; }
    
    public ServicelessStoredApiManager(string route, ManagerConfiguration configuration, Func<TDbContext, DbSet<T>> dbSetReference) {
        Route = "/api/"+route;
        Configuration = configuration;
        DbSetReference = dbSetReference;
        NewInstanceReference = x => DbSetReference(x).CreateProxy();
    }
    
    public ServicelessStoredApiManager(string route, Func<TDbContext, DbSet<T>> dbSetReference) : this(route, new ManagerConfiguration(), dbSetReference) { }

    
    public void Init(WebApplication app) {

        if(Configuration.AllowGetRute) {
            app.MapGet(Route, async (DataSourceLoadOptions dataSourceLoadOptions, TDbContext db) => {
                return await ApiUtils.Result(async () => await DataSourceLoader.LoadAsync(DbSetReference(db), dataSourceLoadOptions));
            });
        }

        if(Configuration.AllowPostRute) {
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

        if(Configuration.AllowPutRute) {
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

        if(Configuration.AllowDeleteRute) {
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

    public Type GetServiceType() => null;

    public bool IsScoped { get; set; }
}