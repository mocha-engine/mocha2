namespace Mocha;

public class FileHandle : IDisposable
{
	public event Action? FileChanged;
	private readonly FileSystemWatcher _watcher;
	private readonly string _filePath;
	private byte[]? _fileDataCache;
	private readonly SemaphoreSlim _asyncLock = new SemaphoreSlim( 1, 1 );

	public FileHandle( FileSystem fs, string path )
	{
		if ( !fs.Exists( path ) )
		{
			throw new ArgumentException( "Path is null or empty, or file does not exist on filesystem.", nameof( path ) );
		}

		_filePath = path;

		var directoryName = Path.GetDirectoryName( path )!;
		var fileName = Path.GetFileName( path );

		_watcher = fs.Watch( directoryName, fileName, OnFileWatcherChanged, NotifyFilters.LastWrite );

		// initialize cache on startup
		UpdateCache().Wait();
	}

	public async Task<byte[]> ReadDataAsync()
	{
		await _asyncLock.WaitAsync();
		try
		{
			// return a copy to prevent modification
			var cache = _fileDataCache ?? [];
			return (byte[])cache.Clone();
		}
		finally
		{
			_asyncLock.Release();
		}
	}

	private async void OnFileWatcherChanged()
	{
		await _asyncLock.WaitAsync();
		try
		{
			await UpdateCache();
			FileChanged?.Invoke();
		}
		finally
		{
			_asyncLock.Release();
		}
	}

	private async Task UpdateCache()
	{
		try
		{
			_fileDataCache = await FileSystem.Content.ReadAllBytesAsync( _filePath );
		}
		catch ( IOException e )
		{
			Log.Error( e );
			throw;
		}
	}

	public void Dispose()
	{
		_watcher.Dispose();
		FileChanged = null;
		_asyncLock.Dispose();
	}
}
