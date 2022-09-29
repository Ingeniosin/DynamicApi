using Castle.DynamicProxy;
using DynamicApi.Manager.Api;
using DynamicApi.Manager.Api.Managers.Service;
using Microsoft.EntityFrameworkCore;

namespace DynamicApi.Manager; 

public class DynamicDbContext : DbContext{

    public DynamicDbContext(DbContextOptions options) : base(options) {
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken()) {
        await using var scope = DynamicApi.ServiceProvider.CreateAsyncScope();
        var onSaving = await OnSaving(scope, null);
        var saveChangesAsync = await SaveChangesWithOutHandle();
        foreach (var func in onSaving) {
            await func();
        }
        return saveChangesAsync;
    }

    public Task<int> SaveChangesWithOutHandle() {
        return base.SaveChangesAsync(true, CancellationToken.None);
    }
    
    public async Task SaveChangesAsync(Query query) {
        await using var scope = DynamicApi.ServiceProvider.CreateAsyncScope();
        var onSaving = await OnSaving(scope, query);
        await SaveChangesWithOutHandle();
        foreach (var func in onSaving) {
            await func();
        }
    }

    private async Task<List<Func<Task>>> OnSaving(IServiceScope scope, Query query) {
        var entries = ChangeTracker.Entries().AsParallel().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).Select(entityEntry => {
            var entityEntryEntity = entityEntry.Entity;
            var instance = entityEntryEntity.GetType();
            var type =  ApiUtils.Unproxy(instance);
            var state = entityEntry.State;
            var services = DynamicApi.ServiceRoutes.Where(x => x.GetModelType().IsAssignableTo(type) || x.GetModelType().IsAssignableFrom(type))?.Select(x => x.GetServiceType()).Select(serviceType => serviceType != null && scope.ServiceProvider.GetService(serviceType) is IServiceModel service ? service : null).Where(x => x != null);
            var functions = new List<Func<Task<Func<Task>>>>();
            foreach (var service in services) {
                functions.Add(async () => await service.Handle(entityEntryEntity, state, query, this));
            }
            return functions;
        }).Aggregate(new List<Func<Task<Func<Task>>>>(), (list, funcs) => {
            list.AddRange(funcs);
            return list;
        });
        var onSaved = new List<Func<Task>>();
        foreach (var function in entries) {
            var savedHandle = await function();
            if (savedHandle != null) {
                onSaved.Add(savedHandle);
            }
        }
        
        return onSaved;
    }


}