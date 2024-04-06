using System.Runtime.InteropServices;

namespace Mocha.Platforms;

public static class Windows
{
	[DllImport( "uxtheme.dll", SetLastError = true, CharSet = CharSet.Unicode )]
	static extern int SetWindowTheme( IntPtr hwnd, string pszSubAppName, string pszSubIdList );

	[DllImport( "dwmapi.dll", SetLastError = true )]
	static extern int DwmSetWindowAttribute( IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute );

	private static void SetDarkModeForWindow( nint windowHandle )
	{
		if ( windowHandle != IntPtr.Zero )
		{
			// Set dark mode for window
			if ( SetWindowTheme( windowHandle, "DarkMode_Explorer", null! ) != 0 )
				Log.Error( $"Failed to set DarkMode_Explorer window theme for {windowHandle}" );

			int darkMode = 1;
			if ( DwmSetWindowAttribute( windowHandle, 20, ref darkMode, sizeof( int ) ) != 0 )
				if ( DwmSetWindowAttribute( windowHandle, 19, ref darkMode, sizeof( int ) ) != 0 )
					Log.Error( $"Failed to set dark mode DWM attribute for {windowHandle}" );
		}
	}

	internal static void InitWindow( nint windowHandle )
	{
		Log.Trace( $"Windows.InitWindow: {windowHandle}" );
		SetDarkModeForWindow( windowHandle );
	}
}
