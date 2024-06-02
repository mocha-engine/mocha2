namespace Mocha;

public class Logger
{
	static Logger()
	{
		if ( !Console.IsOutputRedirected && Console.IsOutputRedirected )
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;
		}
	}

	private object _threadLock = new();
	private const string DateColor = "#0077c2";
	private const string InfoColor = "#dcdfe4";
	private const string TraceColor = "#adb0b6";
	private const string ErrorColor = "#e06c75";
	private const string WarningColor = "#e5c07b";

	public void Info( object obj ) => Log( "Info", obj, InfoColor );
	public void Warning( object obj ) => Log( "Warning", obj, WarningColor );
	public void Error( object obj ) => Log( "Error", obj, ErrorColor );
	public void Trace( object obj ) => Log( "Trace", obj, TraceColor );

	private void SetBackgroundColor( string hexColor )
	{
		var color = HexToAnsiRgb( hexColor );
		Console.Write( $"\x1b[48;2;{color}m" );
	}

	private void SetForegroundColor( string hexColor )
	{
		var color = HexToAnsiRgb( hexColor );
		Console.Write( $"\x1b[38;2;{color}m" );
	}

	private void ResetColors()
	{
		Console.Write( $"\x1b[0m" );
	}

	private void Log( string level, object obj, string color )
	{
		if ( string.IsNullOrEmpty( level ) || obj == null || string.IsNullOrEmpty( color ) )
			throw new ArgumentException( "Input parameters cannot be null or empty" );

		string dateTime = GetCurrentDateTime();

		lock ( _threadLock )
		{
			WriteLogMessage( level, obj, dateTime, color );
			IConsoleSystem.Current?.Log( level, $"{obj}" );
		}
	}

	private string GetCurrentDateTime()
	{
		return DateTime.Now.ToString( "HH:mm:ss" );
	}

	private void WriteLogMessage( string level, object obj, string dateTime, string color )
	{
		WriteColoredString( DateColor, "█");
		WriteColoredString( InfoColor, $" {dateTime} ", DateColor );
		WriteColoredString( DateColor, "█ ");
		WriteColoredString( color, $"{level,-12}{obj}\n" );

		ResetColors();
	}

	private string WriteColoredString( string foregroundColor, string text, string? backgroundColor = null )
	{
		SetForegroundColor( foregroundColor );

		if ( !string.IsNullOrEmpty( backgroundColor ) )
			SetBackgroundColor( backgroundColor );

		Console.Write( text );

		ResetColors();
		return text;
	}
	
	private static string HexToAnsiRgb( string hexColor )
	{
		if ( hexColor.StartsWith( "#" ) )
			hexColor = hexColor[1..];

		if ( hexColor.Length == 6 )
		{
			var r = Convert.ToInt32( hexColor.Substring( 0, 2 ), 16 );
			var g = Convert.ToInt32( hexColor.Substring( 2, 2 ), 16 );
			var b = Convert.ToInt32( hexColor.Substring( 4, 2 ), 16 );

			return $"{r};{g};{b}";
		}
		else
		{
			throw new ArgumentException( "Invalid hex color. Hex color should be in the form '#RRGGBB'" );
		}
	}
}
