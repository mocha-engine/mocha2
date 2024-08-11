namespace Mocha.ResourceCompiler;

/// <summary>
/// Represents a cached file in the resource compiler system.
/// </summary>
public class CachedFile
{
	/// <summary>
	/// Gets or sets the relative path of the file.
	/// </summary>
	public string Path;

	/// <summary>
	/// Gets or sets the raw byte data of the file. Null if the file is thin.
	/// </summary>
	public byte[]? RawData = null;

	/// <summary>
	/// Gets a value indicating whether this is a thin file (i.e., no raw data loaded).
	/// </summary>
	public bool IsThin => RawData == null;

	/// <summary>
	/// Initializes a new instance of the CachedFile class.
	/// </summary>
	/// <param name="path">The relative path of the file.</param>
	/// <param name="rawData">Optional raw byte data of the file.</param>
	public CachedFile( string path, byte[]? rawData = null )
	{
		Path = path;
		RawData = rawData;
	}

	/// <summary>
	/// Creates a CachedFile instance from a file on disk.
	/// </summary>
	/// <param name="path">The path to the file.</param>
	/// <param name="isThin">Whether to create a thin file (without loading raw data).</param>
	/// <returns>A new CachedFile instance.</returns>
	public static CachedFile FromFile( string path, bool isThin )
	{
		path = FileSystem.ContentSrc.GetRelativePath( path );

		if ( isThin )
		{
			return new CachedFile( path );
		}

		var cachedFile = new CachedFile( path );
		cachedFile.LoadFull();
		return cachedFile;
	}

	/// <summary>
	/// Loads the full content of the file if it's currently thin.
	/// </summary>
	public void LoadFull()
	{
		if ( !IsThin )
			return;

		var path = Path;

		RawData = FileSystem.ContentSrc.ReadAllBytes( path );
	}
}
