namespace Apparatus.Core.Rendering;

public record DescriptorInfo
{
	public string Name = "Unnamed Descriptor";
	public List<DescriptorBindingInfo> Bindings = new();
}
