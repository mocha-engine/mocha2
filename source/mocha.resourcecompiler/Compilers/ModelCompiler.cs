using BCnEncoder.Encoder;

namespace Mocha.ResourceCompiler;

[Compiler( new[] { ".model.json" }, ".model" )]
public class ModelCompiler : IAssetCompiler
{
	/// <inheritdoc />
	public async Task<CompileResult> CompileFile( CompileInput compileInput )
	{
		// Just copy directly
		return CompileResult.Success( compileInput.RawData );
	}
}
