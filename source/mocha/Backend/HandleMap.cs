namespace Mocha.Rendering;

public class HandleMap<T> : List<T>
{
	public T Get( Handle handle )
	{
		return this[(int)handle.Value];
	}
}
