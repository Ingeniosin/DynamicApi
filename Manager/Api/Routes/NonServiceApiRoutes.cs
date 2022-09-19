using DevExtreme.AspNet.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DynamicApi.Manager.Api.Routes; 

public class NonServiceApiRoutes<T, TDbContext> where T : class where TDbContext : DynamicDbContext {
    
    private readonly Func<TDbContext, DbSet<T>> _dbSetReference;
    private readonly Func<TDbContext, T> _newInstanceReference;


    public NonServiceApiRoutes(Func<TDbContext, DbSet<T>> dbSetReference, Func<TDbContext, T> newInstanceReference) {
        _dbSetReference = dbSetReference;
        _newInstanceReference = newInstanceReference;
    }

    public async Task<IResult> Get(DataSourceLoadOptions dataSourceLoadOptions, HttpContext context, TDbContext db) {
        var dbSetReference = _dbSetReference(db).AsQueryable();
        var fields = dataSourceLoadOptions.Select?.ToList();
        dataSourceLoadOptions.StringToLower = true;
        if(fields == null) return await ApiUtils.Result(async () => await DataSourceLoader.LoadAsync(dbSetReference, dataSourceLoadOptions));
        if(!fields.Contains("id")) 
            fields.Add("id");
        dataSourceLoadOptions.Select = fields.ToArray();
        return await ApiUtils.Result(async () => await DataSourceLoader.LoadAsync(dbSetReference, dataSourceLoadOptions));
    }
    
    public async Task<IResult> Post(HttpContext context, TDbContext db) {
        return await ApiUtils.Result(async () => {
            var model = _newInstanceReference(db);
            var dbSet = _dbSetReference(db);
            var values = context.Request.Form["values"];
            JsonConvert.PopulateObject(values, model, ApiUtils.PostOrPutSettings);
            await dbSet.AddAsync(model);
            await db.SaveChangesAsync();
            return true;
        });
    }
    
    public async Task<IResult> Put( HttpContext context, TDbContext db) {
        return await ApiUtils.Result(async () => {
            var dbSet = _dbSetReference(db);
            var key = int.Parse(context.Request.Form["key"].ToString().Replace("\"", ""));
            var values = context.Request.Form["values"];
            var model = await dbSet.FindAsync(key);
            if(model == null)
                throw new Exception("Model not found.");
            JsonConvert.PopulateObject(values, model, ApiUtils.PostOrPutSettings);
            dbSet.Update(model);
            await db.SaveChangesAsync();
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
            dbSet.Remove(model);
            await db.SaveChangesAsync();
            return true;
        });
    }

}