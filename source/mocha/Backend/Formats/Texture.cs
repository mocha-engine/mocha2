using Mocha.Rendering;

namespace Mocha;

public class Texture : IDisposable
{
	private FileHandle? FileHandle { get; set; }
	private ITexture Resource { get; set; }
	
	public Texture( string texturePath )
	{
		FileHandle = new FileHandle( texturePath );
		FileHandle.FileChanged += async () =>
		{
			var textureData = await FileHandle.ReadDataAsync();
			UpdateTextureData( textureData );
		};

		Resource = Factory.CreateResource<ITexture>();

		var initialTextureData = FileHandle.ReadDataAsync().Result;
		UpdateTextureData( initialTextureData );
	}

	/// <summary>
	/// Call this when the texture data changes
	/// </summary>
	private void UpdateTextureData( byte[] textureData )
	{
		var textureResource = TextureData.Load( textureData );
		var textureCreateInfo = TextureCreateInfo.CreateFrom( textureResource );
		
		Resource.Create( textureCreateInfo );
	}

	/// <summary>
	/// Call this to clean up this texture
	/// </summary>
	public void Dispose()
	{
		FileHandle?.Dispose();
	}

	// Placeholder for actual implementation
	private void SetTexture( object newTexture ) { }

	// Placeholder for actual implementation
	private object ConvertByteArrayToTexture( byte[] textureData ) { return null; }
}
