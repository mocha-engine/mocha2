namespace Mocha.Rendering;

internal class Pipeline : RenderObject
{
	public Pipeline( PipelineInfo info )
	{
		IRenderContext.Current.CreatePipeline( info, out Handle );
	}
}
