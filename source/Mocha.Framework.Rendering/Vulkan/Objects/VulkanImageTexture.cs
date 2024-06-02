using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using VMASharp;

namespace Mocha.Rendering.Vulkan;

internal unsafe class VulkanImageTexture : VulkanObject
{
	private uint GetBytesPerPixel( Format format )
	{
		switch ( format )
		{
			case Format.R8G8B8A8Srgb:
			case Format.R8G8B8A8Unorm:
				return 4; // 32 bits (4 bytes)
			case Format.BC3SrgbBlock:
			case Format.BC3UnormBlock:
			case Format.BC5UnormBlock:
			case Format.BC5SNormBlock:
				return 1; // 128-bits = 4x4 pixels - 8 bits (1 byte)
			default:
				throw new NotSupportedException( "Format is not supported." );
		}
	}

	private void GetMipDimensions( uint inWidth, uint inHeight, uint mipLevel, out uint outWidth, out uint outHeight )
	{
		outWidth = inWidth >> (int)mipLevel;
		outHeight = inHeight >> (int)mipLevel;
	}

	private uint CalcMipSize( uint inWidth, uint inHeight, uint mipLevel, Format format )
	{
		GetMipDimensions( inWidth, inHeight, mipLevel, out uint outWidth, out uint outHeight );
		if ( format == Format.BC3SrgbBlock || format == Format.BC3UnormBlock ||
		     format == Format.BC5UnormBlock || format == Format.BC5SNormBlock )
		{
			outWidth = Math.Max( outWidth, 4 );
			outHeight = Math.Max( outHeight, 4 );
		}

		return outWidth * outHeight * GetBytesPerPixel( format );
	}

	private void TransitionLayout( CommandBuffer cmd, ImageLayout newLayout, AccessFlags newAccessFlags, PipelineStageFlags stageFlags )
	{
		ImageSubresourceRange range = new()
		{
			AspectMask = ImageAspectFlags.ColorBit,
			BaseMipLevel = 0,
			LevelCount = (~0U), // VK_REMAINING_MIP_LEVELS
			BaseArrayLayer = 0,
			LayerCount = 1
		};

		ImageMemoryBarrier barrier = new()
		{
			SType = StructureType.ImageMemoryBarrier,
			OldLayout = CurrentLayout,
			NewLayout = newLayout,
			Image = Image,
			SubresourceRange = range,
			SrcAccessMask = CurrentAccessMask,
			DstAccessMask = newAccessFlags
		};

		Parent.Vk.CmdPipelineBarrier( cmd, CurrentStageMask, stageFlags, 0, 0, null, 0, null, 1, ref barrier );

		CurrentStageMask = stageFlags;
		CurrentLayout = newLayout;
		CurrentAccessMask = newAccessFlags;
	}

	public AccessFlags CurrentAccessMask = 0;
	public PipelineStageFlags CurrentStageMask = PipelineStageFlags.TopOfPipeBit;
	public ImageLayout CurrentLayout = ImageLayout.Undefined;

	public Image Image = default;
	public ImageView ImageView = default;
	public Allocation Allocation = null!;
	public Format Format = Format.Undefined;

	public ImageTextureInfo TextureInfo;

	public VulkanImageTexture( VulkanRenderContext parent, ImageTextureInfo textureInfo )
	{
		SetParent( parent );

		TextureInfo = textureInfo;
	}

