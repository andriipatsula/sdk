// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiSymbolExtensions;

namespace Microsoft.DotNet.GenAPI.Shared
{
    internal class FilterOutImplicitSymbols : ISymbolFilter
    {
        /// <inheritdoc />
        public bool Include(ISymbol member)
        {
            if (member is IMethodSymbol method)
            {
                if (member.IsImplicitlyDeclared ||
                    member.Kind == SymbolKind.NamedType ||
                    method.MethodKind == MethodKind.PropertyGet ||
                    method.MethodKind == MethodKind.PropertySet ||
                    method.MethodKind == MethodKind.EventAdd ||
                    method.MethodKind == MethodKind.EventRemove ||
                    method.MethodKind == MethodKind.EventRaise ||
                    method.MethodKind == MethodKind.DelegateInvoke)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
