using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DynamicApi.Manager;

public class CustomContractResolver : DefaultContractResolver{

    private readonly List<string> _showProperties = new();
    public bool IsPut = false;
    
    public CustomContractResolver(){
        NamingStrategy = new CamelCaseNamingStrategy();
    }

    public CustomContractResolver(List<string> showProperties) {
        showProperties?.ForEach(x => {
            foreach (var s in x.Split(".")) {
                _showProperties.Add(s.ToLower());
            }
        });
        NamingStrategy = new CamelCaseNamingStrategy();
    }


    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization){
        var property = base.CreateProperty(member, memberSerialization);
        if(IsPut) 
            return property;

        var attributes = property?.AttributeProvider?.GetAttributes(true);

        var nameLower = property?.PropertyName?.ToLower() ?? string.Empty;
        if(nameLower.Equals("id") || attributes?.Contains(new JsonShow()) == true) return property;

        var containsShowProperties = _showProperties.Contains(nameLower);
        /*if(containsShowProperties)
            _showProperties.Remove(nameLower);*/
        property.Readable =containsShowProperties || attributes?.Contains(new JsonIgnoreGet()) == true || !IsVirtual(member); 

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