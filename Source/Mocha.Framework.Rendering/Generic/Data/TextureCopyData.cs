namespace Mocha.Rendering;

public record TextureCopyData
{
	public uint SourceX = 0;
	public uint SourceY = 0;
	public uint DestX = 0;
	public uint DestY = 0;
	public uint Width = 0;
	public uint Height = 0;

	public ImageTexture Source = null!;
}
