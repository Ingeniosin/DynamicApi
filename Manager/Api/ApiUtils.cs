using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DynamicApi.Manager.Api;

public static class ApiUtils {

    private static JsonSerializerSettings _postOrPutSettings;
    public static JsonSerializerSettings PostOrPutSettings  {
        get {
            if(_postOrPutSettings != null) return _postOrPutSettings;
            var jsonSerializerSettings = JsonConvert.DefaultSettings!.Invoke();
            jsonSerializerSettings.ContractResolver =  new CustomContractResolver(){IsPut = true};
            _postOrPutSettings = jsonSerializerSettings;
            return _postOrPutSettings;
        }
    }

    public static async Task<IResult> Result(Func<Task<object>> action, DefaultContractResolver resolver = null) {
        var jsonSerializerSettings = JsonConvert.DefaultSettings!.Invoke();
        jsonSerializerSettings.ContractResolver = resolver ?? new CustomContractResolver();
        return await Result(action, jsonSerializerSettings);
    }

    public static async Task<IResult> Result(Func<Task<object>> action, JsonSerializerSettings customSettings){
        var start = DateTime.Now;
        try{
            var result = Results.Text(JsonConvert.SerializeObject(await action.Invoke(), customSettings ?? JsonConvert.DefaultSettings!.Invoke()), contentType: "application/json");
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