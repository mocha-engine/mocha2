using Silk.NET.Vulkan;

namespace Mocha.Rendering.Vulkan;

internal unsafe class VulkanCommandContext : VulkanObject
{
	public CommandPool CommandPool;
	public CommandBuffer CommandBuffer;
	public Fence Fence;

	public VulkanCommandContext( VulkanRenderContext parent )
	{
		SetParent( parent );
		
		var poolInfo = new CommandPoolCreateInfo(  );
		poolInfo.SType = StructureType.CommandPoolCreateInfo;
		poolInfo.PNext = null;

		poolInfo.QueueFamilyIndex = Parent.GraphicsQueueFamily;
		poolInfo.Flags = CommandPoolCreateFlags.ResetCommandBufferBit;
		
		VulkanRenderContext.VkCheck( Parent.Vk.CreateCommandPool( Parent.Device, poolInfo, null, out CommandPool ) );
		
		var allocInfo = new CommandBufferAllocateInfo(  );
		allocInfo.SType = StructureType.CommandBufferAllocateInfo;
		allocInfo.PNext = null;
		
		allocInfo.CommandPool = CommandPool;
		allocInfo.Level = CommandBufferLevel.Primary;
		allocInfo.CommandBufferCount = 1;

		VulkanRenderContext.VkCheck( Parent.Vk.AllocateCommandBuffers( Parent.Device, allocInfo, out CommandBuffer ) );
		
		var fenceInfo = new FenceCreateInfo(  );
		fenceInfo.SType = StructureType.FenceCreateInfo;
		fenceInfo.PNext = null;
		fenceInfo.Flags = FenceCreateFlags.SignaledBit;

		VulkanRenderContext.VkCheck( Parent.Vk.CreateFence( Parent.Device, fenceInfo, null, out Fence ) );
		
		SetDebugName( "Command Pool", ObjectType.CommandPool, CommandPool.Handle );
		SetDebugName( "Command Buffer", ObjectType.CommandBuffer, (ulong)CommandBuffer.Handle );
		SetDebugName( "Fence", ObjectType.Fence, Fence.Handle );
	}

	public override void Delete()
	{
		Parent.Vk.DestroyFence( Parent.Device, Fence, null );
		Parent.Vk.FreeCommandBuffers( Parent.Device, CommandPool, 1, CommandBuffer );
		Parent.Vk.DestroyCommandPool( Parent.Device, CommandPool, null );
	}
}
