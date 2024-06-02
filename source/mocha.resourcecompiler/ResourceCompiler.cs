using System.Reflection;

namespace Mocha.ResourceCompiler;
using FileQueue = List<CachedFile>;

public static class ResourceCompiler
{
	private static List<IAssetCompiler> compilers = new();

	private static void RegisterCompilers()
	{
		var iAssetCompilerType = typeof( IAssetCompiler );
		foreach ( var type in Assembly.GetCallingAssembly().GetTypes().Where( x => iAssetCompilerType.IsAssignableFrom( x ) && !x.IsInterface ) )
		{
			var instance = Activator.CreateInstance( type ) as IAssetCompiler;

			if ( instance != null )
				compilers.Add( instance );
		}
	}

	private static FileQueue GetFileQueue()
	{
		FileQueue queue = new();

		foreach ( var compiler in compilers )
		{
			foreach ( var extension in compiler.GetType().GetCustomAttribute<CompilerAttribute>()?.SourceExtensions! )
			{
				var matchingFiles = CachedFileSystem.GetAllFilesWithExtension( extension );
				queue.AddRange( matchingFiles );
			}
		}

		return queue;
	}

	private static async Task CompileEverythingAsync( FileQueue queue )
	{
		foreach ( var item in queue )
		{
			await CompileFileAsync( item );
		}
	}

	public static void Run( Options options )
	{
		using ( _ = new Stopwatch( "Set up compilers" ) )
		{
			RegisterCompilers();
		}

		FileQueue queue;

		string sourcePath = GameInfo.Current.FileSystem.MountPaths.Source;

		if ( options.Preprocess )
		{
			using ( _ = new Stopwatch( "Preprocess files on disk" ) )
			{
				Preprocessor.Run( sourcePath );
			}
		}
		else
		{
			Log.Info( "Skipping preprocess step..." );
		}

		using ( _ = new Stopwatch( "Create queue" ) )
		{
			queue = GetFileQueue();
		}

		using ( _ = new Stopwatch( "Load full filesystem cache for queued files" ) )
		{
			foreach ( var file in queue )
			{
				file.LoadFull();
			}
		}

		using ( _ = new Stopwatch( "Full asset compile" ) )
		{
			TaskPool<CachedFile>.Dispatch( queue, CompileEverythingAsync ).Then( Global.CompileTracker.DisplayResults );
		}
	}

	private static bool GetCompilerForFile( CachedFile file, out IAssetCompiler? foundCompiler, out CompilerAttribute? compilerAttribute )
	{
		var fileExtension = Path.GetFileName( file.Path );
		fileExtension = fileExtension[fileExtension.IndexOf( "." )..];

		foreach ( var compiler in compilers )
		{
			if ( compiler.GetType().GetCustomAttribute<CompilerAttribute>()?.SourceExtensions?.Contains( fileExtension ) ?? false )
			{
				compilerAttribute = compiler.GetType().GetCustomAttribute<CompilerAttribute>();
				foundCompiler = compiler;
				return true;
			}
		}

		compilerAttribute = null;
		foundCompiler = null;
		return false;
	}

	public static async Task CompileFileAsync( CachedFile file )
	{
		if ( !GetCompilerForFile( file, out var compiler, out var compilerAttribute ) )
			return;

		CompileResult result = new();
		Global.CompileTracker.IsProcessing( compiler!.GetType().Name, file.Path.NormalizePath() );

		try
		{
			result = await compiler!.CompileFile( new CompileInput( file!.RawData!, file.Path ) );
		}
		catch ( Exception ex )
		{
			result = CompileResult.Fail( ex.Message );
		}
		finally
		{
			if ( result.WasSuccess )
			{
				var destPath = file.Path.NormalizePath();
				destPath = destPath[..destPath.IndexOf(".")];
				destPath += compilerAttribute!.OutputExtension;
				
				FileSystem.Content.CreateDirectory( Path.GetDirectoryName( destPath )?.NormalizePath() ?? "" );
				FileSystem.Content.WriteAllBytes( destPath, result!.CompileData! );

				Global.CompileTracker.IsCompiled( file.Path.NormalizePath() );
			}
			else
			{
				Global.CompileTracker.IsFailed( file.Path.NormalizePath(), result.ErrorMessage ?? "none" );
			}
		}
	}
}
