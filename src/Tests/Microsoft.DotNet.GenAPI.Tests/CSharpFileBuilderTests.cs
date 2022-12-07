// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiSymbolExtensions;
using Microsoft.DotNet.ApiSymbolExtensions.Tests;

namespace Microsoft.DotNet.GenAPI.Tests
{
    public class CSharpFileBuilderTests
    {
        private readonly StringWriter _stringWriter = new();
        private readonly CSharpSyntaxWriter _csharpSyntaxWriter = new(null);
        private readonly IAssemblySymbolWriter _csharpFileBuilder;

        class AllowAllFilter : ISymbolFilter
        {
            public bool Include(ISymbol symbol) => true;
        }

        public CSharpFileBuilderTests()
        {
            var compositeFilter = new CompositeFilter()
                .Add<ImplicitSymbolsFilter>()
                .Add(new SymbolAccessibilityBasedFilter(true, true));
            _csharpFileBuilder = new CSharpFileBuilder(compositeFilter, _stringWriter, _csharpSyntaxWriter, MetadataReferences);
        }

        private static IEnumerable<MetadataReference> MetadataReferences { get => new List<MetadataReference> { MetadataReference.CreateFromFile(typeof(Object).Assembly!.Location!) }; }

        private static SyntaxTree GetSyntaxTree(string syntax)
        {
            return CSharpSyntaxTree.ParseText(syntax);
        }

        private void RunTest(string original, string expected)
        {
            IAssemblySymbol assemblySymbol = SymbolFactory.GetAssemblyFromSyntax(original);
            _csharpFileBuilder.WriteAssembly(assemblySymbol);

            StringBuilder stringBuilder = _stringWriter.GetStringBuilder();
            var resultedString = stringBuilder.ToString();

            stringBuilder.Remove(0, stringBuilder.Length);

            SyntaxTree resultedSyntaxTree = GetSyntaxTree(resultedString);
            SyntaxTree expectedSyntaxTree = GetSyntaxTree(expected);

            /// compare SyntaxTree and not string representation
            Assert.True(resultedSyntaxTree.IsEquivalentTo(expectedSyntaxTree),
                $"Expected:\n{expected}\nResulted:\n{resultedString}");
        }

        [Fact]
        public void TestNamespaceDeclaration()
        {
            RunTest(original: """
                namespace A
                {
                namespace B {}
                
                namespace C.D { public struct Bar {} }
                }
                """,
                expected: """
                namespace A.C.D { public partial struct Bar {} }
                """);
        }

        [Fact]
        public void TestClassDeclaration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public class PublicClass { }

                    class InternalClass { }

                    public sealed class PublicSealedClass { }

                    public partial class ProtectedPartialClass { }
                }
                """,
                expected: """
                namespace Foo
                {
                    internal partial class InternalClass { }

                    /// `partial` keyword is not added!
                    public partial class ProtectedPartialClass { }

                    public partial class PublicClass { }

