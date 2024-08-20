using System.Text.Json;

namespace Mocha.ResourceCompiler;

// Compiles for:
//			Source			Destination
[Compiler( [".model.json"], ".model" )]
public partial class ModelCompiler : IAssetCompiler
{
	/// <inheritdoc />
	public async Task<CompileResult> CompileFile( CompileInput compileInput )
	{
		return await Task.Run( () =>
		{
			var modelData = JsonSerializer.Deserialize<ModelAsset>( compileInput.RawData );

			if ( modelData is null )
				return CompileResult.Fail( "Model asset was not in correct format, or we were unable to parse it" );

			var meshes = AssimpProcessor.Process( modelData ); // read whatever format into mesh data
			using var stream = new MemoryStream();
			using var br = new BinaryWriter( stream );

			br.Write( meshes.Length ); // Mesh count

			// Mesh data
			foreach ( var mesh in meshes )
			{
				br.Write( Serializer.Serialize( mesh ) );
			}

			return CompileResult.Success( stream.ToArray() );
		} );
	}
}
