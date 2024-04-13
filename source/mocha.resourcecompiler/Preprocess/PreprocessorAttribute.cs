namespace Mocha.ResourceCompiler;

internal class PreprocessorAttribute : Attribute
{
	public string Name { get; set; }

	public PreprocessorAttribute( string name )
	{
		Name = name;
	}
}
