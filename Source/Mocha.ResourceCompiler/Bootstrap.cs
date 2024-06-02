using CommandLine;
using Mocha.ResourceCompiler;

public class Options
{
	[Option( 'p', "preprocess", Required = false )]
	public bool Preprocess { get; set; } = false;
}

internal class Program
{
	static void Main( string[] args )
	{
		Parser.Default.ParseArguments<Options>( args ).WithParsed( o =>
		{
			using ( _ = new Stopwatch( "Total elapsed time" ) )
			{
				ResourceCompiler.Run( o );
			}
		} );
	}
}
