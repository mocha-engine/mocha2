namespace Mocha.Rendering;

public record struct TextureCreateInfo
(
	int Width,
	int Height,

	int MipCount,
	byte[][] Data,

	TextureFormat Format
)
{
	public static TextureCreateInfo CreateFrom( TextureData textureResource )
	{
		var textureCreateInfo = new TextureCreateInfo( 
			(int)textureResource.Width, 
			(int)textureResource.Height, 
			(int)textureResource.MipCount, 
			textureResource.Data!, 
			textureResource.Format
		);

		return textureCreateInfo;
	}
}
