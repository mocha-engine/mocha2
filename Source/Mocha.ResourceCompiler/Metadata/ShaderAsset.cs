namespace Mocha.ResourceCompiler;

public sealed class ShaderAsset : AssetMetadata
{
	[ShaderSection( "Meta" )]
	public string? Meta;

	[ShaderSection( "Common" )]
	public string? Common;

	[ShaderSection( "Vertex" )]
	public string? Vertex;

	[ShaderSection( "Fragment" )]
	public string? Fragment;

	[ShaderSection( "Compute" )]
	public string? Compute;
}
