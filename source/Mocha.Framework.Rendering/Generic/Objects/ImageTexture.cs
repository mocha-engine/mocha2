namespace Mocha.Rendering;

public class ImageTexture : RenderObject
{
	public ImageTexture( ImageTextureInfo info )
	{
		IRenderContext.Current.CreateImageTexture( info, out Handle );
	}

	public void SetData( TextureData textureData )
	{
		IRenderContext.Current.SetImageTextureData( Handle, textureData );
	}

	public void Copy( TextureCopyData textureCopyData )
	{
		IRenderContext.Current.CopyImageTexture( Handle, textureCopyData );
	}
}
