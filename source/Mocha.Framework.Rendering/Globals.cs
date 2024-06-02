global using Vector2Int = Silk.NET.Maths.Vector2D<int>;
global using Vector3Int = Silk.NET.Maths.Vector3D<int>;

global using Vector4 = Silk.NET.Maths.Vector4D<float>;
global using Vector4Int = Silk.NET.Maths.Vector4D<int>;

global using Rotation = Silk.NET.Maths.Quaternion<float>;

global using Mocha;

global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using static Globals;

using Mocha.Rendering;

public static class Globals
{
	public static Logger Log { get; } = new();

	internal static IRenderContext Render => IRenderContext.Current;
}
