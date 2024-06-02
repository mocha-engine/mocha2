namespace Mocha;

[Resource]
public partial struct TextureData
{
	public uint Width { get; set; }
	public uint Height { get; set; }
	public uint MipCount { get; set; }
	public byte[][]? Data { get; set; }
	public TextureFormat Format { get; set; }
}
