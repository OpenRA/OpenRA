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
using System.Linq;
using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	public class MiniYamlTest
	{
		readonly string yamlTabStyle = @"
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

		readonly string yamlMixedStyle = @"
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

		[TestCase(TestName = "Mixed tabs & spaces indents")]
		public void TestIndents()
		{
			var tabs = MiniYaml.FromString(yamlTabStyle, "yamlTabStyle").WriteToString();
			Console.WriteLine(tabs);
			var mixed = MiniYaml.FromString(yamlMixedStyle, "yamlMixedStyle").WriteToString();
			Console.WriteLine(mixed);
			Assert.That(tabs, Is.EqualTo(mixed));
		}

		[TestCase(TestName = "Inheritance and removal can be composed")]
		public void InheritanceAndRemovalCanBeComposed()
		{
			var baseYaml = @"
^BaseA:
	MockA2:
^BaseB:
	Inherits@a: ^BaseA
	MockB2:
";
			var extendedYaml = @"
Test:
	Inherits@b: ^BaseB
	-MockA2:
";
			var mapYaml = @"
^BaseC:
	MockC2:
Test:
	Inherits@c: ^BaseC
";
			var result = MiniYaml.Merge(new[] { baseYaml, extendedYaml, mapYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test").Value.Nodes;

			Assert.IsFalse(result.Any(n => n.Key == "MockA2"), "Node should not have the MockA2 child, but does.");
			Assert.IsTrue(result.Any(n => n.Key == "MockB2"), "Node should have the MockB2 child, but does not.");
			Assert.IsTrue(result.Any(n => n.Key == "MockC2"), "Node should have the MockC2 child, but does not.");
		}

		[TestCase(TestName = "Child can be removed after multiple inheritance")]
		public void ChildCanBeRemovedAfterMultipleInheritance()
		{
			var baseYaml = @"
^BaseA:
	MockA2:
Test:
	Inherits: ^BaseA
	MockA2:
";
			var overrideYaml = @"
Test:
	-MockA2
";

			var result = MiniYaml.Merge(new[] { baseYaml, overrideYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test").Value.Nodes;

			Assert.IsFalse(result.Any(n => n.Key == "MockA2"), "Node should not have the MockA2 child, but does.");
		}

		[TestCase(TestName = "Child can be removed and later overridden")]
		public void ChildCanBeRemovedAndLaterOverridden()
		{
			var baseYaml = @"
^BaseA:
    MockString:
        AString: Base
Test:
    Inherits: ^BaseA
    -MockString:
";
			var overrideYaml = @"
Test:
    MockString:
        AString: Override
";

			var result = MiniYaml.Merge(new[] { baseYaml, overrideYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test").Value.Nodes;

			Assert.IsTrue(result.Any(n => n.Key == "MockString"), "Node should have the MockString child, but does not.");
			Assert.IsTrue(result.First(n => n.Key == "MockString").Value.ToDictionary()["AString"].Value == "Override",
				"MockString value has not been set with the correct override value for AString.");
		}

		[TestCase(TestName = "Child can be removed from intermediate parent")]
		public void ChildCanBeOverriddenThenRemoved()
		{
			var baseYaml = @"
^BaseA:
    MockString:
        AString: Base
^BaseB:
    Inherits: ^BaseA
    MockString:
        AString: Override
";
			var overrideYaml = @"
Test:
    Inherits: ^BaseB
    MockString:
    	-AString:
";

			var result = MiniYaml.Merge(new[] { baseYaml, overrideYaml }.Select(s => MiniYaml.FromString(s, "")))
				.First(n => n.Key == "Test").Value.Nodes;
			Assert.IsTrue(result.Any(n => n.Key == "MockString"), "Node should have the MockString child, but does not.");
			Assert.IsFalse(result.First(n => n.Key == "MockString").Value.Nodes.Any(n => n.Key == "AString"),
				"MockString value should have been removed, but was not.");
		}

		[TestCase(TestName = "Empty lines should count toward line numbers")]
		public void EmptyLinesShouldCountTowardLineNumbers()
		{
			var yaml = @"
TestA:
	Nothing:

TestB:
	Nothing:
";

			var result = MiniYaml.FromString(yaml).First(n => n.Key == "TestB");
			Assert.AreEqual(5, result.Location.Line);
		}

		[TestCase(TestName = "Duplicated nodes are correctly merged")]
		public void TestSelfMerging()
		{
			var baseYaml = @"
Test:
	Merge: original
		Child: original
	Original:
Test:
	Merge: override
		Child: override
	Override:
";

			var result = MiniYaml.Merge(new[] { baseYaml }.Select(s => MiniYaml.FromString(s, "")));
			Assert.That(result.Count(n => n.Key == "Test"), Is.EqualTo(1), "Result should have exactly one Test node.");

			var testNodes = result.First(n => n.Key == "Test").Value.Nodes;
			Assert.That(testNodes.Select(n => n.Key), Is.EqualTo(new[] { "Merge", "Original", "Override" }), "Merged Test node has incorrect child nodes.");

			var mergeNode = testNodes.First(n => n.Key == "Merge").Value;
			Assert.That(mergeNode.Value, Is.EqualTo("override"), "Merge node has incorrect value.");
			Assert.That(mergeNode.Nodes[0].Value.Value, Is.EqualTo("override"), "Merge node Child value should be 'override', but is not");
		}

		[TestCase(TestName = "Comments are correctly separated from values")]
		public void TestEscapedHashInValues()
		{
			var trailingWhitespace = MiniYaml.FromString(@"key: value # comment", "trailingWhitespace", discardCommentsAndWhitespace: false)[0];
			Assert.AreEqual("value", trailingWhitespace.Value.Value);
			Assert.AreEqual(" comment", trailingWhitespace.Comment);

			var noWhitespace = MiniYaml.FromString(@"key:value# comment", "noWhitespace", discardCommentsAndWhitespace: false)[0];
			Assert.AreEqual("value", noWhitespace.Value.Value);
			Assert.AreEqual(" comment", noWhitespace.Comment);

			var escapedHashInValue = MiniYaml.FromString(@"key: before \# after # comment", "escapedHashInValue", discardCommentsAndWhitespace: false)[0];
			Assert.AreEqual("before # after", escapedHashInValue.Value.Value);
			Assert.AreEqual(" comment", escapedHashInValue.Comment);

			var emptyValueAndComment = MiniYaml.FromString(@"key:#", "emptyValueAndComment", discardCommentsAndWhitespace: false)[0];
			Assert.AreEqual(null, emptyValueAndComment.Value.Value);
			Assert.AreEqual("", emptyValueAndComment.Comment);

			var noValue = MiniYaml.FromString(@"key:", "noValue", discardCommentsAndWhitespace: false)[0];
			Assert.AreEqual(null, noValue.Value.Value);
			Assert.AreEqual(null, noValue.Comment);

			var emptyKey = MiniYaml.FromString(@" : value", "emptyKey", discardCommentsAndWhitespace: false)[0];
			Assert.AreEqual(null, emptyKey.Key);
			Assert.AreEqual("value", emptyKey.Value.Value);
			Assert.AreEqual(null, emptyKey.Comment);
		}

		[TestCase(TestName = "Leading and trailing whitespace can be guarded using a backslash")]
		public void TestGuardedWhitespace()
		{
			var testYaml = @"key:   \      test value    \   ";
			var nodes = MiniYaml.FromString(testYaml, "testYaml");
			Assert.AreEqual("      test value    ", nodes[0].Value.Value);
		}

		[TestCase(TestName = "Comments should count toward line numbers")]
		public void CommentsShouldCountTowardLineNumbers()
		{
			var yaml = @"
TestA:
	Nothing:

# Comment
TestB:
	Nothing:
";
			var resultDiscard = MiniYaml.FromString(yaml);
			var resultDiscardLine = resultDiscard.First(n => n.Key == "TestB").Location.Line;
			Assert.That(resultDiscardLine, Is.EqualTo(6), "Node TestB should report its location as line 6, but is not (discarding comments)");
			Assert.That(resultDiscard[1].Key, Is.EqualTo("TestB"), "Node TestB should be the second child of the root node, but is not (discarding comments)");

			var resultKeep = MiniYaml.FromString(yaml, discardCommentsAndWhitespace: false);
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
	First: value containing a \# character
	Second: value # node with inline comment
	Third: value #
	Fourth: #
".Replace("\r\n", "\n");

			var result = MiniYaml.FromString(yaml, discardCommentsAndWhitespace: false).WriteToString();
			Assert.AreEqual(yaml, result);
		}

		[TestCase(TestName = "Comments should be removed when discardCommentsAndWhitespace is false")]
		public void CommentsShouldntSurviveRoundTrip()
		{
			var yaml = @"
# Top level comment node
Parent: # comment without value
	# Indented comment node
	First: value containing a \# character
	Second: value # node with inline comment
";

			var strippedYaml = @"Parent:
	First: value containing a \# character
	Second: value
".Replace("\r\n", "\n");

			var result = MiniYaml.FromString(yaml).WriteToString();
			Assert.AreEqual(strippedYaml, result);
		}
	}
}
