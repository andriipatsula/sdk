﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.DotNet.GenAPI.SyntaxRewriter
{
    /// <summary>
    /// Handles type forward assembly attributes and removes generic type arguments:
    ///     [assembly:TypeForwardedToAttribute(typeof(System.Collections.Generic.IAsyncEnumerable<A, B, C>))] ->
    ///     [assembly:TypeForwardedToAttribute(typeof(System.Collections.Generic.IAsyncEnumerable<,,>))] ->
    /// </summary>
    public class TypeForwardAttributeCSharpSyntaxRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            // skip if not of `typeof(Type<A,B,C>)` format
            if (node.Parent == null || node.Parent.Parent == null || node.Parent.Parent is not TypeOfExpressionSyntax)
            {
                return node;
            }

            TypeArgumentListSyntax typeArgumentList = node.TypeArgumentList;
            SeparatedSyntaxList<TypeSyntax> newArguments = new();

            foreach (IdentifierNameSyntax argument in typeArgumentList.Arguments)
            {
                newArguments = newArguments.Add(argument.WithIdentifier(SyntaxFactory.Identifier("")));
            }

            typeArgumentList = typeArgumentList.WithArguments(newArguments);
            return node.WithTypeArgumentList(typeArgumentList);
        }
    }
}