	public void SetData( TextureData textureData )
	{
		// Destroy old image
		Delete();

		Format imageFormat = (Format)textureData.ImageFormat;
		uint imageSize = 0;

		for ( uint i = 0; i < textureData.MipCount; ++i )
		{
			imageSize += CalcMipSize( textureData.Width, textureData.Height, i, imageFormat );
		}

		BufferInfo bufferInfo = new();
		bufferInfo.size = imageSize;
		bufferInfo.Type = BufferType.Staging;
		bufferInfo.Usage = BufferUsageFlags.TransferSrc;

		Handle bufferHandle;
		Parent.CreateBuffer( bufferInfo, out bufferHandle );

		VulkanBuffer stagingBuffer = Parent.Buffers.Get( bufferHandle );

		var mappedData = stagingBuffer.allocation.Map();
		Marshal.Copy( textureData.MipData, 0, mappedData, textureData.MipData.Length );
		stagingBuffer.allocation.Unmap();

		Extent3D imageExtent = new( textureData.Width, textureData.Height, 1 );

		var usageFlags = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit;
		var imageCreateInfo = VKInit.ImageCreateInfo( imageFormat, usageFlags, imageExtent, textureData.MipCount );

		var allocInfo = new AllocationCreateInfo();
		allocInfo.Usage = MemoryUsage.GPU_Only;

		Image = Parent.Allocator.CreateImage( imageCreateInfo, allocInfo, out Allocation );

		Parent.ImmediateSubmit( ( CommandBuffer cmd ) =>
		{
			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( 0, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			List<BufferImageCopy> mipRegions = [];

			for ( uint mip = 0; mip < textureData.MipCount; ++mip )
			{
				ulong bufferOffset = 0;

				for ( uint i = 0; i < mip; ++i )
				{
					uint mipWidth, mipHeight;
					GetMipDimensions( textureData.Width, textureData.Height, i, out mipWidth, out mipHeight );
					bufferOffset += CalcMipSize( textureData.Width, textureData.Height, i, imageFormat );
				}

				Extent3D mipExtent;
				GetMipDimensions( textureData.Width, textureData.Height, mip, out mipExtent.Width, out mipExtent.Height );
				mipExtent.Depth = 1;

				BufferImageCopy copyRegion = new();
				copyRegion.BufferOffset = bufferOffset;
				copyRegion.BufferRowLength = 0;
				copyRegion.BufferImageHeight = 0;

				copyRegion.ImageSubresource.AspectMask = ImageAspectFlags.ColorBit;
				copyRegion.ImageSubresource.MipLevel = mip;
				copyRegion.ImageSubresource.BaseArrayLayer = 0;
				copyRegion.ImageSubresource.LayerCount = 1;
				copyRegion.ImageExtent = mipExtent;

				mipRegions.Add( copyRegion );
			}

			Parent.Vk.CmdCopyBufferToImage( cmd, stagingBuffer.buffer, Image, ImageLayout.TransferDstOptimal, mipRegions.ToArray() );

			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( AccessFlags.TransferWriteBit, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			return RenderStatus.Ok;
		} );

		ImageViewCreateInfo viewCreateInfo = VKInit.ImageViewCreateInfo( imageFormat, Image, ImageAspectFlags.ColorBit, textureData.MipCount );
		Parent.Vk.CreateImageView( Parent.Device, ref viewCreateInfo, null, out ImageView );

		SetDebugName( TextureInfo.Name, ObjectType.Image, Image.Handle );
		SetDebugName( TextureInfo.Name + " View", ObjectType.ImageView, ImageView.Handle );
	}

	public void Copy( TextureCopyData copyData )
	{
		Parent.ImmediateSubmit( ( CommandBuffer cmd ) =>
		{
			var src = Parent.ImageTextures.Get( copyData.Source.Handle );

			//
			// Transition source image to VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL
			//
			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( AccessFlags.ShaderReadBit, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.TransferSrcOptimal, src.Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			//
			// Transition destination image to VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL
			//
			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( AccessFlags.ShaderReadBit, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.TransferDstOptimal, Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			ImageSubresourceLayers srcSubresource = new();
			srcSubresource.AspectMask = ImageAspectFlags.ColorBit;
			srcSubresource.MipLevel = 0;
			srcSubresource.BaseArrayLayer = 0;
			srcSubresource.LayerCount = 1;

			ImageSubresourceLayers dstSubresource = srcSubresource;

			Offset3D srcOffset = new( (int)copyData.SourceX, (int)copyData.SourceY, 0 );
			Offset3D dstOffset = new( (int)copyData.DestX, (int)copyData.DestY, 0 );

			Offset3D extent = new( (int)copyData.Width, (int)copyData.Height, 1 );

			ImageBlit region = new();
			region.SrcSubresource = srcSubresource;
			region.SrcOffsets[0] = srcOffset;
			region.SrcOffsets[1] = new Offset3D( srcOffset.X + (int)copyData.Width, srcOffset.Y + (int)copyData.Height, 1 );
			region.DstSubresource = dstSubresource;
			region.DstOffsets[0] = dstOffset;
			region.DstOffsets[1] = new Offset3D( dstOffset.X + (int)copyData.Width, dstOffset.Y + (int)copyData.Height, 1 );

			Parent.Vk.CmdBlitImage( cmd, src.Image, ImageLayout.TransferSrcOptimal, Image, ImageLayout.TransferDstOptimal, [region], Filter.Nearest );

			//
			// Return images to initial layouts
			//
			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( AccessFlags.TransferWriteBit, ImageLayout.TransferSrcOptimal, ImageLayout.ShaderReadOnlyOptimal, src.Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			//
			// Transition destination image to VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL
			//
			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( AccessFlags.TransferWriteBit, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			return RenderStatus.Ok;
		} );
	}

	public override void Delete()
	{
		Parent.Vk.DestroyImageView( Parent.Device, ImageView, null );
		Allocation.Dispose();
		Parent.Vk.DestroyImage( Parent.Device, Image, null );
	}
}
