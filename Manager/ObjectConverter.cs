using Newtonsoft.Json;

namespace DynamicApi.Manager; 

public class ObjectConverter : JsonConverter{

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
        throw new NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
        return serializer.Deserialize(reader, objectType);
    }

    public override bool CanConvert(Type objectType) {
        dynamic method = objectType.GetProperty("GetMethod")?.GetValue(null);
        return method?.IsVirtual == true;
    }
}