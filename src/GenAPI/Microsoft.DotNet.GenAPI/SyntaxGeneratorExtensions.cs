// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Linq;
using System.Security.AccessControl;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.GenAPI
{
    internal static class SyntaxGeneratorExtensions
    {
        /// <summary>
        /// Creates a declaration matching an existing symbol.
        ///     The reason of having this similar to `SyntaxGenerator.Declaration` extension method is that
        ///     SyntaxGenerator does not generates attributes neither for types, neither for members.
        /// </summary>
        public static SyntaxNode DeclarationExt(this SyntaxGenerator syntaxGenerator, ISymbol symbol)
        {
            if (symbol.Kind == SymbolKind.NamedType)
            {
                INamedTypeSymbol type = (INamedTypeSymbol)symbol;
                switch (type.TypeKind)
                {
                    case TypeKind.Class:
                    case TypeKind.Struct:
                    case TypeKind.Interface:
                        TypeDeclarationSyntax typeDeclaration = (TypeDeclarationSyntax)syntaxGenerator.Declaration(symbol);
                        return typeDeclaration.WithMembers(new SyntaxList<MemberDeclarationSyntax>());
                    case TypeKind.Enum:
                        EnumDeclarationSyntax enumDeclaration = (EnumDeclarationSyntax)syntaxGenerator.Declaration(symbol);
                        return enumDeclaration.WithMembers(new SeparatedSyntaxList<EnumMemberDeclarationSyntax>());
                }
            }

            if (symbol.Kind == SymbolKind.Method)
            {
                IMethodSymbol method = (IMethodSymbol)symbol;
                if (method.MethodKind == MethodKind.Constructor)
                {
                    INamedTypeSymbol? baseType = method.ContainingType.BaseType;
                    // If the base type does not have default constructor.
                    if (baseType != null && !baseType.Constructors.IsEmpty && baseType.Constructors.Where(c => c.Parameters.IsEmpty).Count() == 0)
                    {
                        ConstructorDeclarationSyntax declaration = (ConstructorDeclarationSyntax)syntaxGenerator.Declaration(method);
                        return declaration.WithInitializer(GenerateBaseConstructorInitializer(baseType.Constructors));
                    }
                }
            }

            try
            {
                return syntaxGenerator.Declaration(symbol);
            }
            catch (ArgumentException ex)
            {
                // re-throw the ArgumentException with the symbol that caused it.
                throw new ArgumentException(ex.Message, symbol.ToDisplayString());
            }
        }

        private static ConstructorInitializerSyntax GenerateBaseConstructorInitializer(ImmutableArray<IMethodSymbol> baseTypeConstructors)
        {
            ConstructorInitializerSyntax constructorInitializer = SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer);

            IMethodSymbol baseConstructor = baseTypeConstructors.OrderBy(c => c.Parameters.Count()).First();
            foreach (IParameterSymbol parameter in baseConstructor.Parameters)
            {
                IdentifierNameSyntax identifier;
                if (parameter.Type.IsValueType)
                    identifier = SyntaxFactory.IdentifierName("default");
                else
                    identifier = SyntaxFactory.IdentifierName("default!");

                constructorInitializer = constructorInitializer.AddArgumentListArguments(SyntaxFactory.Argument(identifier));
            }

            return constructorInitializer;
        }
    }
}
