namespace Mocha.Rendering;

public record TextureInfo
{
	public uint Width = 0;
	public uint Height = 0;

	// This should be 1 or higher.
	public uint MipCount = 1;
}