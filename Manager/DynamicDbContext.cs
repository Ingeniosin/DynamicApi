using DynamicApi.Manager.Api.Managers.Service;
using Microsoft.EntityFrameworkCore;

namespace DynamicApi.Manager; 

public class DynamicDbContext : DbContext{

    public DynamicDbContext(DbContextOptions options) : base(options) {
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken()) {
        using var scope = DynamicApi.ServiceProvider.CreateScope();
        var onSaving = await OnSaving(scope, null);
        var saveChangesAsync = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        foreach (var func in onSaving) {
            await func();
        }
        return saveChangesAsync;
    }
    
    
    public Task<int> SaveChangesWithOutHandle() {
        return base.SaveChangesAsync(true, CancellationToken.None);
    }
    
    public async Task SaveChangesAsync(Query query) {
        using var scope = DynamicApi.ServiceProvider.CreateScope();
        var onSaving = await OnSaving(scope, query);
        await SaveChangesWithOutHandle();
        foreach (var func in onSaving) {
            await func();
        }
    }

    private async Task<List<Func<Task>>> OnSaving(IServiceScope scope, Query query) {
        var entries = ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        var onSaved = new List<Func<Task>>();
        foreach (var entityEntry in entries) {
            var entityEntryEntity = entityEntry.Entity;
            var type = entityEntryEntity.GetType();
            var state = entityEntry.State;
            var serviceType = DynamicApi.RoutesByType.GetValueOrDefault(type.Name.Replace("Proxy", ""))?.GetServiceType();
            if (serviceType == null) continue;
            if(scope.ServiceProvider.GetService(serviceType) is not IServiceModel service) continue;
            var savedHandle = await service.Handle(entityEntryEntity, state, query, this);
            if (savedHandle != null) {
                onSaved.Add(savedHandle);
            }
        }
        return onSaved;
    }


}