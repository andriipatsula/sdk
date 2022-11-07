// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiSymbolExtensions;

namespace Microsoft.DotNet.GenAPI.Shared
{
    internal class IntersectionFilter : ISymbolFilter
    {
        private readonly List<ISymbolFilter> _innerFilters = new();

        /// <inheritdoc />
        public bool Include(ISymbol member) => _innerFilters.All(f => f.Include(member));

        public IntersectionFilter Add<T>() where T : ISymbolFilter, new()
        {
            _innerFilters.Add(new T());
            return this;
        }

        public IntersectionFilter Add(ISymbolFilter filter)
        {
            _innerFilters.Add(filter);
            return this;
        }
    }
}
