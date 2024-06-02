using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class PathPropertySourceGenerator : ISourceGenerator
{
	public void Initialize( GeneratorInitializationContext context )
	{
		context.RegisterForSyntaxNotifications( () => new SyntaxReceiver() );
	}

	public void Execute( GeneratorExecutionContext context )
	{
		if ( context.SyntaxReceiver is not SyntaxReceiver receiver )
			return;

		foreach ( var propertyDeclaration in receiver.CandidateProperties )
		{
			string propertyName = propertyDeclaration.Identifier.Text;
			string className = (propertyDeclaration.Parent as ClassDeclarationSyntax)?.Identifier.Text ?? "Unnamed";

			string source = @$"
namespace Mocha.ResourceCompiler
{{
    public partial class {className}
    {{
        public string? {propertyName}Path {{ get; set; }}
    }}
}}";
			context.AddSource( $"{className}_{propertyName}Path", source );
		}
	}

	private class SyntaxReceiver : ISyntaxReceiver
	{
		public List<PropertyDeclarationSyntax> CandidateProperties { get; } = new();

		public void OnVisitSyntaxNode( SyntaxNode syntaxNode )
		{
			// Any field with the attribute Path will trigger the source generation
			if ( syntaxNode is PropertyDeclarationSyntax propertyDeclaration
				&& propertyDeclaration.AttributeLists.Count > 0
				&& propertyDeclaration.AttributeLists.Any( a => a.Attributes.Any( at => at.Name.ToString() == "Path" ) ) )
			{
				CandidateProperties.Add( propertyDeclaration );
			}
		}
	}
}
