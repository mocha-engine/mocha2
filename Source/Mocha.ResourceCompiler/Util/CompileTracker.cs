namespace Mocha.ResourceCompiler;

public class CompileTracker
{
	private int _successCount = 0;
	private int _failCount = 0;
	
	public void IsCompiled( string path )
	{
		_successCount++;
		Log.Info( $"✅ Compiled '{path}'" );
	}

	public void IsFailed( string path, string error )
	{
		_failCount++;
		Log.Error( $"⚠️ Couldn't compile '{path}': {error}" );
	}

	public void IsProcessing( string type, string path )
	{
		Log.Info( $"⏳ Processing '{path}' with {type}" );
	}

	public void DisplayResults()
	{
		Log.Info( $"🏁 Build: {_successCount} succeeded, {_failCount} failed" );
	}
}
