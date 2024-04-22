﻿using Silk.NET.Vulkan;

namespace Mocha.Rendering.Vulkan;

internal struct VulkanVertexInputDescription
{
	public List<VertexInputBindingDescription> Bindings;
	public List<VertexInputAttributeDescription> Attributes;

	public uint Flags;

	public VulkanVertexInputDescription()
	{
		Bindings = new();
		Attributes = new();
	}
}
