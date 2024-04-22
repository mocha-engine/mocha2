namespace Mocha.Rendering;

internal class RenderTexture : RenderObject
{
	public RenderTexture( RenderTextureInfo info )
	{
		IRenderContext.Current.CreateRenderTexture( info, out Handle );
	}
}
