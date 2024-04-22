namespace Mocha.Rendering;

public class HandleMap<T> : List<T>
{
	public T Get( Handle handle )
	{
		return this[(int)handle.Value];
	}

	public new Handle Add( T value )
	{
		base.Add( value );
		return new Handle( (uint)( Count - 1 ) );
	}
}
