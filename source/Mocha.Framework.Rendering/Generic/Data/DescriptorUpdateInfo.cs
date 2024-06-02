namespace Mocha.Rendering;

public record DescriptorUpdateInfo
{
	public int Binding = 0;
	public ImageTexture Source = null!;
	public SamplerType SamplerType = SamplerType.Anisotropic;
}