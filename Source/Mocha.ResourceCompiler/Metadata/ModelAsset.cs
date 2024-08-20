using System.Text.Json.Serialization;

namespace Mocha.ResourceCompiler;

public sealed class ModelAsset : AssetMetadata
{
	public sealed class Material
	{
		[JsonRequired]
		public string Path { get; set; } = null!;
	}

	public sealed class Mesh
	{
		[JsonRequired]
		public string Path { get; set; } = null!;
	}

	public List<Mesh>? Meshes { get; set; } = null;
	public List<Material>? Materials { get; set; } = null;
}
