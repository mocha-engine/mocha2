﻿namespace Apparatus.Core.Rendering;

public class Shader : RenderObject
{
	public Shader( ShaderInfo info )
	{
		IRenderContext.Current.CreateShader( info, out Handle );
	}
}
