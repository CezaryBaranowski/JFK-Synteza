namespace Synthesis
{
    using System;
    using System.Linq;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Generic;

    internal sealed class Rewriter : CSharpSyntaxRewriter
    {
       private static List<CSharpSyntaxNode>nodesToDelete = new List<CSharpSyntaxNode>();
       private static Dictionary<CSharpSyntaxNode, SyntaxNode> swap = new Dictionary<CSharpSyntaxNode, SyntaxNode>();

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)             // przeszukuje bloki w poszukiwaniu wywołań metod
        {
            var firstNode = node.ChildNodes().OfType<BlockSyntax>().FirstOrDefault();    // pierwszy statement
            if (null != firstNode)
            {
                var invocationExpressions = firstNode.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray()?.ToArray();
                if (invocationExpressions.Count() != 0)
                {
                    foreach (var invoke in invocationExpressions)
                    {
                        String name = invoke.Expression.GetText().ToString();
                        var methodStatements = node.Parent.DescendantNodes().OfType<MethodDeclarationSyntax>()?.ToArray();
                        if (methodStatements.Count() != 0)
                        {
                            foreach (var method in methodStatements)
                            {
                                if (method.Identifier.ValueText.Equals(name))
                                {
                                    if (method.ParameterList.ChildNodes().OfType<ParameterSyntax>().Count() == 0)
                                    {
                                        var methodReturnStatements = method.DescendantNodes().OfType<ReturnStatementSyntax>();          // i tak jest tylko jeden, ale niech bedzie
                                        var methodReturnStatement = methodReturnStatements.First().ChildNodes().FirstOrDefault();       // pierwszy statement pod returnem
                                        if (methodReturnStatement.IsKind(SyntaxKind.NumericLiteralExpression) || (methodReturnStatement.IsKind(SyntaxKind.StringLiteralExpression)))
                                        {
                                            var newNode = methodReturnStatement;
                                            swap.Add(invoke, newNode);
                                            if (!nodesToDelete.Contains(method))
                                            {
                                                nodesToDelete.Add(method);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                } 
            }

            return base.VisitMethodDeclaration(node); 
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var methodStatements = node.Parent.DescendantNodes().OfType<MethodDeclarationSyntax>()?.ToArray();  // wszystkie metody w klasie
            var predefinedType = node.DescendantNodes().OfType<PredefinedTypeSyntax>().FirstOrDefault();    // typ wartosci zwracanej metody jesli nie string
            var identifierName = node.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault();    // a tu jesli string
            SyntaxNode methodType;
            if (predefinedType != null) methodType = predefinedType; else methodType = identifierName;
            var fieldType = node.DescendantNodes().OfType<VariableDeclarationSyntax>().FirstOrDefault().ChildNodes().FirstOrDefault();   // typ zmiennej w polu
            var invocationExpression = node.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();

            if (invocationExpression != null)
            {
                String invokeName = invocationExpression.Expression.GetText().ToString();   // nazwa wywolywanej metody w deklaracji pola
                foreach (var method in methodStatements)
                {
                    if (method.Identifier.ValueText.Equals(invokeName))
                    {
                        if (methodType.GetText().ToString().Equals(fieldType.GetText().ToString()))
                        {
                            if (method.ParameterList.ChildNodes().OfType<ParameterSyntax>().Count() == 0)
                            {
                                var methodReturnStatements = method.DescendantNodes().OfType<ReturnStatementSyntax>();
                                var methodReturnStatement = methodReturnStatements.First().ChildNodes().FirstOrDefault();
                                if (methodReturnStatement.IsKind(SyntaxKind.NumericLiteralExpression) || (methodReturnStatement.IsKind(SyntaxKind.StringLiteralExpression)))
                                {
                                    var newNode = methodReturnStatement;
                                    swap.Add(invocationExpression, newNode);
                                    if (!nodesToDelete.Contains(method))
                                    {
                                        nodesToDelete.Add(method);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return base.VisitFieldDeclaration(node);
        }

        public SyntaxNode SwapNodes (SyntaxNode oldRoot)
        {
            var newRoot = oldRoot.ReplaceNodes(swap.Keys, (n1,n2) => swap[n2]);
            return newRoot;
        }

        public SyntaxNode RemoveNodes (SyntaxNode oldRoot)
        {          
            var methodStatements = oldRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray()?.ToArray();
            List<CSharpSyntaxNode> locals = new List<CSharpSyntaxNode>();
            foreach (CSharpSyntaxNode mth in methodStatements)
            {
                foreach(CSharpSyntaxNode ntd in nodesToDelete)
                {
                    if(ntd.GetText().ToString().Equals(mth.GetText().ToString()))
                    {
                        locals.Add(mth);
                    }
                }
            }
            var newRoot = oldRoot.RemoveNodes(locals, SyntaxRemoveOptions.AddElasticMarker);
            return newRoot;
        }
    }
}
