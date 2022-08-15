using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Siian_Office_V2.Manager;

public class CustomContractResolver : DefaultContractResolver{
    public CustomContractResolver(){
        NamingStrategy = new CamelCaseNamingStrategy();
    }
    
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization){
        var property = base.CreateProperty(member, memberSerialization);
        if(property.AttributeProvider != null && property.AttributeProvider.GetAttributes(true).Contains(new JsonIgnoreGet())){
            property.Readable = false;
        }
        return property;
    }
}

public class JsonIgnoreGet : Attribute{
    
}