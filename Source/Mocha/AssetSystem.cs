/// <summary>
/// Represents a struct for asset paths.
/// </summary>
public struct AssetPath
{
	/// <summary>
	/// Gets or sets the source file path.
	/// </summary>
	public string SourceFilePath { get; set; }

	/// <summary>
	/// Gets or sets the compiled file path.
	/// </summary>
	public string CompiledFilePath { get; set; }

	/// <summary>
	/// Implicitly converts an AssetPath to a string.
	/// </summary>
	/// <param name="assetPath">The AssetPath instance.</param>
	/// <returns>The compiled file path.</returns>
	public static implicit operator string( AssetPath assetPath )
	{
		return assetPath.CompiledFilePath;
	}

	/// <summary>
	/// Implicitly converts a string to an AssetPath.
	/// </summary>
	/// <param name="compiledFilePath">The compiled file path.</param>
	/// <returns>An AssetPath instance with the specified compiled file path.</returns>
	public static implicit operator AssetPath( string compiledFilePath )
	{
		return new AssetPath
		{
			CompiledFilePath = compiledFilePath
		};
	}
}

/// <summary>
/// Provides methods for managing assets.
/// </summary>
public static class AssetSystem
{
	/// <summary>
	/// Determines whether the specified asset is up to date.
	/// </summary>
	/// <param name="assetPath">The path of the asset.</param>
	/// <returns>true if the asset is up to date; otherwise, false.</returns>
	public static bool IsUpToDate( AssetPath assetPath )
	{
		return File.Exists( assetPath.CompiledFilePath );
	}
}
