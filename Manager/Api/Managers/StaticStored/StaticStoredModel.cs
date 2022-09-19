namespace DynamicApi.Manager.Api.Managers.StaticStored; 

public abstract class StaticStoredModel {
    [JsonShow]
    public abstract int Id { get; set; }
}