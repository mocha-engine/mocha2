namespace Mocha.Rendering;

public record TextureData
{
	public uint Width = 0;
	public uint Height = 0;
	public uint MipCount = 1;

	public byte[] MipData = null!;
	public int ImageFormat = 0;
}
