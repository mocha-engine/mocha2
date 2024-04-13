namespace Mocha.Rendering;

public enum RenderTextureType
{
	Color,
	ColorOpaque,
	Depth
}

public enum SamplerType
{
	Point,
	Linear,
	Anisotropic
}

public enum BufferType
{
	Staging,
	VertexIndexData,
	UniformData
}

public enum DescriptorBindingType
{
	Image
}

public enum VertexAttributeFormat
{
	Int,
	Float,
	Float2,
	Float3,
	Float4
}

[Flags]
public enum BufferUsageFlags
{
	VertexBuffer = 1 << 1,
	IndexBuffer = 1 << 2,
	UniformBuffer = 1 << 3,
	TransferSrc = 1 << 4,
	TransferDst = 1 << 5
}

public enum ShaderType
{
	Vertex,
	Fragment
}

public record TextureInfo
{
	public uint Width = 0;
	public uint Height = 0;

	// This should be 1 or higher.
	public uint MipCount = 1;
}

public record RenderTextureInfo : TextureInfo
{
	public string Name = "Unnamed Render Texture";
	public RenderTextureType Type;
}

public record ImageTextureInfo : TextureInfo
{
	public string Name = "Unnamed Image Texture";
}

public record TextureData
{
	public uint Width = 0;
	public uint Height = 0;
	public uint MipCount = 1;

	public byte[] MipData = null!;
	public int ImageFormat = 0;
}

public record TextureCopyData
{
	public uint SourceX = 0;
	public uint SourceY = 0;
	public uint DestX = 0;
	public uint DestY = 0;
	public uint Width = 0;
	public uint Height = 0;

	public ImageTexture Source = null!;
}

public record BufferInfo
{
	public string Name = "Unnamed Buffer";
	public uint size = 0;
	public BufferType Type = BufferType.Staging;
	public BufferUsageFlags Usage = BufferUsageFlags.IndexBuffer;
}

public record BufferUploadInfo
{
	public byte[] Data = null!;
}

public record DescriptorBindingInfo
{
	public DescriptorBindingType Type = DescriptorBindingType.Image;
	public ImageTexture Image = null!;
}

public record DescriptorInfo
{
	public string Name = "Unnamed Descriptor";
	public List<DescriptorBindingInfo> Bindings = new();
}

public record DescriptorUpdateInfo
{
	public int Binding = 0;
	public ImageTexture Source = null!;
	public SamplerType SamplerType = SamplerType.Anisotropic;
}

public record ShaderInfo
{
	public string Name = "Unnamed Shader";
	public uint[] FragmentData;
	public uint[] VertexData;
}

public record VertexAttributeInfo
{
	public string Name = "Unnamed Vertex Attribute";
	public VertexAttributeFormat Format = VertexAttributeFormat.Float;
}

public record PipelineInfo
{
	public string Name = "Unnamed Pipeline";
	public ShaderInfo ShaderInfo = null!;
	public List<Descriptor> Descriptors = new();
	public List<VertexAttributeInfo> VertexAttributes = new();
	public bool IgnoreDepth = false;
	public bool RenderToSwapchain = false;
}

public record GPUInfo
{
	public string Name = "Unnamed";
}

