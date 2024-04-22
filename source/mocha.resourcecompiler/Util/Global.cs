global using static Mocha.ResourceCompiler.Global;

namespace Mocha.ResourceCompiler;

public static class Global
{
	public static CompileTracker CompileTracker { get; set; } = new();
	public static Logger Log { get; set; } = new();
}
