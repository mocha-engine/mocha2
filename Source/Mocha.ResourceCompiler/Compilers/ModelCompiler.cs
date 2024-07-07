namespace Mocha.ResourceCompiler;

[Compiler( new[] { ".model.json" }, ".model" )]
public class ModelCompiler : IAssetCompiler
{
	/// <inheritdoc />
	public Task<CompileResult> CompileFile( CompileInput compileInput )
	{
		// Just copy directly
		return Task.FromResult( CompileResult.Success( compileInput.RawData ) );
	}
}
