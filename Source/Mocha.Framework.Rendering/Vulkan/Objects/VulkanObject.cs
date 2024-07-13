using Silk.NET.Vulkan;

namespace Apparatus.Core.Rendering.Vulkan;

internal class VulkanObject
{
	protected VulkanRenderContext Parent = null!;

	protected void SetParent( VulkanRenderContext parent )
	{
		Parent = parent;
	}

	protected void SetDebugName( string name, ObjectType objectType, ulong handle )
	{

	}

	public virtual void Delete()
	{
		Log.Warning( $"Delete was called on {GetType().Name} but it hasn't been overridden!" );
	}
}
