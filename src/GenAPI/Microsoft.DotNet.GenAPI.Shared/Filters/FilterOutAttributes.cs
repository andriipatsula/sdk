// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiSymbolExtensions;

namespace Microsoft.DotNet.GenAPI.Shared
{
    internal class FilterOutAttributes : ISymbolFilter
    {
        private readonly HashSet<string> _attributesToExclude;

        public FilterOutAttributes(IEnumerable<string> attributes)
        {
            _attributesToExclude = new HashSet<string>(attributes);
        }

        public FilterOutAttributes(string attributeDocIdsFile)
        {
            _attributesToExclude = new HashSet<string>(ReadDocIdsAttributes(attributeDocIdsFile));
        }

        /// <inheritdoc />
        public bool Include(ISymbol member)
        {
            if (member is INamedTypeSymbol namedType)
            {
                string? docId = namedType.GetDocumentationCommentId();
                if (docId != null && _attributesToExclude.Contains(docId))
                {
                    return false;
                }
            }
            return true;
        }

        private static IEnumerable<string> ReadDocIdsAttributes(string docIdsFile)
        {
            if (!File.Exists(docIdsFile))
                yield break;

            foreach (string id in File.ReadAllLines(docIdsFile))
            {
                if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("T:"))
                    continue;

                yield return id.Trim();
            }
        }
    }
}
