namespace Mocha;

/// <summary>
/// Provides a basic interface for a console system, used by <c>mocha.console</c>.
/// </summary>
public interface IConsoleSystem
{
	public static IConsoleSystem? Current { get; set; }

	/// <summary>
	/// Initialise the console system.
	/// </summary>
	void Init();

	/// <summary>
	/// Log something to the console system.
	/// </summary>
	void Log( string level, string message );
}
