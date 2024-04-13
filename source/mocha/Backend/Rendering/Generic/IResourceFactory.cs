public interface IResourceFactory
{
	T CreateResource<T>() where T : IDisposable;
}
