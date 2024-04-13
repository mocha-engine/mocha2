namespace Mocha.ResourceCompiler;

[AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
public class CompilerAttribute : Attribute
{
	public string[] SourceExtensions { get; init; }
	public string OutputExtension { get; init; }
	public bool AutoCompile { get; init; } = true;

	public CompilerAttribute( string[] sourceExtensions, string outputExtension )
	{
		SourceExtensions = sourceExtensions;
		OutputExtension = outputExtension;
	}
}
