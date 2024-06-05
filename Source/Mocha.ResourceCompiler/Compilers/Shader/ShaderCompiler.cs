using System.Text;
using System.Text.Json;
using Veldrid;
using Veldrid.SPIRV;

namespace Mocha.ResourceCompiler;

[Compiler( new[] { ".shader.glsl" }, ".shader" )]
public class ShaderCompiler : IAssetCompiler
{
	/// <summary>
	/// Compiles a shader from GLSL into SPIR-V using Veldrid's libshaderc bindings.
	/// </summary>
	/// <returns>Vulkan-compatible SPIR-V bytecode.</returns>
	private async Task<uint[]> CompileShader( string? commonSource, string shaderSource, ShaderStages shaderStage, string debugName = "temp" )
	{
		Log.Info( $"📥 Compiling {shaderStage} stage for {debugName}..." );

		shaderSource = shaderSource.Trim();
		commonSource = commonSource?.Trim();

		//
		// Prepend a preamble with GLSL version & macro definitions
		//
		var preamble = new StringBuilder();
		preamble.AppendLine( $"#version 460" );
		preamble.AppendLine( commonSource );

		preamble.AppendLine();
		shaderSource = preamble.ToString() + shaderSource;

		//
		// Perform the compilation
		//
		var compileOptions = new GlslCompileOptions( false );
		SpirvCompilationResult? compilationResult = null;

		try
		{
			compilationResult = SpirvCompilation.CompileGlslToSpirv( shaderSource, $"{debugName}_{shaderStage}.glsl", shaderStage, compileOptions );

			if ( compilationResult == null )
			{
				Log.Info( $"⚠️ Failed to compile {shaderStage} stage for {debugName}" );
				return Array.Empty<uint>();
			}

			// Data will be in bytes, but we want it in 32-bit integers as that is what Vulkan expects
			var dataBytes = compilationResult.SpirvBytes;
			var dataUints = new uint[dataBytes.Length / 4];
			Buffer.BlockCopy( dataBytes, 0, dataUints, 0, dataBytes.Length );

			Log.Info( $"🎉 Compiled {shaderStage} stage for {debugName}!" );
			return dataUints;
		}
		catch ( SpirvCompilationException ex )
		{
			// Message format: Compilation failed: default.shader_Vertex.glsl:2: error: '' :  syntax error, unexpected LEFT_BRACE
			var preambleLineCount = preamble.ToString().Split( '\n' ).Length;

			// Parse the error message to get info
			var error = ex.Message;
			var split = error.Split( ':' ).Select( x => x.Trim() ).ToArray();

			// Double-check to make sure we have a valid error
			if ( split.Length < 5 )
				throw new Exception( "Unknown compile error." );

			var lineNumber = int.Parse( split[2] ) - preambleLineCount;
			var errorMessage = string.Join( ':', split[5..] );

			var shaderSourceLines = shaderSource.Split( Environment.NewLine );

			StringBuilder sb = new();
			sb.AppendLine( $"Error in {debugName}, stage {shaderStage}, line {lineNumber}" );
			sb.AppendLine( $"```" );

			lineNumber += 4; // HACK: not sure what causes this offset
			for ( int i = 0; i < shaderSourceLines.Length; ++i )
			{
				if ( i < lineNumber - 1 || i > lineNumber + 1 )
					continue;

				var str = shaderSourceLines[i].Trim();

				if ( i == lineNumber - 1 )
					str = $"{str,-64} <-- {errorMessage}";

				sb.AppendLine( str );
			}

			sb.AppendLine( $"```" );

			Log.Error( sb.ToString() );

			{
				Log.Info( $"⚠️ Failed to compile {shaderStage} stage for {debugName}" );
				return Array.Empty<uint>();
			}
		}
	}

	/// <inheritdoc />
	public async Task<CompileResult> CompileFile( CompileInput compileInput )
	{
		byte[] shaderBytes = compileInput.RawData;

		// Ignore byte order mark
		if ( shaderBytes.Length >= 3 && shaderBytes[0] == 0xEF && shaderBytes[1] == 0xBB && shaderBytes[2] == 0xBF )
		{
			shaderBytes = shaderBytes[3..];
		}

		string shaderString = Encoding.Default.GetString( shaderBytes );

		var shaderParser = new ShaderParser( shaderString );
		var shaderFile = shaderParser.Parse();

		// Debug name is used for error messages and internally by the SPIR-V compiler.
		var debugName = Path.GetFileNameWithoutExtension( compileInput.FilePath ) ?? "temp";

		var shader = new ShaderData();

		if ( shaderFile.Vertex != null )
			shader.VertexData = await CompileShader( shaderFile.Common, shaderFile.Vertex, ShaderStages.Vertex, debugName );

		if ( shaderFile.Fragment != null )
			shader.FragmentData = await CompileShader( shaderFile.Common, shaderFile.Fragment, ShaderStages.Fragment, debugName );

		return CompileResult.Success( JsonSerializer.SerializeToUtf8Bytes( shader ) );
	}
}
