using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace DynamicApi.Manager.Api;

public static class ApiUtils{
    public static async Task<IResult> Result(Func<Task<object>> action, DefaultContractResolver customSettings = null){
        var start = DateTime.Now;
        try{
            var jsonSerializerSettings = JsonConvert.DefaultSettings!.Invoke();
            jsonSerializerSettings.ContractResolver = customSettings ?? jsonSerializerSettings.ContractResolver;
            var result = Results.Text(JsonConvert.SerializeObject(await action.Invoke(), jsonSerializerSettings), contentType: "application/json");
            Console.WriteLine("Successful request in {0}ms", (DateTime.Now - start).TotalMilliseconds);
            return result;
        } catch (CustomValidationException e){
            return Results.Json(new {
                errors = e.Errors,
                isValidationException = true,
            }, contentType: "application/json", statusCode: 400);
        } catch (Exception e){
            return Results.Json(new {
                error = e.Message,
                stackTrace = e.StackTrace,
                innerException = e.InnerException?.Message,
                isValidationException = false,
            }, contentType: "application/json", statusCode: 400);
        }   
    }
    
    /*
    public static IQueryable<TSource> Include<TSource>(this IQueryable<TSource> queryable, params string[] navigations) where TSource : class
    {
        if (navigations == null || navigations.Length == 0) return queryable;

        return navigations.Aggregate(queryable, EntityFrameworkQueryableExtensions.Include);  // EntityFrameworkQueryableExtensions.Include method requires the constraint where TSource : class
    }
    */
    
    public static List<ValidationError> Validate(this object obj){
        var context = new ValidationContext(obj);
        var dateResults = new List<ValidationResult>();
        Validator.TryValidateObject(obj, context, dateResults, true);
        return dateResults.Select(x => new ValidationError(x.MemberNames.First(), x.ErrorMessage)).ToList();
    }
    
}

public class CustomValidationException : Exception{
    public List<ValidationError> Errors { get; set; }
    
    public CustomValidationException(List<ValidationError> errors){
        Errors = errors;
    }
}

public class ValidationError{
    public string Field { get; set; }
    public string Message { get; set; }

    public ValidationError(string field, string message){
        Field = field;
        Message = message;
    }
}