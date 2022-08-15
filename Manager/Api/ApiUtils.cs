using Newtonsoft.Json;

namespace Siian_Office_V2.Manager.Api;

public class ApiUtils{
    public static async Task<IResult> Result(Func<Task<object>> action){
        try{
            return Results.Text(JsonConvert.SerializeObject(await action(), Formatting.Indented), contentType: "application/json");
        } catch (Exception e){
            Console.WriteLine(e);
            return Results.Json(new {
                error = e.Message,
                stackTrace = e.StackTrace,
                innerException = e.InnerException?.Message
            }, contentType: "application/json", statusCode: 400);
        }   
    }
}