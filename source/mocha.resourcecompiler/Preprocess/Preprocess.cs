using System.Reflection;
using System.Text.Json;

namespace Mocha.ResourceCompiler;

/// <summary>
/// <para>
/// Applies pre-processing to disk files before we cache and process them.
/// </para>
/// <para>
/// You should have a "preprocess.json" file containing the details for a (series of) preprocessing step(s).
/// </para>
/// </summary>
public static class Preprocessor
{
	private static readonly Dictionary<string, Type> Preprocessors = new();

	static Preprocessor()
	{
		foreach ( var type in Assembly.GetExecutingAssembly().GetTypes() )
		{
			var attr = type.GetCustomAttribute<PreprocessorAttribute>();

			if ( attr == null )
				continue;

			Preprocessors.Add( attr.Name, type );
		}
	}

	public static void Run( string rootDir )
	{		
		string filePath = Path.Combine( rootDir, "preprocess.json" );

		if ( !File.Exists( filePath ) )
			return;

		var fileContents = File.ReadAllText( filePath );
		var list = JsonSerializer.Deserialize<PreprocessItem[]>( fileContents )!;

		foreach ( var item in list )
		{
			if ( !Preprocessors.TryGetValue( item.Preprocessor, out var preprocessorType ) )
			{
				throw new( $"Preprocessor '{item.Preprocessor}' not found" );
			}

			var sourceDirectory = Path.Combine( rootDir, item.Folder[1..] );
			var targetDirectory = Path.Combine( rootDir, item.Destination[1..] );

			preprocessorType.GetMethod( "ProcessDirectory" ).Invoke( null, new[] { sourceDirectory, targetDirectory } );
		}
	}
}
