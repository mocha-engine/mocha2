namespace Mocha.ResourceCompiler;

public record struct CompileResult( bool WasSuccess, byte[]? CompileData = null, string? ErrorMessage = null )
{
	public static CompileResult Success( byte[] compileData ) => new CompileResult( true, CompileData: compileData );
	public static CompileResult Fail( string errorMessage ) => new CompileResult( false, ErrorMessage: errorMessage );
}
