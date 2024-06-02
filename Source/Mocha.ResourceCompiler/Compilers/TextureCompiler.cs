using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.Json;

namespace Mocha.ResourceCompiler;

[Compiler( new[] { ".png", ".jpg" }, ".texture", AutoCompile = false )]
public class TextureCompiler : IAssetCompiler
{
	/// <inheritdoc />
	public async Task<CompileResult> CompileFile( CompileInput compileInput )
	{
		using var stream = new MemoryStream( compileInput.RawData );
		using Image<Rgba32> image = await Image.LoadAsync<Rgba32>( stream );
		BcEncoder encoder = new();

		encoder.OutputOptions.GenerateMipMaps = true;
		encoder.OutputOptions.Quality = CompressionQuality.Balanced;
		encoder.OutputOptions.Format = BCnEncoder.Shared.CompressionFormat.Bc3;
		encoder.OutputOptions.FileFormat = BCnEncoder.Shared.OutputFileFormat.Dds;

		var data = await encoder.EncodeToRawBytesAsync( image );

		var textureData = new TextureData()
		{
			Data = data,
			Width = (uint)image.Width,
			Height = (uint)image.Height,
			MipCount = (uint)data.Length,
			Format = TextureFormat.Bc3,
		};

		return CompileResult.Success( JsonSerializer.SerializeToUtf8Bytes( textureData ) );
	}
}
