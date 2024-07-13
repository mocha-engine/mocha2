namespace Apparatus.Core.Rendering.Vulkan;

internal struct QueueFamilyIndices
{
	public uint? GraphicsFamily { get; set; }
	public uint? PresentFamily { get; set; }

	public readonly bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;
}
