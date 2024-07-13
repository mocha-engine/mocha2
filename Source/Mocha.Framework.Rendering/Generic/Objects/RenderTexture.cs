﻿namespace Apparatus.Core.Rendering;

public class RenderTexture : RenderObject
{
	public RenderTexture( RenderTextureInfo info )
	{
		IRenderContext.Current.CreateRenderTexture( info, out Handle );
	}
}
