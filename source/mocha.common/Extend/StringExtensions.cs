using System.IO;

public static class StringExtensions
{
	public static bool IsValid( this string? str )
	{
		if ( str == null )
			return false;

		if ( string.IsNullOrEmpty( str ) )
			return false;

		return true;
	}

	public static string NormalizePath( this string str )
	{
		return str.Replace( "\\", "/" );
	}
}
