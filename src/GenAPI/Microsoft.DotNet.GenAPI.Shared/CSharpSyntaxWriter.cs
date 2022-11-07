// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.GenAPI.Shared
{
    internal class CSharpSyntaxWriter : CSharpSyntaxRewriter
    {
        private readonly string? _exceptionMessage;

        public CSharpSyntaxWriter(string? exceptionMessage)
        {
            _exceptionMessage = exceptionMessage;
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            node = node.WithBody(GetEmptyBody(true))
                       .WithParameterList((ParameterListSyntax)node.ParameterList.WithTrailingTrivia(SyntaxFactory.Space));
            return base.VisitConstructorDeclaration(node);
        }

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            node = (InterfaceDeclarationSyntax)base.VisitInterfaceDeclaration(node)!;
            return AddPartialModifier(node);
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
            return AddPartialModifier(node);
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node)!;
            return AddPartialModifier(node);
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // visit subtree first to normalize type names.
            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;

            if (node.Modifiers.Where(token => token.IsKind(SyntaxKind.AbstractKeyword)).Any())
            {
                return node;
            }

            if (node.ExpressionBody != null)
            {
                node = node.WithExpressionBody(null);
            }

            if (node.ReturnType.ToString() != "System.Void")
            {
                node = node.WithBody(GetThrowNullBody(true));
            }
            else
            {
                node = node.WithBody(GetEmptyBody(true));
            }

            return node.WithParameterList((ParameterListSyntax)node.ParameterList.WithTrailingTrivia(SyntaxFactory.Space));
        }

        public override SyntaxNode? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            return node.Kind() switch
            {

                SyntaxKind.GetAccessorDeclaration => node.WithSemicolonToken(default)
                                                         .WithKeyword(node.Keyword.WithTrailingTrivia(SyntaxFactory.Space))
                                                         .WithBody(GetThrowNullBody(newLine: false)),
                SyntaxKind.SetAccessorDeclaration => node.WithSemicolonToken(default)
                                                         .WithKeyword(node.Keyword.WithTrailingTrivia(SyntaxFactory.Space))
                                                         .WithBody(GetEmptyBody(newLine: false)),
                _ => base.VisitAccessorDeclaration(node)
            };
        }

        private SyntaxNode AddPartialModifier<T>(T node) where T: TypeDeclarationSyntax
        {
            return node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space));
        }

        private BlockSyntax GetEmptyBody(bool newLine = false)
        {
            BlockSyntax node = GetMethodBodyFromText(SyntaxFactory.Space.ToString(), newLine);
            return node.WithOpenBraceToken(node.OpenBraceToken.WithTrailingTrivia(SyntaxFactory.Space));
        }

        private BlockSyntax GetThrowNullBody(bool newLine = false)
        {
            if (_exceptionMessage is string exceptionMessage)
            {
                return GetMethodBodyFromText(string.Format(" throw new PlatformNotSupportedException(\"{0}\"); ", exceptionMessage), newLine);
            }
            return GetMethodBodyFromText(" throw null; ", newLine);
        }

        private BlockSyntax GetMethodBodyFromText(string text, bool newLine = false) =>
            SyntaxFactory.Block(SyntaxFactory.ParseStatement(text))
                         .WithTrailingTrivia(newLine ? SyntaxFactory.CarriageReturnLineFeed : SyntaxFactory.Space);
    }

    
}
