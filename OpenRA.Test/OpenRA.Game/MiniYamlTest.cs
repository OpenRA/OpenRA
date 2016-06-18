#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	}
}
