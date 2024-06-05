using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mocha.Rendering.Vulkan;

internal struct Vertex
{
	public Vector2 Position = Vector2.Zero;
	public Vector3 Color = Vector3.Zero;

	public Vertex()
	{

	}

	public static VertexInputBindingDescription GetBindingDescription()
	{
		var bindingDescription = new VertexInputBindingDescription()
		{
			Binding = 0,
			Stride = (uint)Unsafe.SizeOf<Vertex>(),
			InputRate = VertexInputRate.Vertex
		};

		return bindingDescription;
	}

	public static VertexInputAttributeDescription[] GetAttributeDescriptions()
	{
		var attributeDescriptions = new VertexInputAttributeDescription[]
		{
			new VertexInputAttributeDescription()
			{
				Binding = 0,
				Location = 0,
				Format = Format.R32G32Sfloat,
				Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Position))
			},
			new VertexInputAttributeDescription()
			{
				Binding = 0,
				Location = 1,
				Format = Format.R32G32B32Sfloat,
				Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Color))
			}
		};

		return attributeDescriptions;
	}
}
