namespace Mocha.ResourceCompiler;

public interface IAssetCompiler
{
	/// <summary>
	/// Compiles this asset.
	/// </summary>
	public Task<CompileResult> CompileFile( CompileInput compileInput );
}
