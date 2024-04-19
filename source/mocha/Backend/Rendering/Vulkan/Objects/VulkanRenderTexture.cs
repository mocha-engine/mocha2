using Silk.NET.Vulkan;
using VMASharp;

namespace Mocha.Rendering.Vulkan;

internal unsafe class VulkanRenderTexture : VulkanObject
{
	private ImageUsageFlags GetUsageFlagBits( RenderTextureType type )
	{
		switch ( type )
		{
			case RenderTextureType.Color:
			case RenderTextureType.ColorOpaque:
				return ImageUsageFlags.ColorAttachmentBit;
			case RenderTextureType.Depth:
				return ImageUsageFlags.DepthStencilAttachmentBit;
		}

		throw new ArgumentException( "Invalid render texture type!" );
	}

	private Format GetFormat( RenderTextureType type )
	{
		switch ( type )
		{
			case RenderTextureType.Color:
			case RenderTextureType.ColorOpaque:
				return Format.B8G8R8A8Unorm;
			case RenderTextureType.Depth:
				return Format.D32SfloatS8Uint;
		}

		throw new ArgumentException( "Invalid render texture type!" );
	}

	private ImageAspectFlags GetAspectFlags( RenderTextureType type )
	{
		switch ( type )
		{
			case RenderTextureType.Color:
			case RenderTextureType.ColorOpaque:
				return ImageAspectFlags.ColorBit;
			case RenderTextureType.Depth:
				return ImageAspectFlags.DepthBit;
		}

		throw new ArgumentException( "Invalid render texture type!" );
	}

	public Image Image;
	public Allocation Allocation;
	public ImageView ImageView;
	public Format Format;

	public Size2D Size;

	public VulkanRenderTexture( VulkanRenderContext parent )
	{
		SetParent( parent );
	}

	public VulkanRenderTexture( VulkanRenderContext parent, RenderTextureInfo textureInfo )
	{
		SetParent( parent );

		Size = new Size2D( textureInfo.Width, textureInfo.Height );

		var depthImageExtent = new Extent3D( textureInfo.Width, textureInfo.Height, 1 );
		Format = GetFormat( textureInfo.Type );
		var imageCreateInfo = VKInit.ImageCreateInfo( Format, GetUsageFlagBits( textureInfo.Type ) | ImageUsageFlags.SampledBit, depthImageExtent, 1 );

		AllocationCreateInfo allocInfo = new();
		allocInfo.Usage = MemoryUsage.GPU_Only;
		allocInfo.RequiredFlags = MemoryPropertyFlags.DeviceLocalBit;

		Image = Parent.Allocator.CreateImage( imageCreateInfo, allocInfo, out Allocation );

		ImageViewCreateInfo viewInfo = VKInit.ImageViewCreateInfo( Format, Image, GetAspectFlags( textureInfo.Type ), 1 );
		Parent.Vk.CreateImageView( Parent.Device, viewInfo, null, out ImageView );

		SetDebugName( "RenderTexture Image", ObjectType.Image, Image.Handle );
		SetDebugName( "RenderTexture Image View", ObjectType.ImageView, ImageView.Handle );
	}

	public override void Delete()
	{
		Parent.Vk.DestroyImageView( Parent.Device, ImageView, null );
		Allocation.Dispose();
		Parent.Vk.DestroyImage( Parent.Device, Image, null );
	}
}
