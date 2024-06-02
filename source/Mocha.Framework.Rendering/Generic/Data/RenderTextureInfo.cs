namespace Mocha.Rendering;

public record RenderTextureInfo : TextureInfo
{
	public string Name = "Unnamed Render Texture";
	public RenderTextureType Type;
}