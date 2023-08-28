using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace OpenRA.SourceGenerators
{
	[Generator]
	public class HelloWorldGenerator : ISourceGenerator
	{
		public void Execute(GeneratorExecutionContext context)
		{
			// begin creating the source we'll inject into the users compilation
			var sourceCode = SourceText.From(@"
#pragma warning disable 1591
namespace OpenRA {

public static class GeneratedCode
{
    public static string GeneratedMessage = ""Hello from Generated Code"";
}}
#pragma warning restore 1591 
", Encoding.UTF8);

			context.AddSource("GeneratedCode.g.cs", sourceCode);
		}

		public void Initialize(GeneratorInitializationContext context)
		{
		}
	}
}
