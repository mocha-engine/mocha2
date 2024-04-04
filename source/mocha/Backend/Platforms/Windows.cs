using Mocha.Rendering;
using System.Runtime.InteropServices;

namespace Mocha.Platforms;

public static class Windows
{
	[DllImport( "user32.dll", SetLastError = true )]
	static extern IntPtr FindWindow( string lpClassName, string lpWindowName );

	[DllImport( "uxtheme.dll", SetLastError = true, CharSet = CharSet.Unicode )]
	static extern int SetWindowTheme( IntPtr hwnd, string pszSubAppName, string pszSubIdList );

	[DllImport( "dwmapi.dll", SetLastError = true )]
	static extern int DwmSetWindowAttribute( IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute );

	private static void SetDarkModeForWindow( Window window )
	{
		var windowHandle = FindWindow( null!, window.Title );

		if ( windowHandle != IntPtr.Zero )
		{
			// Set dark mode for window
			if ( SetWindowTheme( windowHandle, "DarkMode_Explorer", null! ) != 0 )
				throw new();

			int darkMode = 1;
			if ( DwmSetWindowAttribute( windowHandle, 20, ref darkMode, sizeof( int ) ) != 0 )
				if ( DwmSetWindowAttribute( windowHandle, 19, ref darkMode, sizeof( int ) ) != 0 )
					throw new();
		}
	}

	internal static void InitWindow( Window window )
	{
		SetDarkModeForWindow( window );
	}
}
