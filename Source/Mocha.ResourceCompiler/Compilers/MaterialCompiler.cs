using System.Text.Json;

namespace Mocha.ResourceCompiler;

[Compiler( new[] { ".material.json" }, ".material" )]
public class MaterialCompiler : IAssetCompiler
{
	/// <inheritdoc />
	public async Task<CompileResult> CompileFile( CompileInput compileInput )
	{
		return await Task.Run( () =>
		{
			var materialData = JsonSerializer.Deserialize<MaterialAsset>( compileInput.RawData );

			//
			// todo: Process?
			//

			return CompileResult.Success( JsonSerializer.SerializeToUtf8Bytes( materialData ) );
		} );
	}
}
