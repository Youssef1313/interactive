// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    [TestClass]
    public class Directives
    {
        [TestMethod]
        [DataRow("""
                    var x = 1;
                    #r "nuget:SomePackage"
                    x
                    """,
                    "#r \"nuget:SomePackage\"")]
        [DataRow(""""
                    var x = 1;
                    #r """nuget:SomePackage"""
                    x
                    """",
                    """"
                    #r """nuget:SomePackage"""
                    """")]
        public void Pound_r_nuget_is_parsed_as_a_compiler_directive_node_in_csharp(
            string codeSubmission, 
            string expectedDirectiveText)
        {
            var tree = Parse(codeSubmission, "csharp");

            var node = tree.RootNode
                           .ChildNodes
                           .Should()
                           .ContainSingle<DirectiveNode>()
                           .Which;
            node.Text
                .Should()
                .Be(expectedDirectiveText);

            node.Kind.Should().Be(DirectiveNodeKind.CompilerDirective);

            var expectedParameterValue = expectedDirectiveText.Replace("#r", "").Trim();
            
            node.DescendantNodesAndTokens()
                .Should().ContainSingle<DirectiveParameterValueNode>()
                .Which.Text
                .Should().Be(expectedParameterValue);
        }

        [TestMethod]
        [DataRow("""
                    var x = 1;
                    #r "nuget:SomePackage"
                    x
                    """,
                    "#r \"nuget:SomePackage\"")]
        [DataRow(""""
                    var x = 1;
                    #r """nuget:SomePackage"""
                    x
                    """",
                    """"
                    #r """nuget:SomePackage"""
                    """")]
        public void Pound_r_nuget_is_parsed_as_a_compiler_directive_node_in_fsharp(
            string codeSubmission,
            string expectedDirectiveText)
        {
            var tree = Parse(codeSubmission, "fsharp");

            var node = tree.RootNode
                           .ChildNodes
                           .Should()
                           .ContainSingle<DirectiveNode>()
                           .Which;
            node.Text
                .Should()
                .Be(expectedDirectiveText);

            node.Kind.Should().Be(DirectiveNodeKind.CompilerDirective);

            var expectedParameterValue = expectedDirectiveText.Replace("#r", "").Trim();
            
            node.DescendantNodesAndTokens()
                .Should().ContainSingle<DirectiveParameterValueNode>()
                .Which.Text
                .Should().Be(expectedParameterValue);
        }

        [TestMethod]
        public void Pound_i_is_a_valid_directive()
        {
            var tree = Parse("var x = 1;\n#i \"nuget:/some/path\"\nx");

            var node = tree.RootNode
                           .ChildNodes
                           .Should()
                           .ContainSingle<DirectiveNode>()
                           .Which;
            node.Text
                .Should()
                .Be("#i \"nuget:/some/path\"");

            node.Kind.Should().Be(DirectiveNodeKind.CompilerDirective);
        }

        [TestMethod]
        [DataRow("var x = 123$$;", typeof(LanguageNode))]
        [DataRow("#!csharp\nvar x = 123$$;", typeof(LanguageNode))]
        [DataRow("#!csharp\nvar x = 123$$;\n", typeof(LanguageNode))]
        [DataRow("#!csh$$arp\nvar x = 123;", typeof(DirectiveNameNode))]
        [DataRow("#!csharp\n#!time a b$$ c", typeof(DirectiveParameterValueNode))]
        public void Node_type_is_correctly_identified(
            string markupCode,
            Type expectedNodeType)
        {
            MarkupTestFile.GetPosition(markupCode, out var code, out var position);

            var tree = Parse(code);

            var node = tree.RootNode.FindNode(position.Value);

            node.Should().BeOfType(expectedNodeType);
        }

        [TestMethod]
        [DataRow("#!csh$$arp\nvar x = 123;", nameof(DirectiveNodeKind.KernelSelector))]
        [DataRow("#!csharp\n#!time a b$$ c", nameof(DirectiveNodeKind.Action))]
        [DataRow("""#r $$"nuget:PocketLogger"  """, nameof(DirectiveNodeKind.CompilerDirective))]
        [DataRow("""#r $$"/path/to/a.dll"  """, nameof(DirectiveNodeKind.CompilerDirective))]
        [DataRow("""#i $$"nuget:https://api.nuget.org/v3/index.json" """, nameof(DirectiveNodeKind.CompilerDirective))]
        [DataRow("""#i $$"/path/to/some-folder"  """, nameof(DirectiveNodeKind.CompilerDirective))]
        public void DirectiveNode_kind_is_correctly_identified(
            string markupCode,
            string kind)
        {
            MarkupTestFile.GetPosition(markupCode, out var code, out var position);

            var tree = Parse(code);

            tree.RootNode
                .FindNode(position.Value)
                .AncestorsAndSelf()
                .Should()
                .ContainSingle<DirectiveNode>()
                .Which
                .Kind
                .ToString()
                .Should()
                .Be(kind);
        }

        [TestMethod]
        public void Directive_character_ranges_can_be_read()
        {
            var markupCode = @"
[|#!csharp|] 
var x = 123;
x
";

            MarkupTestFile.GetSpan(markupCode, out var code, out var span);

            var tree = Parse(code);

            tree.RootNode
                .ChildNodes
                .Should()
                .ContainSingle<DirectiveNode>()
                .Which
                .Span
                .Should()
                .BeEquivalentTo(span);
        }

        [TestMethod]
        [DataRow(@"{|csharp:    |}", "csharp")]
        [DataRow(@"{|csharp: var x = abc|}", "csharp")]
        [DataRow(@"
#!fsharp
{|fsharp:let x = |}
#!csharp
{|csharp:var x = 123;|}", "csharp")]
        [DataRow(@"
#!fsharp
{|fsharp:let x = |}
#!csharp
{|csharp:var x = 123;|}", "fsharp")]
        [DataRow(@"
#!fsharp
{|fsharp:  let x = |}
#!csharp
{|csharp:  var x = 123;|}", "fsharp")]
        public void Kernel_name_can_be_determined_for_a_given_position(
            string markupCode,
            string defaultLanguage)
        {
            MarkupTestFile.GetNamedSpans(markupCode, out var code, out var spansByName);

            var tree = Parse(code, defaultLanguage);

            using var _ = new AssertionScope();

            foreach (var pair in spansByName)
            {
                var expectedLanguage = pair.Key;
                var spans = pair.Value;

                foreach (var position in spans.SelectMany(s => Enumerable.Range(s.Start, s.Length)))
                {
                    var language = tree.GetKernelNameAtPosition(position);

                    language
                        .Should()
                        .Be(expectedLanguage, because: $"position {position} should be {expectedLanguage}");
                }
            }
        }

        [TestMethod]
        [DataRow("""
                    {|none:#!fsharp |}
                    let x =
                    {|.NET:#!time |}
                    {|none:#!csharp|}
                    {|csharp:#!who |}
                    """, 
                    "fsharp")]
        public void Directive_node_indicates_kernel_name(
            string markupCode,
            string defaultLanguage)
        {
            MarkupTestFile.GetNamedSpans(markupCode, out var code, out var spansByName);

            var tree = Parse(code, defaultLanguage);

            // using var _ = new AssertionScope();

            foreach (var pair in spansByName)
            {
                var expectedParentLanguage = pair.Key;
                var spans = pair.Value;

                foreach (var position in spans.SelectMany(s => Enumerable.Range(s.Start, s.Length)))
                {
                    var node = tree.RootNode.FindNode(position);

                    switch (node.Parent)
                    {
                        case DirectiveNode { Kind: DirectiveNodeKind.KernelSelector }:
                            expectedParentLanguage.Should().Be("none");
                            break;

                        case DirectiveNode { Kind: DirectiveNodeKind.Action } adn:
                            adn.TargetKernelName.Should().Be(expectedParentLanguage);
                            break;

                        default:
                            throw new AssertionFailedException($"Expected a {nameof(DirectiveNode)}  but found: {node}");
                    }
                }
            }
        }

        [TestMethod]
        public void root_node_span_always_expands_with_child_nodes()
        {
            var code = """
            #r "path/to/file"
            // language line
            """;
            var tree = Parse(code);
            var root = tree.RootNode;
            var rootSpan = root.Span;

            root.ChildNodes
                .Should()
                .AllSatisfy(child => rootSpan.Contains(child.Span).Should().BeTrue());
        }

        [TestMethod]
        [DataRow("""
            #!time
            #!set --name x --value 123
            """)]
        [DataRow("""
            #!set --name x --value 123
            #!set --name y --value xyz
            """)]
        public void Directives_do_not_span_line_endings(string code)
        {
            var tree = Parse(code);

            tree.RootNode.ChildNodes.OfType<DirectiveNode>().Should().HaveCount(2);
        }
    }
}