using Silk.NET.Vulkan;

namespace Mocha.Rendering.Vulkan
{
	[ImplFor<VulkanBackend>]
	public class VulkanTexture : ITexture
	{
		private Image _image;
		private DeviceMemory _imageMemory;
		private ImageView _imageView;

		public void Create( TextureCreateInfo textureCreateInfo )
		{
			if ( IRenderingBackend.Current is not VulkanBackend backend )
				throw new InvalidOperationException( "Current rendering backend is not Vulkan." );

			var vk = backend.Vk;
			var device = backend.Device;
			var physicalDevice = backend.PhysicalDevice;
			var queue = backend.Queue;
			var cmd = backend.CommandBuffer;

			var imageExtent = new Extent3D
			{
				Width = (uint)textureCreateInfo.Width,
				Height = (uint)textureCreateInfo.Height,
				Depth = 1
			};

			//
			// Create Image
			//
			var imageInfo = new ImageCreateInfo
			{
				SType = StructureType.ImageCreateInfo,
				ImageType = ImageType.Type2D,
				Extent = imageExtent,
				MipLevels = (uint)textureCreateInfo.MipCount,
				ArrayLayers = 1,
				Format = textureCreateInfo.Format == TextureFormat.Rgb24 ? Format.R8G8B8A8Unorm : Format.R8G8B8A8Unorm,
				Tiling = ImageTiling.Optimal,
				InitialLayout = ImageLayout.Undefined,
				Usage = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit,
				Samples = SampleCountFlags.Count1Bit,
				SharingMode = SharingMode.Exclusive
			};

			unsafe { VulkanBackend.VkCheck( vk.CreateImage( device, imageInfo, null, out _image ) ); }

			//
			// Allocate Memory
			//
			vk.GetImageMemoryRequirements( device, _image, out var memRequirements );
			var allocInfo = new MemoryAllocateInfo
			{
				SType = StructureType.MemoryAllocateInfo,
				AllocationSize = memRequirements.Size,
				MemoryTypeIndex = FindMemoryType( vk, physicalDevice, memRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit )
			};

			unsafe { VulkanBackend.VkCheck( vk.AllocateMemory( device, allocInfo, null, out _imageMemory ) ); }
			VulkanBackend.VkCheck( vk.BindImageMemory( device, _image, _imageMemory, 0 ) );

			var subresourceRange = new ImageSubresourceRange
			{
				AspectMask = ImageAspectFlags.ColorBit,
				BaseMipLevel = 0,
				LevelCount = imageInfo.MipLevels,
				BaseArrayLayer = 0,
				LayerCount = 1
			};

			//
			// Create ImageView
			//
			var viewInfo = new ImageViewCreateInfo
			{
				SType = StructureType.ImageViewCreateInfo,
				Image = _image,
				ViewType = ImageViewType.Type2D,
				Format = imageInfo.Format,
				SubresourceRange = subresourceRange
			};

			unsafe { VulkanBackend.VkCheck( vk.CreateImageView( device, viewInfo, null, out _imageView ) ); }

			//
			// Transition layout
			//
			backend.ImmediateSubmit( ( commandBuffer ) =>
			{
				var imageBarrierToTransfer = new ImageMemoryBarrier
				{
					SType = StructureType.ImageMemoryBarrier,

					OldLayout = ImageLayout.Undefined,
					NewLayout = ImageLayout.TransferDstOptimal,

					Image = _image,
					SubresourceRange = subresourceRange,

					SrcAccessMask = 0,
					DstAccessMask = AccessFlags.TransferWriteBit
				};

				unsafe
				{
					vk.CmdPipelineBarrier(
						commandBuffer,
						PipelineStageFlags.TopOfPipeBit,
						PipelineStageFlags.TransferBit,
						0,
						0,
						null,
						0,
						null,
						1,
						imageBarrierToTransfer
					);
				}

				var copyRegion = new BufferImageCopy
				{
					BufferOffset = 0,
					BufferRowLength = 0,
					BufferImageHeight = 0,

					ImageSubresource = new ImageSubresourceLayers( ImageAspectFlags.ColorBit, 0, 0, 1 ),
					ImageExtent = imageExtent
				};

				// vk.CmdCopyBufferToImage( commandBuffer, stagingBuffer );
			} );

			// TODO: Add code for uploading data, which would involve staging buffers, copy commands, etc.
		}

		private uint FindMemoryType( Vk vk, PhysicalDevice physicalDevice, uint typeFilter, MemoryPropertyFlags properties )
		{
			vk.GetPhysicalDeviceMemoryProperties( physicalDevice, out var memProperties );

			for ( uint i = 0; i < memProperties.MemoryTypeCount; i++ )
			{
				if ( (typeFilter & (1 << (int)i)) != 0 && (memProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties )
				{
					return i;
				}
			}
			
			throw new InvalidOperationException( "Failed to find suitable memory type!" );
		}

		public void Dispose()
		{
			if ( IRenderingBackend.Current is not VulkanBackend backend )
				throw new InvalidOperationException( "Current rendering backend is not Vulkan." );

			var _vk = backend.Vk;
			var _device = backend.Device;

			unsafe
			{
				_vk.DestroyImageView( _device, _imageView, null );
				_vk.DestroyImage( _device, _image, null );
				_vk.FreeMemory( _device, _imageMemory, null );
			}
		}
	}
}
