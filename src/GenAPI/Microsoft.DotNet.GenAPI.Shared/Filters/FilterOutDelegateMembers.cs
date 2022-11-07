// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiSymbolExtensions;

namespace Microsoft.DotNet.GenAPI.Shared
{
    internal class FilterOutDelegateMembers : ISymbolFilter
    {
        /// <inheritdoc />
        public bool Include(ISymbol member)
        {
            if (member is IMethodSymbol method)
            {
                return member.ContainingType.TypeKind != TypeKind.Delegate;
            }
            return true;
        }
    }
}
