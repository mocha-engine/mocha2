namespace Mocha.Rendering;

[AttributeUsage( AttributeTargets.Class, Inherited = false, AllowMultiple = false )]
sealed class ImplForAttribute<T> : Attribute
{
	public Type BackendType { get; }

	public ImplForAttribute()
	{
		BackendType = typeof( T );
	}
}
