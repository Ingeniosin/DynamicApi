using Microsoft.EntityFrameworkCore;

namespace DynamicApi.Manager.Api.Managers; 

public interface ITypeManager<in TDbContext> where TDbContext : DbContext {
    
    void InitDefaults(TDbContext context);
    
}