using Mocha;
using Mocha.ResourceCompiler;

public static class CachedFileSystem
{
	private static readonly Dictionary<string, CachedFile> FileSystem = new();

	static CachedFileSystem()
	{
		AddDirectory( "" );
	}

	public static bool TryGetFile( string path, out CachedFile file )
	{
		return FileSystem.TryGetValue( path, out file );
	}

	private static void AddFile( string path, bool isThin )
	{
		try
		{
			FileSystem.Add( path, CachedFile.FromFile( path, isThin ) );
		}
		catch ( ArgumentException )
		{
			Log.Error( "Couldn't add file" );
		}
	}

	private static void AddDirectory( string path, bool recursive = true, bool isThin = true )
	{
		void ProcessDirectory( string dir )
		{
			foreach ( var file in Mocha.FileSystem.ContentSrc.GetFiles( dir ) )
			{
				AddFile( file, isThin );
			}

			if ( recursive )
			{
				foreach ( var subDir in Mocha.FileSystem.ContentSrc.GetDirectories( dir ) )
				{
					ProcessDirectory( subDir );
				}
			}
		}

		ProcessDirectory( path );
	}

	public static IEnumerable<CachedFile> GetAllFilesWithExtension( string extension = ".txt" )
	{
		return FileSystem
			.Where( pair => pair.Key.EndsWith( extension, StringComparison.OrdinalIgnoreCase ) )
			.Select( pair => pair.Value );
	}

	public static byte[] GetData( string filePath )
	{
		if ( FileSystem.ContainsKey( filePath ) )
		{
			FileSystem[filePath].LoadFull();
			return FileSystem[filePath].RawData ?? Array.Empty<byte>();
		}

		throw new FileNotFoundException( $"File at path {filePath} not found in cached file system." );
	}
}
