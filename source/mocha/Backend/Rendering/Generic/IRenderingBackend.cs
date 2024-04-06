using System.Drawing;

namespace Mocha.Rendering;

internal interface IRenderingBackend : IDisposable
{
	public static IRenderingBackend Current { get; set; }

	void Render();
	void DrawRect( RectangleF rect );
}
