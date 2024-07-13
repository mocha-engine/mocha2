namespace Apparatus.Core.Rendering;

public record DescriptorBindingInfo
{
	public DescriptorBindingType Type = DescriptorBindingType.Image;
	public ImageTexture Image = null!;
}
