using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mocha;

public class GameInfo
{
	public string Game { get; set; } = "Game";
	public string Title { get; set; } = "Game";
	public FileSystemInfo FileSystem { get; set; } = null!;

	[JsonIgnore]
	public static GameInfo Current { get; set; }

	static GameInfo()
	{
		// HACK: can't load FileSystem at this stage because it cause a circular
		// dependency so we'll just load directly from the disk
		var jsonContents = System.IO.File.ReadAllText( "./fs/.mocha/config/game.json" );
		Current = JsonSerializer.Deserialize<GameInfo>( jsonContents ) ?? throw new Exception( "Couldn't load 'game.json'?" );
	}
}
