namespace Mocha.Rendering;

[Flags]
public enum BufferUsageFlags
{
	VertexBuffer = 1 << 1,
	IndexBuffer = 1 << 2,
	UniformBuffer = 1 << 3,
	TransferSrc = 1 << 4,
	TransferDst = 1 << 5
}
