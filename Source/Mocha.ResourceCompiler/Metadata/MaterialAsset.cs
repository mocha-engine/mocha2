namespace Mocha.ResourceCompiler;

public sealed class MaterialAsset : AssetMetadata
{
	public string? DiffuseTexture { get; set; }
	public string? NormalTexture { get; set; }
	public string? PackedTexture { get; set; }

	public List<string> GetAllTextures()
	{
		var textureProperties = this.GetType()
			.GetProperties()
			.Where( p => p.PropertyType == typeof( string ) && p.Name.Contains( "Texture" ) );

		var textureValues = new List<string>();

		foreach ( var prop in textureProperties )
		{
			var value = prop.GetValue( this ) as string;

			if ( value.IsValid() )
				textureValues.Add( value! );
		}

		return textureValues;
	}
}
