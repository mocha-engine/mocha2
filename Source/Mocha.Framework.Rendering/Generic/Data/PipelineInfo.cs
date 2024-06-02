namespace Mocha.Rendering;

public record PipelineInfo
{
	public string Name = "Unnamed Pipeline";
	public ShaderInfo ShaderInfo = null!;
	public List<Descriptor> Descriptors = new();
	public List<VertexAttributeInfo> VertexAttributes = new();
	public bool IgnoreDepth = false;
	public bool RenderToSwapchain = false;
}