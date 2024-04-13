using Mocha.Rendering.Vulkan;
using Mocha.Rendering;

internal class ResourceFactory<TBackend> : IResourceFactory where TBackend : IRenderingBackend
{
	record struct TypeMapping( Type Type, Type Backend, Type Interface );

	// ( Type, Backend )
	private List<TypeMapping> _typeMappings = new();

	public ResourceFactory()
	{
		// Look for all types that have the ImplFor<VulkanBackend> attribute.
		var typesWithAttribute = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany( assembly => assembly.GetTypes() )
			.Where( type => type.GetCustomAttributes( false ).OfType<ImplForAttribute<TBackend>>().Any() );

		foreach ( var type in typesWithAttribute )
		{
			var attribute = type.GetCustomAttributes( false ).OfType<ImplForAttribute<TBackend>>().First();
			var @interface = type.GetInterfaces().First();

			_typeMappings.Add( new( type, attribute.BackendType, @interface ) );
		}
	}

	public T CreateResource<T>() where T : IDisposable
	{
		var @interface = typeof( T );

		var targetType = _typeMappings.FirstOrDefault( x => x.Backend == typeof( VulkanBackend ) && x.Interface == @interface ).Type
			?? throw new Exception( $"No implementation for the resource type {@interface.Name} found for the Vulkan backend." );

		var instance = Activator.CreateInstance( targetType );

		if ( instance is T obj )
			return obj;

		throw new Exception( $"The target type {targetType.Name} does not implement IDisposable." );
	}
}
