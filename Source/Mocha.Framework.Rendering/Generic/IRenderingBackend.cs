﻿using System.Drawing;

namespace Mocha.Rendering;

[Obsolete]
internal interface IRenderingBackend : IDisposable
{
	public static IRenderingBackend Current { get; set; } = null!;

	void Render();
	void DrawRect( RectangleF rect );
}
