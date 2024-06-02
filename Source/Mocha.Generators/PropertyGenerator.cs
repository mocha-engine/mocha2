using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace Mocha.Generators
{
	[Generator]
	public class PropertyGenerator : ISourceGenerator
	{
		public void Initialize( GeneratorInitializationContext context )
		{
			// Nothing to do here
		}

		private static string GetPropertyName( string fieldName )
		{
			var sb = new StringBuilder();

			for ( int i = 0; i < fieldName.Length; i++ )
			{
				char c = fieldName[i];
				
				if ( c == '_' )
				{
					sb.Append( char.ToUpper( fieldName[i + 1] ) );
					i++;
				}
				else
				{
					sb.Append( c );
				}
			}
			
			return sb.ToString();
		}

		public void Execute( GeneratorExecutionContext context )
		{
			foreach ( var syntaxTree in context.Compilation.SyntaxTrees )
			{
				// Find all fields marked as [WithProperty]
				var propertyFields = syntaxTree.GetRoot()
												.DescendantNodes()
												.OfType<FieldDeclarationSyntax>()
												.Where( s => s.AttributeLists.Any( al => al.Attributes.Any( a => a.Name.ToString() == "WithProperty" ) ) );

				var semanticModel = context.Compilation.GetSemanticModel( syntaxTree );
				
				// Iterate over the resource structs and generate code
				foreach ( var field in propertyFields )
				{
					var fieldSymbol = semanticModel.GetDeclaredSymbol( field.Declaration.Variables.First() ) as IFieldSymbol;
					
					var fieldType = fieldSymbol.Type.ToDisplayString( SymbolDisplayFormat.FullyQualifiedFormat );
					var fieldName = fieldSymbol.Name.ToString();

					var className = fieldSymbol.ContainingType.Name.ToString();
					var namespaceName = fieldSymbol.ContainingNamespace.ToDisplayString();

					var attributeSyntax = field.AttributeLists
										 .SelectMany( al => al.Attributes )
										 .First( a => a.Name.ToString() == "WithProperty" );

					string propertyName = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.ToString();

					if ( propertyName == null )
						propertyName = GetPropertyName( fieldName );
					else
						propertyName = propertyName.Substring( 1, propertyName.Length - 2 );
					
					var sourceBuilder = new StringBuilder( $@"
using System.Text.Json;

namespace {namespaceName}
{{
    partial class {className}
    {{
		/// <summary>
		/// <para>
		/// <b>(Auto-generated)</b> from <see cref=""{fieldName}"" />
		/// </para>
		/// </summary>
		public {fieldType} {propertyName} => {fieldName};
    }}
}}" );

					context.AddSource( $"{className}_{propertyName}Property.generated.cs", SourceText.From( sourceBuilder.ToString(), Encoding.UTF8 ) );
				}
			}
		}
	}
}