                    public sealed partial class PublicSealedClass { }
                }
                """);
        }

        [Fact]
        public void TestStructDeclaration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public struct PublicStruct { }

                    struct InternalStruct { }

                    readonly struct ReadonlyStruct { }

                    public readonly struct PublicReadonlyStruct { }

                    record struct RecordStruct { }

                    readonly record struct ReadonlyRecordStruct { }

                    public ref struct PublicRefStruct { }

                    public readonly ref struct PublicReadonlyRefStruct { }
                }
                """,
                expected: """
                namespace Foo
                {
                    internal partial struct InternalStruct { }

                    public partial struct PublicReadonlyRefStruct { }

                    public partial struct PublicReadonlyStruct { }

                    public partial struct PublicRefStruct { }

                    public partial struct PublicStruct { }

                    internal partial struct ReadonlyRecordStruct : System.IEquatable<ReadonlyRecordStruct> { }

                    internal partial struct ReadonlyStruct { }

                    internal partial struct RecordStruct : System.IEquatable<RecordStruct> { }
                }
                """);
        }

        [Fact]
        public void TestInterfaceGeneration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public interface IPoint
                    {
                        // Property signatures:
                        int X { get; set; }
                        int Y { get; set; }
                        
                        double CalculateDistance(IPoint p);
                    }
                }
                """,
                expected: """
                namespace Foo
                {
                    public partial interface IPoint
                    {
                        // Property signatures:
                        int X { get; set; }
                        int Y { get; set; }
                        
                        double CalculateDistance(IPoint p);
                    }
                }
                """);
        }

        [Fact]
        public void TestEnumGeneration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public enum Color
                    {
                        White = 0,
                        Green = 100,
                        Blue = 200
                    }
                }
                """,
                expected: """
                namespace Foo
                {
                    public enum Color
                    {
                        White = 0,
                        Green = 100,
                        Blue = 200
                    }
                }
                """);
        }

        [Fact]
        public void TestPropertyGeneration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public class Car
                    {
                        public int? Drivers { get; }
                        public int Wheels { get => 4; }
                        public bool IsRunning { get; set; }
                        public bool Is4x4 { get => false; set { } }
                    }
                }
                """,
                expected: """
                namespace Foo
                {
                    public partial class Car
                    {
                        public int? Drivers { get { throw null; } }
                        public bool Is4x4 { get { throw null; } set { } }
                        public bool IsRunning { get { throw null; } set { } }
                        public int Wheels { get { throw null; } }
                    }
                }
                """);
        }

        [Fact]
        public void TestAbstractPropertyGeneration()
        {
            RunTest(original: """
                namespace Foo
                {
                    abstract class Car
                    {
                        abstract protected int? Wheels { get; }
                        abstract public bool IsRunning { get; set; }
                    }
                }
                """,
                expected: """
                namespace Foo
                {
                    internal abstract partial class Car
                    {
                        public abstract bool IsRunning { get; set; }
                        protected abstract int? Wheels { get; }
                    }
                }
                """);
        }

        [Fact]
        public void TestExplicitInterfaceImplementation()
        {
            RunTest(original: """
                namespace Foo
                {
                    public interface IControl
                    {
                        void Paint();
                    }
                    public interface ISurface
                    {
                        void Paint();
                    }
                
                    public class SampleClass : IControl, ISurface
                    {
                        public void Paint()
                        {
                        }
                    }
                }
                """,
                expected: """
                namespace Foo
                {
                    public partial interface IControl
                    {
                        void Paint();
                    }
                    public partial interface ISurface
                    {
                        void Paint();
                    }
                
                    public partial class SampleClass : IControl, ISurface
                    {
                        public void Paint()
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public void TestPartiallySpecifiedGenericClassGeneration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public class BaseNodeMultiple<T, U> { }
                
                    public class Node4<T> : BaseNodeMultiple<T, int> { }
                
                    public class Node5<T, U> : BaseNodeMultiple<T, U> { }
                }
                """,
                expected: """
                namespace Foo
                {
                    public partial class BaseNodeMultiple <T, U> { }
                
                    public partial class Node4 <T> : BaseNodeMultiple<T, int> { }
                
                    public partial class Node5 <T, U> : BaseNodeMultiple<T, U> { }
                }
                """);
        }

        [Fact]
        public void TestGenericClassWitConstraintsParameterGeneration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public class SuperKeyType<K, V, U>
                        where U : System.IComparable<U>
                        where V : new()
                    { }
                }
                """,
                expected: """
                namespace Foo
                {
                    public partial class SuperKeyType <K, V, U> where V : new()
                        where U : System.IComparable<U>
                    {
                    }
                }
                """);
        }

        [Fact]
        public void TestPublicMembersGeneration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public enum Kind
                    {
                        None = 0,
                        Disable = 1
                    }
                
                    public readonly struct Options
                    {
                        public readonly bool BoolMember = true;
                        public readonly Kind KindMember = Kind.Disable;
                
                        public Options(Kind kindVal)
                            : this(kindVal, false)
                        {
                        }
                
                        public Options(Kind kindVal, bool boolVal)
                        {
                            BoolMember = boolVal;
                            KindMember = kindVal;
                        }
                    }
                }
                """,
                expected: """
                namespace Foo
                {
                    public enum Kind
                    {
                        None = 0,
                        Disable = 1
                    }

                    public partial struct Options
                    {
                        public readonly bool BoolMember;
                        public readonly Kind KindMember;
                        public Options(Kind kindVal, bool boolVal) { }
                        public Options(Kind kindVal) { }
                    }
                }
                """);
        }

        [Fact]
        void TestDelegateGeneration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public delegate bool SyntaxReceiverCreator(int a, bool b);
                }
                """,
                expected: """
                namespace Foo
                {
                    public sealed delegate bool SyntaxReceiverCreator(int a, bool b);
                }
                """);
        }

        [Fact]
        void TestAbstractEventGeneration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public abstract class Events
                    {
                        public abstract event System.EventHandler<bool> TextChanged;
                    }
                }
                """,
                expected: """
                namespace Foo
                {
                    public abstract partial class Events
                    {
                        public event System.EventHandler<bool> TextChanged;
                    }
                }
                """);
        }

        [Fact]
        void TestCustomAttributeGeneration()
        {
            RunTest(original: """
                using System;
                using System.Diagnostics;

                namespace Foo
                {
                    public enum Animal
                    {
                        Dog = 1,
                        Cat,
                        Bird,
                    }

                    public class AnimalTypeAttribute : Attribute
                    {
                        protected Animal thePet;

                        public AnimalTypeAttribute(Animal pet)
                        {
                            thePet = pet;
                        }

                        public Animal Pet
                        {
                            get { return thePet; }
                            set { thePet = value; }
                        }
                    }

                    [AnimalType(Animal.Cat)]
                    public class Creature
                    {
                        [AnimalType(Animal.Cat)]
                        [Conditional("DEBUG"), Conditional("TEST1")]
                        public void SayHello() { }
                    }
                }
                """,
                expected: """
                namespace Foo
                {
                    public enum Animal
                    {
                        Dog = 1,
                        Cat = 2,
                        Bird = 3
                    }

                    public partial class AnimalTypeAttribute : System.Attribute
                    {
                        protected Animal thePet;
                        public AnimalTypeAttribute(Animal pet) { }
                        public Animal Pet { get { throw null; } set { } }
                    }

                    [AnimalType(Animal.Cat)]
                    public partial class Creature
                    {
                        [AnimalType(Animal.Cat)]
                        [System.Diagnostics.Conditional("DEBUG")]
                        [System.Diagnostics.Conditional("TEST1")]
                        public void SayHello() { }
                    }
                }
                """);
        }

        [Fact]
        void TestFullyQualifiedNamesForDefaultEnumParameters()
        {
            RunTest(original: """
                namespace Foo
                {
                    public enum Animal
                    {
                        Dog = 1,
                        Cat = 2,
                        Bird = 3
                    }

                    public class AnimalProperty {
                        public Animal _animal;

                        public AnimalProperty(Animal animal = Animal.Cat)
                        {
                            _animal = animal;
                        }

                        public int Execute(int p = 42) { return p; }
                    }
                }
                """,
                expected: """
                namespace Foo
                {
                    public enum Animal
                    {
                        Dog = 1,
                        Cat = 2,
                        Bird = 3
                    }
                
                    public partial class AnimalProperty {
                        public Animal _animal;

                        public AnimalProperty(Animal animal = Animal.Cat) { }

                        public int Execute(int p = 42) { throw null; }
                    }
                }
                """);
        }

        [Fact]
        void TestCustomComparisonOperatorGeneration()
        {
            RunTest(original: """
                namespace Foo
                {
                    public class Car : System.IEquatable<Car>
                    {
                        public bool Equals(Car c) { return true; }
                        public override bool Equals(object o) { return true; }
                        public override int GetHashCode() => 0;
                        public static bool operator ==(Car lhs, Car rhs) { return true; }
                        public static bool operator !=(Car lhs, Car rhs) { return false; }
                    }
                }
                """,
                expected: """
                namespace Foo
                {
                    public partial class Car : System.IEquatable<Car>
                    {
                        public bool Equals(Car c) { throw null; }
                        public override bool Equals(object o) { throw null; }
                        public override int GetHashCode() { throw null; }
                        public static bool operator ==(Car lhs, Car rhs) { throw null; }
                        public static bool operator !=(Car lhs, Car rhs) { throw null; }
                    }
                }
                """);
        }
    }
}
