namespace Mocha;

public static class Time
{
	public static float Delta { get; internal set; }
	public static float Now { get; internal set; }

	internal static void OnFrame( float deltaTime )
	{
		Delta = deltaTime;
		Now += deltaTime;
	}
}
