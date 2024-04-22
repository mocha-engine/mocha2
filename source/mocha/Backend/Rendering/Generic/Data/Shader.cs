namespace Mocha.Rendering;

internal class Shader : RenderObject
{
	public Shader( ShaderInfo info )
	{
		IRenderContext.Current.CreateShader( info, out Handle );
	}
}
