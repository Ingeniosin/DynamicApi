using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.Helpers;

namespace Siian_Office_V2.Manager;

public class DataSourceLoadOptions : DataSourceLoadOptionsBase{
    public static ValueTask<DataSourceLoadOptions> BindAsync(HttpContext httpContext){
        var loadOptions = new DataSourceLoadOptions();
        DataSourceLoadOptionsParser.Parse(loadOptions, key => httpContext.Request.Query[key].FirstOrDefault());
        return ValueTask.FromResult(loadOptions);
    }
}