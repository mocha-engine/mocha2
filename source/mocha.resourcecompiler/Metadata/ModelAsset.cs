namespace Mocha.ResourceCompiler;

public sealed class ModelAsset : AssetMetadata
{
	public string? Model { get; set; }

	public sealed class MaterialReference
	{
		public string? Name { get; set; }
		public string? Path { get; set; }
	}

	public List<MaterialReference>? Materials { get; set; }
}
