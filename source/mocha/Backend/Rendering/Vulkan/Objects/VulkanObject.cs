using FlexLayoutSharp;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Mocha.Rendering.Vulkan;

internal class VulkanObject
{
	protected VulkanRenderContext Parent;

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
