namespace Mocha;

public static class MathX
{
	public static int CeilToInt( this float x ) => (int)Math.Ceiling( x );
	public static int FloorToInt( this float x ) => (int)Math.Floor( x );
	public static int RoundToInt( this float x ) => (int)Math.Round( x );

	public static float DegreesToRadians( this float degrees ) => degrees * 0.0174533f;
	public static float RadiansToDegrees( this float radians ) => radians * 57.2958f;

	public static float Clamp( this float v, float min, float max )
	{
		if ( min > max )
			return max;
		if ( v < min )
			return min;
		return v > max ? max : v;
	}

	public static float LerpTo( this float a, float b, float t )
	{
		return a * (1 - t) + b * t.Clamp( 0, 1 );
	}

	public static float LerpInverse( this float t, float a, float b )
	{
		return ((t - a) / (b - a)).Clamp( 0, 1 );
	}
}
