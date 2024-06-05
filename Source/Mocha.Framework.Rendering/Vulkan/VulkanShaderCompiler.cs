using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace Mocha.Rendering;

public record struct VulkanShaderInfo( byte[] Vertex, byte[] Fragment );
public record struct VulkanShaderSources( string Vertex, string Fragment );

public static class VulkanShaderCompiler
{
	private static byte[] CompileShader( string shaderSource, ShaderStages shaderStage, string debugName = "temp" )
	{
		// Prepend a preamble with GLSL version
		var preamble = new StringBuilder();
		preamble.AppendLine( $"#version 460" );
		preamble.AppendLine();
		shaderSource = preamble.ToString() + shaderSource;

		// Perform the compilation
		var compileOptions = new GlslCompileOptions( false );
		var compileResult = SpirvCompilation.CompileGlslToSpirv( shaderSource, $"{debugName}_{shaderStage}.glsl", shaderStage, compileOptions );

		var dataBytes = compileResult.SpirvBytes;
		return dataBytes;
	}

	/// <inheritdoc/>
	public static VulkanShaderInfo Compile( VulkanShaderSources shaderSources )
	{
		var vertexShader = File.ReadAllText( shaderSources.Vertex );
		var fragmentShader = File.ReadAllText( shaderSources.Fragment );

		// Debug name is used for error messages and internally by the SPIR-V compiler.
		var vertexDebugName = Path.GetFileNameWithoutExtension( shaderSources.Vertex );
		var fragmentDebugName = Path.GetFileNameWithoutExtension( shaderSources.Fragment );

		var shaderInfo = new VulkanShaderInfo
		{
			Vertex = CompileShader( vertexShader, ShaderStages.Vertex, vertexDebugName ),
			Fragment = CompileShader( fragmentShader, ShaderStages.Fragment, fragmentDebugName )
		};

		return shaderInfo;
	}
}
