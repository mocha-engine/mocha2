namespace Apparatus.Core.Rendering;

public record BufferInfo
{
	public string Name = "Unnamed Buffer";
	public uint size = 0;
	public BufferType Type = BufferType.Staging;
	public BufferUsageFlags Usage = BufferUsageFlags.IndexBuffer;
}
