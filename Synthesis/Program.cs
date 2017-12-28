namespace Synthesis
{
    using System;
    using System.IO;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    public static class Program
    {
        private static readonly string AssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

        public static void Main()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText("Class.cs"));

            Rewriter rewriter = new Rewriter();

            var newRoot = rewriter.Visit(tree.GetRoot());
            newRoot = rewriter.SwapNodes(newRoot);

            newRoot = rewriter.RemoveNodes(newRoot);
            File.WriteAllText(@"..\..\Class.altered.cs", newRoot.GetText().ToString());

            var mscorlib = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "mscorlib.dll"));
            var system = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "System.dll"));
            var systemCore = MetadataReference.CreateFromFile(Path.Combine(AssemblyPath, "System.Core.dll"));

            var compilation = CSharpCompilation.Create(
                "Altered",
                syntaxTrees: new[] { newRoot.SyntaxTree },
                references: new[] { mscorlib, system, systemCore },
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));

            foreach (var item in compilation.GetDiagnostics())
                Console.WriteLine($"Diagnostics: {item}");

            var emitResult = compilation.Emit("Altered.exe", "Altered.pdb");

            if (!emitResult.Success)
                foreach (var error in emitResult.Diagnostics)
                    Console.WriteLine(error);
        }
    }
}
