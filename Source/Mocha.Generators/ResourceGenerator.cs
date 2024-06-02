using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace Mocha.Generators
{
	[Generator]
	public class ResourceGenerator : ISourceGenerator
	{
		public void Initialize( GeneratorInitializationContext context )
		{
			// Nothing to do here
		}

		public void Execute( GeneratorExecutionContext context )
		{
			// Iterate over all syntax trees in the compilation
			foreach ( var syntaxTree in context.Compilation.SyntaxTrees )
			{
				// Find all struct declarations that are annotated with [Resource]
				var resourceStructs = syntaxTree.GetRoot()
												.DescendantNodes()
												.OfType<StructDeclarationSyntax>()
												.Where( s => s.AttributeLists
															 .Any( al => al.Attributes
																		   .Any( a => a.Name.ToString() == "Resource" ) ) );

				// Iterate over the resource structs and generate code
				foreach ( var resourceStruct in resourceStructs )
				{
					var namespaceName = (resourceStruct.Parent as NamespaceDeclarationSyntax)?.Name.ToString() ?? "Mocha";
					var structName = resourceStruct.Identifier.Text;

					var sourceBuilder = new StringBuilder( $@"
using System.Text.Json;

namespace {namespaceName}
{{
    partial struct {structName}
    {{
		/// <summary>
		/// <para>
		/// Loads a <see cref=""{structName}"" /> structure from a file using <see cref=""FileSystem.Content"" />
		/// </para>
		/// <para>
		/// <b>(Auto-generated)</b>
		/// </para>
		/// </summary>
        public static {structName} Load( string filePath )
        {{
			var file = FileSystem.Content.ReadAllText( filePath );
			return JsonSerializer.Deserialize<{structName}>( file );
        }}

		/// <summary>
		/// <para>
		/// Loads a <see cref=""{structName}"" /> structure from a data array.
		/// </para>
		/// <para>
		/// <b>(Auto-generated)</b>
		/// </para>
		/// </summary>
        public static {structName} Load( byte[] data )
        {{
			return JsonSerializer.Deserialize<{structName}>( data );
        }}
    }}
}}" );

					context.AddSource( $"{structName}_Load.generated.cs", SourceText.From( sourceBuilder.ToString(), Encoding.UTF8 ) );
				}
			}
		}
	}
}
