using DevExtreme.AspNet.Data;
using DynamicApi.Manager.Api.Managers.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Routes; 

public class ServiceApiRoutes<T, TService, TDbContext> where T : class where TDbContext : DynamicDbContext where TService : ServiceModel<T>{
    
    private readonly Func<TDbContext, DbSet<T>> _dbSetReference;
    private readonly Func<TDbContext, T> _newInstanceReference;
    private readonly ServiceConfiguration _serviceConfiguration;


    public ServiceApiRoutes(Func<TDbContext, DbSet<T>> dbSetReference, Func<TDbContext, T> newInstanceReference, ServiceConfiguration serviceConfiguration) {
        _dbSetReference = dbSetReference;
        _newInstanceReference = newInstanceReference;
        _serviceConfiguration = serviceConfiguration;
    }

    public async Task<IResult> Get(DataSourceLoadOptions dataSourceLoadOptions, HttpContext context, TDbContext db, [FromServices] TService service) {
        var dbSetReference = _dbSetReference(db).AsQueryable();
        var select = dataSourceLoadOptions.Select?.Where(x => x.Contains('.')).ToList();

        select?.Select(x => {
            var strings = x.Split('.');
            return x.Replace("." + strings[^1], "");
        }).Distinct().ToList().ForEach(x => {
            dbSetReference = dbSetReference.Include(x);
        });
        dataSourceLoadOptions.Select = null;
        
        return await ApiUtils.Result(async () => {
            var dbSet = _dbSetReference(db);
            var loadResult = await DataSourceLoader.LoadAsync(dbSetReference, dataSourceLoadOptions);
            var isValid = _serviceConfiguration.OnGet && loadResult.data is IEnumerable<T>;
            if(!isValid) return loadResult;
            var loadResultData = loadResult.data as IEnumerable<T>;
            var query = new Query(dataSourceLoadOptions, context);
            foreach (var model in loadResultData!) {
                await service.OnGet(model, query);
                dbSet.Update(model);
            }
            await db.SaveChangesAsync();
            return loadResult;
        }, new CustomContractResolver(select));
    }
    
    public async Task<IResult> Post(HttpContext context, TDbContext db) {
        return await ApiUtils.Result(async () => {
            var model = _newInstanceReference(db);
            var dbSet = _dbSetReference(db);
            var values = context.Request.Form["values"];
            JsonConvert.PopulateObject(values, model, ApiUtils.PostOrPutSettings);
            var query = new Query(null, context);
            await dbSet.AddAsync(model);
            await db.SaveChangesAsync(query);
            return true;
        });
    }
    
    public async Task<IResult> Put(HttpContext context, TDbContext db) {
        return await ApiUtils.Result(async () => {
            var dbSet = _dbSetReference(db);
            var key = int.Parse(context.Request.Form["key"].ToString().Replace("\"", ""));
            var values = context.Request.Form["values"];
            var model = await dbSet.FindAsync(key);
            if(model == null)
                throw new Exception("Model not found.");
            var query = new Query(null, context);
            JsonConvert.PopulateObject(values, model, ApiUtils.PostOrPutSettings);
            dbSet.Update(model);
            await db.SaveChangesAsync(query);
            return true;
        });
    }

    public async Task<IResult> Delete(HttpContext context, TDbContext db) {
        return await ApiUtils.Result(async () => {
            var dbSet = _dbSetReference(db);
            var key = int.Parse(context.Request.Form["key"].ToString().Replace("\"", ""));
            var model = await dbSet.FindAsync(key);
            if(model == null)
                throw new Exception("Model not found.");
            var query = new Query(null, context);
            dbSet.Remove(model);
            await db.SaveChangesAsync(query);
            return true;
        });
    }

}