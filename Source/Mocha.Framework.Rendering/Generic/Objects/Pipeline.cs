namespace Apparatus.Core.Rendering;

public class Pipeline : RenderObject
{
	public Pipeline( PipelineInfo info )
	{
		IRenderContext.Current.CreatePipeline( info, out Handle );
	}
}
