using Silk.NET.Vulkan;

namespace Apparatus.Core.Rendering.Vulkan;

internal unsafe class VulkanCommandContext : VulkanObject
{
	public CommandPool CommandPool;
	public CommandBuffer CommandBuffer;
	public Fence Fence;

	public VulkanCommandContext( VulkanRenderContext parent )
	{
		SetParent( parent );

		var poolInfo = new CommandPoolCreateInfo();
		poolInfo.SType = StructureType.CommandPoolCreateInfo;
		poolInfo.PNext = null;

		poolInfo.QueueFamilyIndex = Parent.GraphicsQueueFamily;
		poolInfo.Flags = CommandPoolCreateFlags.ResetCommandBufferBit;

		VulkanRenderContext.VkCheck( Parent.Vk.CreateCommandPool( Parent.Device, ref poolInfo, null, out CommandPool ) );

		var allocInfo = new CommandBufferAllocateInfo();
		allocInfo.SType = StructureType.CommandBufferAllocateInfo;
		allocInfo.PNext = null;

		allocInfo.CommandPool = CommandPool;
		allocInfo.Level = CommandBufferLevel.Primary;
		allocInfo.CommandBufferCount = 1;

		VulkanRenderContext.VkCheck( Parent.Vk.AllocateCommandBuffers( Parent.Device, ref allocInfo, out CommandBuffer ) );

		var fenceInfo = new FenceCreateInfo();
		fenceInfo.SType = StructureType.FenceCreateInfo;
		fenceInfo.PNext = null;
		fenceInfo.Flags = FenceCreateFlags.SignaledBit;

		VulkanRenderContext.VkCheck( Parent.Vk.CreateFence( Parent.Device, ref fenceInfo, null, out Fence ) );

		SetDebugName( "Command Pool", ObjectType.CommandPool, CommandPool.Handle );
		SetDebugName( "Command Buffer", ObjectType.CommandBuffer, (ulong)CommandBuffer.Handle );
		SetDebugName( "Fence", ObjectType.Fence, Fence.Handle );
	}

	public override void Delete()
	{
		Parent.Vk.DestroyFence( Parent.Device, Fence, null );
		Parent.Vk.FreeCommandBuffers( Parent.Device, CommandPool, 1, ref CommandBuffer );
		Parent.Vk.DestroyCommandPool( Parent.Device, CommandPool, null );
	}
}
