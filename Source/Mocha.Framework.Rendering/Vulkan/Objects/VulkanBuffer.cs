using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using VMASharp;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Mocha.Rendering.Vulkan;

internal unsafe class VulkanBuffer : VulkanObject
{
	private Silk.NET.Vulkan.BufferUsageFlags GetBufferUsageFlags( BufferInfo bufferInfo )
	{
		Silk.NET.Vulkan.BufferUsageFlags outFlags = Silk.NET.Vulkan.BufferUsageFlags.None;

		if ( bufferInfo.Usage.HasFlag( BufferUsageFlags.VertexBuffer ) )
			outFlags |= Silk.NET.Vulkan.BufferUsageFlags.VertexBufferBit;

		if ( bufferInfo.Usage.HasFlag( BufferUsageFlags.IndexBuffer ) )
			outFlags |= Silk.NET.Vulkan.BufferUsageFlags.IndexBufferBit;

		if ( bufferInfo.Usage.HasFlag( BufferUsageFlags.UniformBuffer ) )
			outFlags |= Silk.NET.Vulkan.BufferUsageFlags.UniformBufferBit;

		if ( bufferInfo.Usage.HasFlag( BufferUsageFlags.TransferSrc ) )
			outFlags |= Silk.NET.Vulkan.BufferUsageFlags.TransferSrcBit;

		if ( bufferInfo.Usage.HasFlag( BufferUsageFlags.TransferDst ) )
			outFlags |= Silk.NET.Vulkan.BufferUsageFlags.TransferDstBit;

		if ( bufferInfo.Type == BufferType.VertexIndexData )
			Debug.Assert( outFlags.HasFlag( Silk.NET.Vulkan.BufferUsageFlags.IndexBufferBit ) || outFlags.HasFlag( Silk.NET.Vulkan.BufferUsageFlags.VertexBufferBit ), "Invalid flags" );

		Debug.Assert( outFlags != Silk.NET.Vulkan.BufferUsageFlags.None, "Invalid flags" );

		return outFlags;
	}

	public Buffer buffer = default;
	public Allocation allocation = null!;

	public void Create( BufferInfo bufferInfo )
	{
		if ( IRenderContext.Current is not VulkanRenderContext context )
			throw new InvalidOperationException( "Current rendering context is not Vulkan" );

		SetParent( context );

		BufferCreateInfo bufferCreateInfo = new();
		bufferCreateInfo.SType = StructureType.BufferCreateInfo;
		bufferCreateInfo.PNext = null;
		bufferCreateInfo.Size = bufferInfo.size;
		bufferCreateInfo.Usage = GetBufferUsageFlags( bufferInfo );

		AllocationCreateInfo allocInfo = new();
		allocInfo.Usage = MemoryUsage.Unknown;
		allocInfo.Flags = AllocationCreateFlags.Mapped;

		buffer = Parent.Allocator.CreateBuffer( bufferCreateInfo, allocInfo, out allocation );

		SetDebugName( bufferInfo.Name, ObjectType.Buffer, buffer.Handle );
	}

	private struct AllocatedBuffer
	{
		public Buffer Buffer;
		public Allocation Allocation;
	}

	public void Upload( BufferUploadInfo bufferUploadInfo )
	{
		BufferCreateInfo stagingBufferInfo = new();
		stagingBufferInfo.SType = StructureType.BufferCreateInfo;
		stagingBufferInfo.PNext = null;

		stagingBufferInfo.Size = (uint)bufferUploadInfo.Data.Length;
		stagingBufferInfo.Usage = Silk.NET.Vulkan.BufferUsageFlags.TransferSrcBit;

		AllocationCreateInfo stagingAllocInfo = new();
		stagingAllocInfo.Usage = MemoryUsage.CPU_Only;

		AllocatedBuffer stagingBuffer = new();
		stagingBuffer.Buffer = Parent.Allocator.CreateBuffer( stagingBufferInfo, stagingAllocInfo, out stagingBuffer.Allocation );

		var mappedData = stagingBuffer.Allocation.Map();
		Marshal.Copy( bufferUploadInfo.Data, 0, mappedData, bufferUploadInfo.Data.Length );
		stagingBuffer.Allocation.Unmap();

		Parent.ImmediateSubmit( ( CommandBuffer cmd ) =>
		{
			BufferCopy copyRegion = new();
			copyRegion.SrcOffset = 0;
			copyRegion.DstOffset = 0;
			copyRegion.Size = (uint)bufferUploadInfo.Data.Length;

			Parent.Vk.CmdCopyBuffer( cmd, stagingBuffer.Buffer, buffer, 1, &copyRegion );

			return RenderStatus.Ok;
		} );

		stagingBuffer.Allocation.Dispose();
	}

	public override void Delete()
	{
		allocation.Dispose();
	}
}
