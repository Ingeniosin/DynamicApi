using DynamicApi.Manager;
using DynamicApi.Manager.Api;
using DynamicApi.Manager.Api.Grouped;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DynamicApi;
/*
new DynamicApi.DynamicApi<ApplicationDbContext>(new List<IApiManager>{
    new StoredServicelessApiManager<User, ApplicationDbContext>("users", x => x.Users),
    new StoredServicelessApiManager<Post, ApplicationDbContext>("posts", x => x.Posts),

}, WebApplication.CreateBuilder(args)).Start();

coloca el conectionstring

 "ConnectionStrings": {
    "DefaultConnection": "User ID=postgres;Password=password;Host=localhost;Port=5432;Database=SacasaNew;Pooling=true;"
  },

*/

public class DynamicApi<TDbContext> where TDbContext : DbContext{

    private readonly List<IApiManager> _routes;
    private readonly WebApplicationBuilder _webApplicationBuilder;
    private readonly Action<TDbContext> _initDefaultValues;
    private readonly Action<WebApplication> _onPreStart;
    private DateTime _initTime;

    public DynamicApi(List<IApiManager> routes, WebApplicationBuilder webApplicationBuilder) : this(routes, webApplicationBuilder, null) { }

    
    public DynamicApi(List<IApiManager> routes, WebApplicationBuilder webApplicationBuilder, Action<TDbContext> initDefaultValues) : this(routes, webApplicationBuilder, initDefaultValues, null){}
    
    public DynamicApi(List<IApiManager> routes, WebApplicationBuilder webApplicationBuilder, Action<TDbContext> initDefaultValues, Action<WebApplication> onPreStart){
        _routes = routes;
        _webApplicationBuilder = webApplicationBuilder;
        _initDefaultValues = initDefaultValues;
        _onPreStart = onPreStart;
        Init();
    }

    private void Init(){
        _initTime = DateTime.Now;
        _routes.ForEach(x => {
            var isGrouped = x is GroupedStaticApiManager;
            if(!isGrouped){
                var serviceType = x.GetServiceType();
                if(serviceType != null) {
                    if(x.IsScoped) {
                        _webApplicationBuilder.Services.AddScoped(serviceType);
                    } else {
                        _webApplicationBuilder.Services.AddSingleton(serviceType);
                    }
                }
            } else{
                var groupedStaticApiManager = (GroupedStaticApiManager)x;
                groupedStaticApiManager.ApiManagers.Where(y => y.GetServiceType() != null).ToList().ForEach(y => {
                    if(y.IsScoped) {
                        _webApplicationBuilder.Services.AddScoped(y.GetServiceType());
                    } else {
                        _webApplicationBuilder.Services.AddSingleton(y.GetServiceType());
                    }
                });
            }
        });
        
        JsonConvert.DefaultSettings = () => {
            var expandoObjectConverter = new ExpandoObjectConverter();
            return new JsonSerializerSettings{
                Formatting = Formatting.Indented,
                ContractResolver = new CustomContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter>{
                    new StringEnumConverter(),
                    expandoObjectConverter,
                   // new ObjectConverter(),
                },
            };
        };

        
        _webApplicationBuilder.Services.AddControllersWithViews();
        
        _webApplicationBuilder.Services.Configure<FormOptions>(x => {
            x.ValueLengthLimit = int.MaxValue;
            x.MultipartBodyLengthLimit = int.MaxValue;
            x.MultipartHeadersLengthLimit = int.MaxValue;
        });
        
        _webApplicationBuilder.Services.Configure<HttpSysOptions>(x => {
            x.MaxRequestBodySize = null;
        });
        
        _webApplicationBuilder.Services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize =  int.MaxValue;
        });
        
        _webApplicationBuilder.Services.Configure<IISServerOptions>(options =>
        {
            options.MaxRequestBodySize = int.MaxValue;
        });
        

        _webApplicationBuilder.Services.AddDbContext<TDbContext>(x => {
            x.UseLazyLoadingProxies().UseNpgsql(_webApplicationBuilder.Configuration.GetConnectionString("DefaultConnection"));
            x.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.DetachedLazyLoadingWarning));
            x.LogTo(Console.WriteLine, new[] { RelationalEventId.CommandExecuted });
        });
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    }

    private WebApplication GetApp(){
        var app = _webApplicationBuilder.Build();

        if (!app.Environment.IsDevelopment()){
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.MapControllerRoute(name: "default", pattern: "{controller}/{action=Index}/{id?}");

        app.MapFallbackToFile("index.html");
        return app;
    }


    public WebApplication Start(){
        var app = GetApp();
        using var scope = app.Services.CreateScope();
        var applicationDbContext = scope.ServiceProvider.GetService<TDbContext>();
        if (applicationDbContext == null) throw new Exception("ApplicationDbContext is null");
        Console.WriteLine("Starting server...");
        Console.WriteLine("Creando tablas...");
        applicationDbContext.Database.Migrate();
        Console.WriteLine("Tablas creadas correctamente");
        _routes.ForEach(x => x.Init(app));
        _initDefaultValues?.Invoke(applicationDbContext);
        applicationDbContext.SaveChanges();
        Console.WriteLine($"¡Rutas creadas correctamente!");
        _onPreStart?.Invoke(app);
        Console.WriteLine($"¡Servidor iniciado correctamente en {DateTime.Now.Subtract(_initTime).TotalSeconds} segundos!");
        app.Run();
        return app;
    }
}