namespace Mocha.ResourceCompiler;

public class CachedFile
{
	public string Path;
	public byte[]? RawData = null;
	public bool IsThin => RawData == null;

	public CachedFile( string path, byte[]? rawData = null )
	{
		Path = path;
		RawData = rawData;
	}

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

	public void LoadFull()
	{
		if ( !IsThin )
			return;

		var path = Path;

		RawData = FileSystem.ContentSrc.ReadAllBytes( path );
	}
}

