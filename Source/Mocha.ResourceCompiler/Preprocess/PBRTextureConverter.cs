using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace Mocha.ResourceCompiler;

[Preprocessor( "pbrconvert" )]
public static class PbrTextureConverter
{
	public static void ProcessDirectory( string sourceDirectory, string destinationDirectory )
	{
		var colorMapFiles = Directory.EnumerateFiles( sourceDirectory, "*_color.png", new EnumerationOptions { RecurseSubdirectories = true } );

		foreach ( var colorMapFile in colorMapFiles )
		{
			ConvertTextureMaps( colorMapFile, destinationDirectory );
		}
	}

	private static Mat LoadImageOrDefault( string filePath, int width, int height, ImreadModes mode, int defaultValue )
	{
		return File.Exists( filePath ) ? new Mat( filePath, mode ) : CreateDefaultMat( width, height, mode, defaultValue );
	}

	private static Mat LoadImageOrDefault( string filePath, int width, int height, ImreadModes mode, int defaultB, int defaultG, int defaultR )
	{
		return File.Exists( filePath ) ? new Mat( filePath, mode ) : CreateDefaultMat( width, height, mode, defaultB, defaultG, defaultR );
	}

	private static Mat CreateDefaultMat( int width, int height, ImreadModes mode, int defaultB, int defaultG, int defaultR )
	{
		var defaultMat = new Mat( width, height, DepthType.Cv8U, 3 );
		defaultMat.SetTo( new MCvScalar( defaultB, defaultG, defaultR ) );
		return defaultMat;
	}

	private static Mat CreateDefaultMat( int width, int height, ImreadModes mode, int defaultValue )
	{
		var defaultMat = new Mat( width, height, DepthType.Cv8U, 1 );
		defaultMat.SetTo( new MCvScalar( defaultValue ) );
		return defaultMat;
	}

	private static void ConvertTextureMaps( string colorMapFile, string destinationDirectory )
	{
		var baseMaterialName = Path.GetFileNameWithoutExtension( colorMapFile ).Replace( "_color", "" );
		var sourcePath = Path.GetDirectoryName( colorMapFile ) ?? "";
		var materialPath = Path.Combine( sourcePath, baseMaterialName );

		Directory.CreateDirectory( destinationDirectory );
		var destinationPath = Path.Combine( destinationDirectory, baseMaterialName );

		var baseColor = new Mat( colorMapFile, ImreadModes.Color );

		var roughness = LoadRoughnessMap( materialPath, baseColor.Width, baseColor.Height );
		var ao = LoadImageOrDefault( materialPath + "_ao.png", baseColor.Width, baseColor.Height, ImreadModes.Grayscale, 255 );
		var normal = LoadImageOrDefault( materialPath + "_normal.png", baseColor.Width, baseColor.Height, ImreadModes.Color, 127, 127, 255 );

		var shininess = InvertColorMap( roughness );
		var specular = LoadImageOrDefault( materialPath + "_metal.png", baseColor.Width, baseColor.Height, ImreadModes.Grayscale, 0 );

		var packedTexture = CreatePackedTexture( shininess, specular, ao );

		SaveTextures( baseColor, packedTexture, normal, destinationPath );
		WriteMaterialFile( destinationPath );
	}

	private static Mat LoadRoughnessMap( string materialPath, int width, int height )
	{
		Mat roughness;
		if ( File.Exists( materialPath + "_rough.png" ) )
		{
			roughness = LoadImageOrDefault( materialPath + "_rough.png", width, height, ImreadModes.Grayscale, 127 );
		}
		else
		{
			roughness = LoadImageOrDefault( materialPath + "_smooth.png", width, height, ImreadModes.Grayscale, 127 );
			CvInvoke.BitwiseNot( roughness, roughness );
		}

		return roughness;
	}

	private static Mat InvertColorMap( Mat sourceMat )
	{
		var invertedMat = new Mat();
		CvInvoke.BitwiseNot( sourceMat, invertedMat );
		return invertedMat;
	}

	private static Mat CreatePackedTexture( Mat shininess, Mat specular, Mat ao )
	{
		var packedTexture = new Mat();
		VectorOfMat vectorOfMat = new VectorOfMat( ao, specular, shininess );
		CvInvoke.Merge( vectorOfMat, packedTexture );
		return packedTexture;
	}

	private static void SaveTextures( Mat baseColor, Mat packedTexture, Mat normal, string destinationPath )
	{
		baseColor.Save( destinationPath + "_base.png" );
		packedTexture.Save( destinationPath + "_packed.png" );
		normal.Save( destinationPath + "_normal.png" );
	}

	private static void WriteMaterialFile( string destinationPath )
	{
		var assetPath = FileSystem.ContentSrc.GetRelativePath( destinationPath );

		var materialData = new MaterialAsset()
		{
			DiffuseTexture = $"{assetPath.NormalizePath()}_diffuse.png",
			NormalTexture = $"{assetPath.NormalizePath()}_normal.png",
			PackedTexture = $"{assetPath.NormalizePath()}_packed.png"
		};

		var fileData = Serializer.Serialize( materialData );
		File.WriteAllBytes( destinationPath + ".material.json", fileData );
	}
}
