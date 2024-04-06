namespace Mocha;

public static class Debug
{
	public static bool Assert( bool condition, string message )
	{
		if ( !condition )
			Log.Error( $"{message}" );

		return condition;
	}

	public static bool AssertThrow( bool condition, string message )
	{
		if ( !condition )
			throw new Exception( message );

		return condition;
	}
}
