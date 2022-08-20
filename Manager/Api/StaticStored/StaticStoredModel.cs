namespace DynamicApi.Manager.Api.StaticStored; 

public abstract class StaticStoredModel {
    [JsonShow]
    public abstract int Id { get; set; }
}