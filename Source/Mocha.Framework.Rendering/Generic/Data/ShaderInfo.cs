namespace Apparatus.Core.Rendering;

public record ShaderInfo
{
	public string Name = "Unnamed Shader";
	public uint[] FragmentData = [];
	public uint[] VertexData = [];
}
