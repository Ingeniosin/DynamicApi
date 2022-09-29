namespace DynamicApi.Manager; 

public class DynamicList<T> : List<T> {

    public T First => this.FirstOrDefault();
    public T Last => this.LastOrDefault();

}