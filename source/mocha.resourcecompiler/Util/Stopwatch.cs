namespace Mocha.ResourceCompiler;

public class Stopwatch : IDisposable
{
	private readonly DateTime _startTime;
	private bool _disposed;
	private string _title;

	public Stopwatch( string title )
	{
		_title = title;
		_startTime = DateTime.UtcNow;
		_disposed = false;
	}

	public void Dispose()
	{
		Dispose( true );
		GC.SuppressFinalize( this );
	}

	protected virtual void Dispose( bool disposing )
	{
		if ( !_disposed )
		{
			if ( disposing )
			{
				var duration = DateTime.UtcNow - _startTime;
				var totalMilliseconds = duration.TotalMilliseconds;

				Log.Trace( $"{_title}: {totalMilliseconds} ms" );
			}
			_disposed = true;
		}
	}
}
