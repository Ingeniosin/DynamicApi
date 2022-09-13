using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DynamicApi.Manager;

public class CustomContractResolver : DefaultContractResolver{
    
    public readonly List<string> ShowProperties = new List<string>();
    public bool IsPut = false;
    
    public CustomContractResolver(){
        NamingStrategy = new CamelCaseNamingStrategy();
    }

    public CustomContractResolver(List<string> showProperties) {
        ShowProperties = showProperties.Where(x => !x.Contains('.')).Select(x => x.ToLower()).ToList();
        var stringsEnumerable = showProperties.Where(x => x.Contains('.')).Select(x => x.Split("."));
        foreach (var list in stringsEnumerable) {
            foreach (var field in list) {
                ShowProperties.Add(field.ToLower());
            }
        }
        NamingStrategy = new CamelCaseNamingStrategy();
    }


    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization){
        var property = base.CreateProperty(member, memberSerialization);
        if(IsPut) 
            return property;

        var attributes = property?.AttributeProvider?.GetAttributes(true);

        var nameLower = property?.PropertyName?.ToLower() ?? string.Empty;
        if(nameLower.Equals("id") || attributes?.Contains(new JsonShow()) == true) return property;

        property.Readable =ShowProperties.Contains(nameLower) || attributes?.Contains(new JsonIgnoreGet()) == true || !IsVirtual(member); 

        return property;
    }
    
    
    

    public static bool IsVirtual(MemberInfo member) {
        dynamic method = member.GetType().GetProperty("GetMethod")?.GetValue(member);
        return method?.IsVirtual == true;
    }
}


public class JsonIgnoreGet : Attribute{
    
}

public class JsonShow : Attribute{
    
}