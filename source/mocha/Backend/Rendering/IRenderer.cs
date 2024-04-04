using System.Drawing;

namespace Mocha.Rendering;

public interface IRenderer
{
	void DrawRectangle( RectangleF rectangle, uint color, float rounding = 0 );
	void DrawText( RectangleF rectangle, string text, int weight, float fontSize, uint color );
	System.Numerics.Vector2 CalcTextSize( string text, int weight, float fontSize );
}
