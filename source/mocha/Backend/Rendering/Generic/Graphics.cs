using System.Drawing;

namespace Mocha.Rendering;

public static class Graphics
{
	public static void DrawRect( RectangleF rect )
	{
		IRenderingBackend.Current.DrawRect( rect );
	}
}
