// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.GenAPI.Shared
{
    /// <summary>
    /// Interface incapsulates logic for processing symbol assemblies.
    /// </summary>
    internal interface IAssemblySymbolWriter
    {
        void WriteAssembly(IAssemblySymbol assemblySymbol);
    }
}
