namespace Mocha;

public class Logger
{
	private void Log( string level, object obj, ConsoleColor color )
	{
		var dateTime = DateTime.Now.ToString( "HH:mm:ss" );

		Console.ForegroundColor = ConsoleColor.DarkGray; 
		Console.Write( $"{dateTime,-12}" );

		Console.ForegroundColor = color;
		Console.Write( $"{level,-12}" );

		Console.ForegroundColor = ConsoleColor.Gray;
		Console.WriteLine( $"{obj}" );
	}

	public void Info( object obj ) => Log( "Info", obj, ConsoleColor.White );
	public void Warning( object obj ) => Log( "Warning", obj, ConsoleColor.Yellow );
	public void Error( object obj ) => Log( "Error", obj, ConsoleColor.Red );
	public void Trace( object obj ) => Log( "Trace", obj, ConsoleColor.Gray );
}
