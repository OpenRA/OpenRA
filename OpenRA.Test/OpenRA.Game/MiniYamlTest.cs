#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	public class MiniYamlTest
	{
		[TestCase(TestName = "Parse tree roundtrips")]
		public void TestParseRoundtrip()
		{
			const string Yaml =
@"1:
2: Test
3: # Test
4:
	4.1:
5: Test
	5.1:
6: # Test
	6.1:
7:
	7.1.1:
	7.1.2: Test
	7.1.3: # Test
8: Test
	8.1.1:
	8.1.2: Test
	8.1.3: # Test
9: # Test
	9.1.1:
	9.1.2: Test
	9.1.3: # Test
";
			var serialized = MiniYaml.FromString(Yaml, "", discardCommentsAndWhitespace: false).WriteToString();
			Console.WriteLine();
			Assert.That(serialized, Is.EqualTo(Yaml));
		}

		[TestCase(TestName = "Parse tree can handle empty lines")]
		public void TestParseEmptyLines()
		{
			const string Yaml =
@"1:

2: Test

3: # Test

4:

	4.1:

5: Test

	5.1:

6: # Test

	6.1:

7:

	7.1.1:

	7.1.2: Test

	7.1.3: # Test

8: Test

	8.1.1:

	8.1.2: Test

	8.1.3: # Test

9: # Test

	9.1.1:

	9.1.2: Test

	9.1.3: # Test

";

			const string ExpectedYaml =
@"1:
2: Test
3:
4:
	4.1:
5: Test
	5.1:
6:
	6.1:
7:
	7.1.1:
	7.1.2: Test
	7.1.3:
8: Test
	8.1.1:
	8.1.2: Test
	8.1.3:
9:
	9.1.1:
	9.1.2: Test
	9.1.3:
";
			var serialized = MiniYaml.FromString(Yaml, "").WriteToString();
			Assert.That(serialized, Is.EqualTo(ExpectedYaml));
		}

		[TestCase(TestName = "Mixed tabs & spaces indents")]
		public void TestIndents()
		{
			const string YamlTabStyle = @"
Root1:
	Child1:
		Attribute1: Test
		Attribute2: Test
	Child2:
		Attribute1: Test
		Attribute2: Test
Root2:
	Child1:
		Attribute1: Test
";

			const string YamlMixedStyle = @"
Root1:
    Child1:
        Attribute1: Test
        Attribute2: Test
	Child2:
		Attribute1: Test
	    Attribute2: Test
Root2:
    Child1:
		Attribute1: Test
";
			var tabs = MiniYaml.FromString(YamlTabStyle, "").WriteToString();
			Console.WriteLine(tabs);
			var mixed = MiniYaml.FromString(YamlMixedStyle, "").WriteToString();
			Console.WriteLine(mixed);
			Assert.That(tabs, Is.EqualTo(mixed));
		}

		[TestCase(TestName = "Yaml files should be able to remove nodes")]
		public void NodeRemoval()
		{
			const string BaseString = @"
Parent:
	Child:
		Key: value
		-Key:
";

			const string ResultString = "Parent:\n\tChild:\n";
			var baseYaml = MiniYaml.FromString(BaseString, "");

			var resultYaml = MiniYaml.Merge([baseYaml]);
			Assert.That(resultYaml.WriteToString(), Is.EqualTo(ResultString));
		}

		[TestCase(TestName = "Yaml files should be able to remove nodes and immediately override")]
		public void NodeRemovalAndOverride()
		{
			const string BaseString = @"
Parent:
	Child:
		Key: value
		-Key:
		Key: value2
";

			const string ResultString = "Parent:\n\tChild:\n\t\tKey: value2\n";
			var baseYaml = MiniYaml.FromString(BaseString, "");

			var resultYaml = MiniYaml.Merge([baseYaml]);
			Assert.That(resultYaml.WriteToString(), Is.EqualTo(ResultString));
		}

		[TestCase(TestName = "Merged yaml files should be able to remove nodes")]
		public void MergedNodeRemoval()
		{
			const string BaseString = @"
Parent:
	Child:
		Key: value
";

			const string MergeString = @"
Parent:
	Child:
		-Key:
";

			const string ResultString = "Parent:\n\tChild:\n";
			var baseYaml = MiniYaml.FromString(BaseString, "");
			var mergeYaml = MiniYaml.FromString(MergeString, "");

			var resultYaml = MiniYaml.Merge([baseYaml, mergeYaml]);
			Assert.That(resultYaml.WriteToString(), Is.EqualTo(ResultString));
		}

		[TestCase(TestName = "Merged yaml files should be able to remove nodes and immediately override")]
		public void MergedNodeRemovalAndOverride()
		{
			const string BaseString = @"
Parent:
	Child:
		Key: value
";

			const string MergeString = @"
Parent:
	Child:
		-Key:
		Key: value2
";

			const string ResultString = "Parent:\n\tChild:\n\t\tKey: value2\n";
			var baseYaml = MiniYaml.FromString(BaseString, "");
			var mergeYaml = MiniYaml.FromString(MergeString, "");

			var resultYaml = MiniYaml.Merge([baseYaml, mergeYaml]);
			Assert.That(resultYaml.WriteToString(), Is.EqualTo(ResultString));
		}

		[TestCase(TestName = "Merged yaml files should be able to remove nodes from inherited parents")]
		public void MergedInheritedNodeRemoval()
		{
			const string BaseString = @"
^Base:
	Child:
		Key: value
Parent:
	Inherits: ^Base
";

			const string MergeString = @"
Parent:
	Child:
		-Key:
";

			const string ResultString = "^Base:\n\tChild:\n\t\tKey: value\nParent:\n\tChild:\n";
			var baseYaml = MiniYaml.FromString(BaseString, "");
			var mergeYaml = MiniYaml.FromString(MergeString, "");

			var resultYaml = MiniYaml.Merge([baseYaml, mergeYaml]);
			Assert.That(resultYaml.WriteToString(), Is.EqualTo(ResultString));
		}

		[TestCase(TestName = "Merged yaml files should be able to remove nodes from inherited parents and immediately override")]
		public void MergedInheritedNodeRemovalAndOverride()
		{
			const string BaseString = @"
^Base:
	Child:
		Key: value
Parent:
	Inherits: ^Base
";

			const string MergeString = @"
Parent:
	Child:
		-Key:
		Key: value2
";

			const string ResultString = "^Base:\n\tChild:\n\t\tKey: value\nParent:\n\tChild:\n\t\tKey: value2\n";
			var baseYaml = MiniYaml.FromString(BaseString, "");
			var mergeYaml = MiniYaml.FromString(MergeString, "");

			var resultYaml = MiniYaml.Merge([baseYaml, mergeYaml]);
			Assert.That(resultYaml.WriteToString(), Is.EqualTo(ResultString));
		}

		[TestCase(TestName = "Inheritance and removal can be composed")]
		public void InheritanceAndRemovalCanBeComposed()
		{
			const string BaseYaml = @"
^BaseA:
	MockA2:
^BaseB:
	Inherits@a: ^BaseA
	MockB2:
";
			const string ExtendedYaml = @"
Test:
	Inherits@b: ^BaseB
	-MockA2:
";
			const string MapYaml = @"
^BaseC:
	MockC2:
Test:
	Inherits@c: ^BaseC
";
			var result = MiniYaml.Merge(new[] { BaseYaml, ExtendedYaml, MapYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test").Value.Nodes;

			Assert.That(result.Any(n => n.Key == "MockA2"), Is.False, "Node should not have the MockA2 child, but does.");
			Assert.That(result.Any(n => n.Key == "MockB2"), Is.True, "Node should have the MockB2 child, but does not.");
			Assert.That(result.Any(n => n.Key == "MockC2"), Is.True, "Node should have the MockC2 child, but does not.");
		}

		[TestCase(TestName = "Child can be removed after multiple inheritance")]
		public void ChildCanBeRemovedAfterMultipleInheritance()
		{
			const string BaseYaml = @"
^BaseA:
	MockA2:
Test:
	Inherits: ^BaseA
	MockA2:
";
			const string OverrideYaml = @"
Test:
	-MockA2
";

			var result = MiniYaml.Merge(new[] { BaseYaml, OverrideYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test").Value.Nodes;

			Assert.That(result.Any(n => n.Key == "MockA2"), Is.False, "Node should not have the MockA2 child, but does.");
		}

		[TestCase(TestName = "Inherited child can be immediately removed")]
		public void InheritedChildCanBeImmediatelyRemoved()
		{
			const string BaseYaml = @"
^BaseA:
	MockString:
		AString: Base
Test:
	Inherits: ^BaseA
	MockString:
		AString: Override
	-MockString:
";

			var result = MiniYaml.Merge(new[] { BaseYaml }.Select(s => MiniYaml.FromString(s, "")))
				 .First(n => n.Key == "Test").Value.Nodes;

			Assert.That(result.Any(n => n.Key == "MockString"), Is.False, "Node should not have the MockString child, but does.");
		}

		[TestCase(TestName = "Inherited child can be removed and immediately overridden")]
		public void InheritedChildCanBeRemovedAndImmediatelyOverridden()
		{
			const string BaseYaml = @"
^BaseA:
	MockString:
		AString: Base
Test:
	Inherits: ^BaseA
	-MockString:
	MockString:
		AString: Override
";

			var result = MiniYaml.Merge(new[] { BaseYaml }.Select(s => MiniYaml.FromString(s, "")))
				 .First(n => n.Key == "Test").Value.Nodes;

			Assert.That(result.Any(n => n.Key == "MockString"), Is.True, "Node should have the MockString child, but does not.");
			Assert.That(result.First(n => n.Key == "MockString").Value.NodeWithKey("AString").Value.Value == "Override", Is.True,
				"MockString value has not been set with the correct override value for AString.");
		}

		[TestCase(TestName = "Inherited child can be removed and later overridden")]
		public void InheritedChildCanBeRemovedAndLaterOverridden()
		{
			const string BaseYaml = @"
^BaseA:
	MockString:
		AString: Base
Test:
	Inherits: ^BaseA
	-MockString:
";
			const string OverrideYaml = @"
Test:
	MockString:
		AString: Override
";

			var result = MiniYaml.Merge(new[] { BaseYaml, OverrideYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test").Value.Nodes;

			Assert.That(result.Any(n => n.Key == "MockString"), Is.True, "Node should have the MockString child, but does not.");
			Assert.That(result.First(n => n.Key == "MockString").Value.NodeWithKey("AString").Value.Value == "Override", Is.True,
				"MockString value has not been set with the correct override value for AString.");
		}

		[TestCase(TestName = "Inherited child can be removed from intermediate parent")]
		public void InheritedChildCanBeOverriddenThenRemoved()
		{
			const string BaseYaml = @"
^BaseA:
	MockString:
		AString: Base
^BaseB:
	Inherits: ^BaseA
	MockString:
		AString: Override
";
			const string OverrideYaml = @"
Test:
	Inherits: ^BaseB
	MockString:
		-AString:
";

			var result = MiniYaml.Merge(new[] { BaseYaml, OverrideYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test").Value.Nodes;
			Assert.That(result.Any(n => n.Key == "MockString"), Is.True, "Node should have the MockString child, but does not.");
			Assert.That(result.First(n => n.Key == "MockString").Value.Nodes.Any(n => n.Key == "AString"), Is.False,
				"MockString value should have been removed, but was not.");
		}

		[TestCase(TestName = "Merged child subnode can be removed and immediately overridden")]
		public void MergedChildSubNodeCanBeRemovedAndImmediatelyOverridden()
		{
			const string BaseYaml = @"
Test:
	MockString:
		CollectionOfStrings:
			StringA: A
			StringB: B
Test:
	MockString:
		-CollectionOfStrings:
		CollectionOfStrings:
			StringC: C
";

			var merged = MiniYaml.Merge(new[] { BaseYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test");

			var traitNode = merged.Value.Nodes.Single();
			var fieldNodes = traitNode.Value.Nodes;
			var fieldSubNodes = fieldNodes.Single().Value.Nodes;

			Assert.That(fieldSubNodes.Length == 1, Is.True, "Collection of strings should only contain the overriding subnode.");
			Assert.That(fieldSubNodes.Single(n => n.Key == "StringC").Value.Value == "C", Is.True,
				"CollectionOfStrings value has not been set with the correct override value for StringC.");
		}

		[TestCase(TestName = "Merged child subnode can be removed and later overridden")]
		public void MergedChildSubNodeCanBeRemovedAndLaterOverridden()
		{
			const string BaseYaml = @"
Test:
	MockString:
		CollectionOfStrings:
			StringA: A
			StringB: B
Test:
	MockString:
		-CollectionOfStrings:
";

			const string OverrideYaml = @"
Test:
	MockString:
		CollectionOfStrings:
			StringC: C
";

			var merged = MiniYaml.Merge(new[] { BaseYaml, OverrideYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test");

			var traitNode = merged.Value.Nodes.Single();
			var fieldNodes = traitNode.Value.Nodes;
			var fieldSubNodes = fieldNodes.Single().Value.Nodes;

			Assert.That(fieldSubNodes.Length == 1, Is.True, "Collection of strings should only contain the overriding subnode.");
			Assert.That(fieldSubNodes.Single(n => n.Key == "StringC").Value.Value == "C", Is.True,
				"CollectionOfStrings value has not been set with the correct override value for StringC.");
		}

		[TestCase(TestName = "Inherited child subnode can be removed and immediately overridden")]
		public void InheritedChildSubNodeCanBeRemovedAndImmediatelyOverridden()
		{
			const string BaseYaml = @"
^BaseA:
	MockString:
		CollectionOfStrings:
			StringA: A
			StringB: B
Test:
	Inherits: ^BaseA
	MockString:
		-CollectionOfStrings:
		CollectionOfStrings:
			StringC: C
";

			var merged = MiniYaml.Merge(new[] { BaseYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test");

			var traitNode = merged.Value.Nodes.Single();
			var fieldNodes = traitNode.Value.Nodes;
			var fieldSubNodes = fieldNodes.Single().Value.Nodes;

			Assert.That(fieldSubNodes.Length == 1, Is.True, "Collection of strings should only contain the overriding subnode.");
			Assert.That(fieldSubNodes.Single(n => n.Key == "StringC").Value.Value == "C", Is.True,
				"CollectionOfStrings value has not been set with the correct override value for StringC.");
		}

		[TestCase(TestName = "Inherited child subnode can be removed and later overridden")]
		public void InheritedChildSubNodeCanBeRemovedAndLaterOverridden()
		{
			const string BaseYaml = @"
^BaseA:
	MockString:
		CollectionOfStrings:
			StringA: A
			StringB: B
Test:
	Inherits: ^BaseA
	MockString:
		-CollectionOfStrings:
";

			const string OverrideYaml = @"
Test:
	MockString:
		CollectionOfStrings:
			StringC: C
";

			var merged = MiniYaml.Merge(new[] { BaseYaml, OverrideYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test");

			var traitNode = merged.Value.Nodes.Single();
			var fieldNodes = traitNode.Value.Nodes;
			var fieldSubNodes = fieldNodes.Single().Value.Nodes;

			Assert.That(fieldSubNodes.Length == 1, Is.True, "Collection of strings should only contain the overriding subnode.");
			Assert.That(fieldSubNodes.Single(n => n.Key == "StringC").Value.Value == "C", Is.True,
				"CollectionOfStrings value has not been set with the correct override value for StringC.");
		}

		[TestCase(TestName = "Inheritance works for nested nodes")]
		public void InheritanceWorksForNestedNodes()
		{
			const string BaseYaml = @"
^DefaultKey:
	Key: value
";
			const string ExtendedYaml = @"
Parent:
	Child:
		Inherits: ^DefaultKey
";

			const string ResultString =
@"^DefaultKey:
	Key: value
Parent:
	Child:
		Key: value
";
			var baseYaml = MiniYaml.FromString(BaseYaml, "");
			var mergeYaml = MiniYaml.FromString(ExtendedYaml, "");

			var resultYaml = MiniYaml.Merge([baseYaml, mergeYaml]);
			Assert.That(resultYaml.WriteToString(), Is.EqualTo(ResultString));
		}

		[TestCase(TestName = "Empty lines should count toward line numbers")]
		public void EmptyLinesShouldCountTowardLineNumbers()
		{
			const string Yaml = @"
TestA:
	Nothing:

TestB:
	Nothing:
";

			var result = MiniYaml.FromString(Yaml, "").First(n => n.Key == "TestB");
			Assert.That(5, Is.EqualTo(result.Location.Line));
		}

		[TestCase(TestName = "Duplicated nodes are correctly merged")]
		public void TestSelfMerging()
		{
			const string BaseYaml = @"
Test:
	Merge: original
		Child: original
	Original:
Test:
	Merge: override
		Child: override
	Override:
";

			var result = MiniYaml.Merge(new[] { BaseYaml }.Select(s => MiniYaml.FromString(s, "")));
			Assert.That(result.Count(n => n.Key == "Test"), Is.EqualTo(1), "Result should have exactly one Test node.");

			var testNodes = result.First(n => n.Key == "Test").Value.Nodes;
			Assert.That(testNodes.Select(n => n.Key), Is.EqualTo(["Merge", "Original", "Override"]), "Merged Test node has incorrect child nodes.");

			var mergeNode = testNodes.First(n => n.Key == "Merge").Value;
			Assert.That(mergeNode.Value, Is.EqualTo("override"), "Merge node has incorrect value.");
			Assert.That(mergeNode.Nodes[0].Value.Value, Is.EqualTo("override"), "Merge node Child value should be 'override', but is not");
		}

		[TestCase(TestName = "Duplicated nodes across multiple sources are correctly merged")]
		public void TestSelfMergingMultiSource()
		{
			const string FirstYaml = @"
Test:
	Merge: original
		Child: original
	Original:
";
			const string SecondYaml = @"
Test:
	Merge: original
		Child: original
	Original:
Test:
	Merge: override
		Child: override
	Override:
";

			var result = MiniYaml.Merge(new[] { FirstYaml, SecondYaml }.Select(s => MiniYaml.FromString(s, "")));
			Assert.That(result.Count(n => n.Key == "Test"), Is.EqualTo(1), "Result should have exactly one Test node.");

			var testNodes = result.First(n => n.Key == "Test").Value.Nodes;
			Assert.That(testNodes.Select(n => n.Key), Is.EqualTo(["Merge", "Original", "Override"]), "Merged Test node has incorrect child nodes.");

			var mergeNode = testNodes.First(n => n.Key == "Merge").Value;
			Assert.That(mergeNode.Value, Is.EqualTo("override"), "Merge node has incorrect value.");
			Assert.That(mergeNode.Nodes[0].Value.Value, Is.EqualTo("override"), "Merge node Child value should be 'override', but is not");
		}

		[TestCase(TestName = "Duplicated child nodes throw merge error if parent does not require merging")]
		public void TestMergeConflictsNoMerge()
		{
			const string BaseYaml = @"
Test:
	Merge:
		Child:
		Child:
";

			static void Merge() => MiniYaml.Merge(new[] { BaseYaml }.Select(s => MiniYaml.FromString(s, "test-filename")));

			Assert.That(Merge, Throws.Exception.TypeOf<YamlException>().And.Message.EqualTo(
				"MiniYaml.Merge, duplicate values found for the following keys: Child: [Child (at test-filename:4),Child (at test-filename:5)]"));
		}

		[TestCase(TestName = "Duplicated removal nodes throw removal error")]
		public void TestDuplicatedRemovals()
		{
			const string BaseYaml = @"
Test:
	Merge:
		Child:
		-Child:
		-Child:
";

			static void Merge() => MiniYaml.Merge(new[] { BaseYaml }.Select(s => MiniYaml.FromString(s, "test-filename")));

			Assert.That(Merge, Throws.Exception.TypeOf<YamlException>().And.Message.EqualTo(
				"test-filename:6: There are no elements with key `Child` to remove"));
		}

		[TestCase(TestName = "Duplicated child nodes with intervening removals do not throw if parent does not require merging")]
		public void TestMergeConflictsNoMergeWithRemovals()
		{
			const string BaseYaml = @"
Test:
	Merge:
		ChildA:
		ChildB:
		-ChildA:
		ChildA:
		-ChildB:
		ChildB:
";

			const string ResultString = "Test:\n\tMerge:\n\t\tChildA:\n\t\tChildB:\n";
			var baseYaml = MiniYaml.FromString(BaseYaml, "");

			var resultYaml = MiniYaml.Merge([baseYaml]);
			Assert.That(resultYaml.WriteToString(), Is.EqualTo(ResultString));
		}

		[TestCase(TestName = "Duplicated child nodes with insufficient intervening removals throw merge error")]
		public void TestMergeConflictsNoMergeWithInsufficientRemovals()
		{
			const string BaseYaml = @"
Test:
	Merge:
		-ChildA:
		-ChildB:
		ChildA:
		ChildB:
		ChildA:
		-ChildB:
		ChildB:
";

			static void Merge() => MiniYaml.Merge(new[] { BaseYaml }.Select(s => MiniYaml.FromString(s, "test-filename")));

			Assert.That(Merge, Throws.Exception.TypeOf<YamlException>().And.Message.EqualTo(
				"MiniYaml.Merge, duplicate values found for the following keys: ChildA: [ChildA (at test-filename:6),ChildA (at test-filename:8)]"));
		}

		[TestCase(TestName = "Duplicated child nodes with intervening removals across multiple source do not throw")]
		public void TestMergeMultiSourceWithRemovals()
		{
			const string BaseYaml = @"
Test:
	Merge:
		ChildA:
		ChildB:
";

			const string OverrideYaml = @"
Test:
	Merge:
		-ChildB:
		ChildA:
		ChildB:
		-ChildA:
		-ChildB:
";

			const string ResultString = "Test:\n\tMerge:\n";
			var baseYaml = MiniYaml.FromString(BaseYaml, "");
			var overrideYaml = MiniYaml.FromString(OverrideYaml, "");

			var resultYaml = MiniYaml.Merge([baseYaml, overrideYaml]);
			Assert.That(resultYaml.WriteToString(), Is.EqualTo(ResultString));
		}

		[TestCase(TestName = "Duplicated child nodes throw merge error if first parent requires merging")]
		public void TestMergeConflictsFirstParent()
		{
			const string BaseYaml = @"
Test:
	Merge:
		Child1:
		Child1:
	Merge:
";

			static void Merge() => MiniYaml.Merge(new[] { BaseYaml }.Select(s => MiniYaml.FromString(s, "test-filename")));

			Assert.That(Merge, Throws.Exception.TypeOf<YamlException>().And.Message.EqualTo(
				"MiniYaml.Merge, duplicate values found for the following keys: Child1: [Child1 (at test-filename:4),Child1 (at test-filename:5)]"));
		}

		[TestCase(TestName = "Duplicated child nodes throw merge error if second parent requires merging")]
		public void TestMergeConflictsSecondParent()
		{
			const string BaseYaml = @"
Test:
	Merge:
	Merge:
		Child2:
		Child2:
";

			static void Merge() => MiniYaml.Merge(new[] { BaseYaml }.Select(s => MiniYaml.FromString(s, "test-filename")));

			Assert.That(Merge, Throws.Exception.TypeOf<YamlException>().And.Message.EqualTo(
				"MiniYaml.Merge, duplicate values found for the following keys: Child2: [Child2 (at test-filename:5),Child2 (at test-filename:6)]"));
		}

		[TestCase(TestName = "Duplicated child nodes across multiple sources do not throw")]
		public void TestMergeConflictsMultiSourceMerge()
		{
			const string FirstYaml = @"
Test:
	Merge:
		Child:
";
			const string SecondYaml = @"
Test:
	Merge:
		Child:
";

			var result = MiniYaml.Merge(new[] { FirstYaml, SecondYaml }.Select(s => MiniYaml.FromString(s, "")));
			var testNodes = result.First(n => n.Key == "Test").Value.Nodes;
			var mergeNode = testNodes.First(n => n.Key == "Merge").Value;
			Assert.That(mergeNode.Nodes.Count, Is.EqualTo(1));
		}

		[TestCase(TestName = "Duplicated child nodes across multiple sources throw merge error if first parent requires merging")]
		public void TestMergeConflictsMultiSourceFirstParent()
		{
			const string FirstYaml = @"
Test:
	Merge:
		Child1:
		Child1:
";
			const string SecondYaml = @"
Test:
	Merge:
";

			static void Merge() => MiniYaml.Merge(new[] { FirstYaml, SecondYaml }.Select(s => MiniYaml.FromString(s, "test-filename")));

			Assert.That(Merge, Throws.Exception.TypeOf<YamlException>().And.Message.EqualTo(
				"MiniYaml.Merge, duplicate values found for the following keys: Child1: [Child1 (at test-filename:4),Child1 (at test-filename:5)]"));
		}

		[TestCase(TestName = "Duplicated child nodes across multiple sources throw merge error if second parent requires merging")]
		public void TestMergeConflictsMultiSourceSecondParent()
		{
			const string FirstYaml = @"
Test:
	Merge:
";
			const string SecondYaml = @"
Test:
	Merge:
		Child2:
		Child2:
";

			static void Merge() => MiniYaml.Merge(new[] { FirstYaml, SecondYaml }.Select(s => MiniYaml.FromString(s, "test-filename")));

			Assert.That(Merge, Throws.Exception.TypeOf<YamlException>().And.Message.EqualTo(
				"MiniYaml.Merge, duplicate values found for the following keys: Child2: [Child2 (at test-filename:4),Child2 (at test-filename:5)]"));
		}

		[TestCase(TestName = "Comments are correctly separated from values")]
		public void TestEscapedHashInValues()
		{
			var trailingWhitespace = MiniYaml.FromString("key: value # comment", "", discardCommentsAndWhitespace: false).Single();
			Assert.That("value", Is.EqualTo(trailingWhitespace.Value.Value));
			Assert.That(" comment", Is.EqualTo(trailingWhitespace.Comment));

			var noWhitespace = MiniYaml.FromString("key:value# comment", "", discardCommentsAndWhitespace: false).Single();
			Assert.That("value", Is.EqualTo(noWhitespace.Value.Value));
			Assert.That(" comment", Is.EqualTo(noWhitespace.Comment));

			var escapedHashInValue = MiniYaml.FromString(@"key: before \# after # comment", "", discardCommentsAndWhitespace: false).Single();
			Assert.That("before # after", Is.EqualTo(escapedHashInValue.Value.Value));
			Assert.That(" comment", Is.EqualTo(escapedHashInValue.Comment));

			var emptyValueAndComment = MiniYaml.FromString("key:#", "", discardCommentsAndWhitespace: false).Single();
			Assert.That(null, Is.EqualTo(emptyValueAndComment.Value.Value));
			Assert.That("", Is.EqualTo(emptyValueAndComment.Comment));

			var noValue = MiniYaml.FromString("key:", "", discardCommentsAndWhitespace: false).Single();
			Assert.That(null, Is.EqualTo(noValue.Value.Value));
			Assert.That(null, Is.EqualTo(noValue.Comment));

			var emptyKey = MiniYaml.FromString(" : value", "", discardCommentsAndWhitespace: false).Single();
			Assert.That(null, Is.EqualTo(emptyKey.Key));
			Assert.That("value", Is.EqualTo(emptyKey.Value.Value));
			Assert.That(null, Is.EqualTo(emptyKey.Comment));
		}

		[TestCase(TestName = "Leading and trailing whitespace can be guarded using a backslash")]
		public void TestGuardedWhitespace()
		{
			const string TestYaml = @"key:   \      test value    \   ";
			var nodes = MiniYaml.FromString(TestYaml, "");
			Assert.That("      test value    ", Is.EqualTo(nodes.Single().Value.Value));
		}

		[TestCase(TestName = "Comments should count toward line numbers")]
		public void CommentsShouldCountTowardLineNumbers()
		{
			const string Yaml = @"
TestA:
	Nothing:

# Comment
TestB:
	Nothing:
";
			var resultDiscard = MiniYaml.FromString(Yaml, "").ToList();
			var resultDiscardLine = resultDiscard.First(n => n.Key == "TestB").Location.Line;
			Assert.That(resultDiscardLine, Is.EqualTo(6), "Node TestB should report its location as line 6, but is not (discarding comments)");
			Assert.That(resultDiscard[1].Key, Is.EqualTo("TestB"), "Node TestB should be the second child of the root node, but is not (discarding comments)");

			var resultKeep = MiniYaml.FromString(Yaml, "", discardCommentsAndWhitespace: false).ToList();
			var resultKeepLine = resultKeep.First(n => n.Key == "TestB").Location.Line;
			Assert.That(resultKeepLine, Is.EqualTo(6), "Node TestB should report its location as line 6, but is not (parsing comments)");
			Assert.That(resultKeep[4].Key, Is.EqualTo("TestB"), "Node TestB should be the fifth child of the root node, but is not (parsing comments)");
		}

		[TestCase(TestName = "Comments should survive a round trip intact")]
		public void CommentsSurviveRoundTrip()
		{
			var yaml = @"
# Top level comment node
#
Parent: # comment without value
	# Indented comment node
	#
		# Double Indented comment node
		#
			# Triple Indented comment node
			#
	First: value containing a \# character
	Second: value # node with inline comment
	Third: value #
	Fourth: #
	Fifth# embedded comment:
	Sixth# embedded comment: still a comment
	Seventh# embedded comment: still a comment # more comment
".Replace("\r\n", "\n");

			var canonicalYaml = @"
# Top level comment node
#
Parent: # comment without value
	# Indented comment node
	#
		# Double Indented comment node
		#
			# Triple Indented comment node
			#
	First: value containing a \# character
	Second: value # node with inline comment
	Third: value #
	Fourth: #
	Fifth: # embedded comment:
	Sixth: # embedded comment: still a comment
	Seventh: # embedded comment: still a comment # more comment
".Replace("\r\n", "\n");

			var result = MiniYaml.FromString(yaml, "", discardCommentsAndWhitespace: false).WriteToString();
			Assert.That(canonicalYaml, Is.EqualTo(result));
		}

		[TestCase(TestName = "Comments should be removed when discardCommentsAndWhitespace is false")]
		public void CommentsShouldntSurviveRoundTrip()
		{
			const string Yaml = @"
# Top level comment node
#
Parent: # comment without value
	# Indented comment node
	#
		# Double Indented comment node
		#
			# Triple Indented comment node
			#
	First: value containing a \# character
	Second: value # node with inline comment
	Third: value #
	Fourth: #
	Fifth# embedded comment:
	Sixth# embedded comment: still a comment
	Seventh# embedded comment: still a comment # more comment
";

			var strippedYaml = @"Parent:
	First: value containing a \# character
	Second: value
	Third: value
	Fourth:
	Fifth:
	Sixth:
	Seventh:
".Replace("\r\n", "\n");

			var result = MiniYaml.FromString(Yaml, "").WriteToString();
			Assert.That(strippedYaml, Is.EqualTo(result));
		}

		[TestCase(TestName = "Can enumerate top-level nodes from a stream")]
		public void FromStreamAsEnumerable()
		{
			const string FirstYaml =
@"Parent: First
	Child: First
Parent: Second
";

			const string SecondYaml =
@"	Child: Second
";
			var events = new List<(string Event, string Payload)>();
			var stream = new TestStream();
			var ars = new AutoResetEvent(false);

			var readTask = Task.Run(() =>
			{
				foreach (var node in MiniYaml.FromStream(stream, ""))
				{
					events.Add(("Saw Node", new[] { node }.WriteToString()));
					ars.Set();
				}
			});

			events.Add(("Stream Write", FirstYaml));
			stream.WriteBytes(Encoding.UTF8.GetBytes(FirstYaml));
			if (!ars.WaitOne(TimeSpan.FromSeconds(1)))
				Assert.Fail("Timeout waiting for first node");

			events.Add(("Stream Write", SecondYaml));
			stream.WriteBytes(Encoding.UTF8.GetBytes(SecondYaml));

			events.Add(("Stream End", ""));
			stream.WriteEnd();
			if (!ars.WaitOne(TimeSpan.FromSeconds(1)))
				Assert.Fail("Timeout waiting for second node");

			if (!readTask.Wait(TimeSpan.FromSeconds(1)))
				Assert.Fail("Timeout waiting for task completion");

			Assert.That(events, Is.EquivalentTo([
				("Stream Write", FirstYaml),
				("Saw Node", "Parent: First\n\tChild: First\n"),
				("Stream Write", SecondYaml),
				("Stream End", ""),
				("Saw Node", "Parent: Second\n\tChild: Second\n"),
			]));
		}

		sealed class TestStream : Stream
		{
			readonly ManualResetEventSlim mres = new();
			readonly List<byte> bytes = [];
			bool ended;

			public void WriteEnd()
			{
				ended = true;
				mres.Set();
			}

			public void WriteBytes(ReadOnlySpan<byte> bytes)
			{
				if (ended) throw new InvalidOperationException();
				lock (this.bytes)
				{
					this.bytes.AddRange(bytes);
					mres.Set();
				}
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (bytes.Count == 0 && ended)
					return 0;

				if (bytes.Count == 0)
					mres.Wait();

				lock (bytes)
				{
					var read = Math.Min(bytes.Count, count);

					for (var i = 0; i < read; i++)
						buffer[offset + i] = bytes[i];

					bytes.RemoveRange(0, read);

					if (bytes.Count == 0)
						mres.Reset();

					return read;
				}
			}

			public override bool CanRead => true;
			public override bool CanSeek => false;
			public override bool CanWrite => false;
			public override long Length => throw new NotSupportedException();
			public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
			public override void Flush() { }
			public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
			public override void SetLength(long value) => throw new NotSupportedException();
			public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		}
	}
}
