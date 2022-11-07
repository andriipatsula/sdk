// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiSymbolExtensions;

namespace Microsoft.DotNet.GenAPI.Shared
{

    /// <summary>
    /// Class to standertize initilization and running of GenAPI tool.
    ///     Shared between CLI and MSBuild tasks frontends.
    /// </summary>
    public static class GenAPIApp
    {
        public class Context
        {
            /// <summary>
            /// Delimited (',' or ';') set of paths for assemblies or directories to get all assemblies.
            /// </summary>
            public string? Assembly { get; set; }

            /// <summary>
            /// If true, tries to resolve assembly reference.
            /// </summary>
            public bool ResolveAssemblyReferences { get; set; } = false;

            /// <summary>
            /// Delimited (',' or ';') set of paths to use for resolving assembly references.
            /// </summary>
            public string? LibPath { get; set; }

            /// <summary>
            /// Output path. Default is the console. Can specify an existing directory as well and
            /// then a file will be created for each assembly with the matching name of the assembly.
            /// </summary>
            public string? OutputPath { get; set; }

            /// <summary>
            /// Specify a file with an alternate header content to prepend to output.
            /// </summary>
            public string? HeaderFile { get; set; }

            /// <summary>
            /// Method bodies should throw PlatformNotSupportedException.
            /// </summary>
            public string? ExceptionMessage { get; set; }

            /// <summary>
            /// Indentation size in `IndentationChar`s. Default is 4.
            /// </summary>
            public int IndentationSize { get; set; } = 4;

            /// <summary>
            /// Indentation character: space, tabulation. Default is space.
            /// </summary>
            public char IndentationChar { get; set; } = ' ';

            /// <summary>
            /// Specify a list in the DocId format of which attributes should be excluded from being applied on apis.
            /// </summary>
            public string? ExcludeAttributesList { get; set; }

            /// <summary>
            /// Include all API's not just public APIs. Default is public only.
            /// </summary>
            public bool IncludeVisibleOutsideOfAssembly { get; set; } = false;
        }

        /// <summary>
        /// Initialize and run Roslyn-based GenAPI tool.
        /// </summary>
        public static void Run(Context context)
        {
            IAssemblySymbolLoader loader = new AssemblySymbolLoader(context.ResolveAssemblyReferences);

            loader.AddReferenceSearchPaths(SplitPaths(context.LibPath));
            AddReferenceToRuntimeLibraries(loader);

            var intersectionFilter = new IntersectionFilter()
                .Add<FilterOutDelegateMembers>()
                .Add<FilterOutImplicitSymbols>()
                .Add(new SymbolAccessibilityBasedFilter(context.IncludeVisibleOutsideOfAssembly));

            if (context.ExcludeAttributesList != null)
            {
                intersectionFilter.Add(new FilterOutAttributes(context.ExcludeAttributesList));
            }

            var assemblySymbols = loader.LoadAssemblies(SplitPaths(context.Assembly));
            foreach (var assemblySymbol in assemblySymbols)
            {
                if (assemblySymbol == null) continue;

                using var fileBuilder = new CSharpFileBuilder(
                    intersectionFilter,
                    GetTextWriter(context.OutputPath, assemblySymbol.Name),
                    new CSharpSyntaxWriter(context.ExceptionMessage));

                fileBuilder.WriteAssembly(assemblySymbol);
            }

            IReadOnlyList<Diagnostic> diagnostics = new List<Diagnostic>();
            loader.HasRoslynDiagnostics(out diagnostics);

            foreach (var diagnostic in diagnostics)
            {
                //logger.LogMessage(MessageImportance.High, "Roslyn Diagnostics: {0}", diagnostic.ToString());
            }

            IReadOnlyList<AssemblyLoadWarning> warnings = new List<AssemblyLoadWarning>();
            loader.HasLoadWarnings(out warnings);

            foreach (var warning in warnings)
            {
                //logger.LogMessage(MessageImportance.Normal, "WARN: {0}", warning.Message);
            }
        }

        /// <summary>
        /// Creates a TextWriter capable to write into Console or cs file.
        /// </summary>
        /// <param name="outputDirPath">Path to a directory where file with `assemblyName`.cs filename needs to be created.
        ///     If Null - output to Console.Out.</param>
        /// <param name="assemblyName">Name of an assembly. if outputDirPath is not a Null - represents a file name.</param>
        /// <returns></returns>
        private static TextWriter GetTextWriter(string? outputDirPath, string assemblyName)
        {
            if (outputDirPath == null)
            {
                return Console.Out;
            }

            string fileName = assemblyName + ".cs";
            if (Directory.Exists(outputDirPath) && !string.IsNullOrEmpty(fileName))
            {
                return File.CreateText(Path.Combine(outputDirPath, fileName));
            }

            return File.CreateText(outputDirPath);
        }

        /// <summary>
        /// Splits delimiter separated list of pathes represented as a string to a List of paths.
        /// </summary>
        /// <param name="pathSet">Delimiter separated list of paths.</param>
        /// <returns></returns>
        private static string[] SplitPaths(string? pathSet)
        {
            if (pathSet == null) return Array.Empty<string>();

            return pathSet.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Read the header file if specified, or use default one.
        /// </summary>
        /// <param name="headerFile">File with an alternate header content to prepend to output</param>
        /// <returns></returns>
        public static string ReadHeaderFile(string? headerFile)
        {
            const string defaultFileHeader = @"
            //------------------------------------------------------------------------------
            // <auto-generated>
            //     This code was generated by a tool.
            //
            //     Changes to this file may cause incorrect behavior and will be lost if
            //     the code is regenerated.
            // </auto-generated>
            //------------------------------------------------------------------------------
            ";

            if (!string.IsNullOrEmpty(headerFile))
            {
                return File.ReadAllText(headerFile);
            }
            return defaultFileHeader;
        }

        private static void AddReferenceToRuntimeLibraries(IAssemblySymbolLoader loader)
        {
            var corlibLocation = typeof(object).Assembly.Location;
            var runtimeFolder = Path.GetDirectoryName(corlibLocation);

            if (runtimeFolder != null)
            {
                loader.AddReferenceSearchPaths(runtimeFolder);
            }
        }
    }
}
