#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		readonly string mixedMergeA = @"
Merge:
	FromA:
	FromARemovedB:
	FromARemovedA:
	-FromBRemovedA:
	-FromARemovedA:
";

		readonly string mixedMergeB = @"
Merge:
	FromB:
	FromBRemovedA:
	FromBRemovedB:
	-FromARemovedB:
	-FromBRemovedB:
";

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

		[TestCase(TestName = "Merging: mixed addition and removal")]
		public void MergeYamlA()
		{
			var a = MiniYaml.FromString(mixedMergeA, "mixedMergeA");
			var b = MiniYaml.FromString(mixedMergeB, "mixedMergeB");

			// Merge order should not matter
			// Note: All the Merge* variants are different plumbing over the same
			// Internal logic.  Testing only Merge is sufficient.
			TestMixedMerge(MiniYaml.Merge(a, b).First().Value);
			TestMixedMerge(MiniYaml.Merge(b, a).First().Value);
		}

		void TestMixedMerge(MiniYaml result)
		{
			Console.WriteLine(result.ToLines("result").JoinWith("\n"));
			Assert.That(result.Nodes.Any(n => n.Key == "FromA"), Is.True, "Node from A");
			Assert.That(result.Nodes.Any(n => n.Key == "FromB"), Is.True, "Node from B");
			Assert.That(result.Nodes.Any(n => n.Key == "FromARemovedA"), Is.Not.True, "Node from A removed by A");
			Assert.That(result.Nodes.Any(n => n.Key == "FromARemovedB"), Is.Not.True, "Node from A removed by B");
			Assert.That(result.Nodes.Any(n => n.Key == "FromBRemovedA"), Is.Not.True, "Node from B removed by A");
			Assert.That(result.Nodes.Any(n => n.Key == "FromBRemovedB"), Is.Not.True, "Node from B removed by B");
		}

		[TestCase(TestName = "Mixed tabs & spaces indents")]
		public void TestIndents()
		{
			var tabs = MiniYaml.FromString(yamlTabStyle, "yamlTabStyle").WriteToString();
			Console.WriteLine(tabs);
			var mixed = MiniYaml.FromString(yamlMixedStyle, "yamlMixedStyle").WriteToString();
			Console.WriteLine(mixed);
			Assert.That(tabs, Is.EqualTo(mixed));
		}
	}
}
