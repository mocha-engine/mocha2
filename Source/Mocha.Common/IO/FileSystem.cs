using System.Text;

namespace Mocha;

public class FileSystem
{
	/// <summary>
	/// Used to fetch configuration data (usually in TOML format).
	/// </summary>
	internal static FileSystem Config { get; } = new FileSystem( "./fs/mocha/config/" );
	
	public static FileSystem Content { get; } = new FileSystem( GameInfo.Current.FileSystem.MountPaths.Content );
	internal static FileSystem ContentSrc { get; } = new FileSystem( GameInfo.Current.FileSystem.MountPaths.Source );
	private string BasePath { get; }
	
	private List<FileSystemWatcher> _watchers = new();

	public FileSystem( string relativePath )
	{
		Log.Trace( $"Mounting {relativePath}" );
		BasePath = Path.GetFullPath( relativePath, Directory.GetCurrentDirectory() );
	}

	public string GetAbsolutePath( string relativePath, bool ignorePathNotFound = false )
	{
		if ( relativePath.StartsWith( "/" ) )
			relativePath = relativePath[1..];

		var path = Path.Combine( BasePath, relativePath );
		return path;
	}

	public string GetRelativePath( string absolutePath )
	{
		var path = Path.GetRelativePath( BasePath, absolutePath );
		if ( !path.StartsWith( "/" ) )
			path = "/" + path;

		return path;
	}

	public string ReadAllText( string relativePath )
	{
		return Encoding.ASCII.GetString( ReadAllBytes( relativePath ) );
	}

	[Obsolete( "Use FileHandle" )]
	public byte[] ReadAllBytes( string relativePath )
	{
		var stream = InternalOpenFile( relativePath );
		var bytes = new byte[stream.Length];
		stream.Read( bytes, 0, (int)stream.Length );

		return bytes;
	}

	public void WriteAllText( string relativePath, string data )
	{
		WriteAllBytes( relativePath, Encoding.ASCII.GetBytes( data ) );
	}

	public void WriteAllBytes( string relativePath, byte[] data )
	{
		var absolutePath = GetAbsolutePath( relativePath );
		File.Delete( absolutePath );

		var dir = Path.GetDirectoryName( absolutePath );
		Directory.CreateDirectory( dir );

		var stream = InternalOpenFileWrite( relativePath );
		stream.Write( data, 0, data.Length );
	}

	public Stream OpenRead( string relativePath )
	{
		return InternalOpenFile( GetAbsolutePath( relativePath ) );
	}

	public void CreateDirectory( string relativePath )
	{
		Directory.CreateDirectory( GetAbsolutePath( relativePath ) );
	}

	public string[] GetDirectories( string relativePath )
	{
		var dirs = InternalGetDirectories( GetAbsolutePath( relativePath ) );
		return dirs.ToArray();
	}

	public string[] GetFiles( string relativePath )
	{
		var files = InternalGetFiles( GetAbsolutePath( relativePath ) );
		return files.ToArray();
	}

	/// <summary>
	/// Handles enumerating through directories
	/// </summary>
	private string[] InternalGetDirectories( string relativePath )
	{
		var absolutePath = GetAbsolutePath( relativePath );
		return Directory.GetDirectories( absolutePath ).Select( x => x.NormalizePath() ).ToArray();
	}

	/// <summary>
	/// Handles enumerating through files
	/// </summary>
	private string[] InternalGetFiles( string relativePath )
	{
		var absolutePath = GetAbsolutePath( relativePath );
		return Directory.GetFiles( absolutePath ).Select( x => x.NormalizePath() ).ToArray();
	}

	/// <summary>
	/// Handles opening a file
	/// </summary>
	private Stream InternalOpenFileWrite( string relativePath )
	{
		var absolutePath = GetAbsolutePath( relativePath );
		return File.OpenWrite( absolutePath );
	}

	/// <summary>
	/// Handles opening a file
	/// </summary>
	private Stream InternalOpenFile( string relativePath )
	{
		var absolutePath = GetAbsolutePath( relativePath );
		return File.OpenRead( absolutePath );
	}

	public bool Exists( string relativePath )
	{
		return File.Exists( GetAbsolutePath( relativePath ) );
	}

	public FileSystemWatcher Watch( string relativeDir, string filter, Action onChange, NotifyFilters? filters = null )
	{
		var directoryName = GetAbsolutePath( relativeDir );
		var watcher = new FileSystemWatcher( directoryName, filter );

		watcher.IncludeSubdirectories = true;
		watcher.NotifyFilter = filters ?? (NotifyFilters.Attributes
							 | NotifyFilters.CreationTime
							 | NotifyFilters.DirectoryName
							 | NotifyFilters.FileName
							 | NotifyFilters.LastAccess
							 | NotifyFilters.LastWrite
							 | NotifyFilters.Security
							 | NotifyFilters.Size);

		watcher.EnableRaisingEvents = true;
		watcher.Changed += ( _, e ) =>
		{
			onChange();
		};

		_watchers.Add( watcher );

		return watcher;
	}

	public Task<byte[]> ReadAllBytesAsync( string filePath )
	{
		return File.ReadAllBytesAsync( GetAbsolutePath( filePath ) );
	}
}
