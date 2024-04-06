namespace Mocha;

[Resource]
public partial struct MaterialData
{
	public TextureData? DiffuseTexture { get; set; }
	public TextureData? NormalTexture { get; set; }
	public TextureData? PackedTexture { get; set; }
}
