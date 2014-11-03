#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;
using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	public class MiniYamlTest
	{
		readonly string yamlForParent = @"
^Parent:
	FromParent:
	FromParentRemove:
";
		readonly string yamlForChild = @"
Child:
	Inherits: ^Parent
	FromChild:
	-FromParentRemove:
";
		List<MiniYamlNode> parentList;
		List<MiniYamlNode> childList;
		MiniYaml parent;
		MiniYaml child;

		[SetUp]
		public void SetUp()
		{
			parentList = MiniYaml.FromString(yamlForParent);
			childList = MiniYaml.FromString(yamlForChild);
			parent = parentList.First().Value;
			child = childList.First().Value;
		}

		void InheritanceTest(List<MiniYamlNode> nodes)
		{
			Assert.That(nodes.Any(n => n.Key == "FromParent"), Is.True, "Node from parent");
			Assert.That(nodes.Any(n => n.Key == "FromChild"), Is.True, "Node from child");
			Assert.That(nodes.Any(n => n.Key == "FromParentRemove"), Is.Not.True, "Node from parent - node from child");
		}

		[TestCase(TestName = "MergeStrict(MiniYaml, MiniYaml)")]
		public void MergeYamlA()
		{
			var res = MiniYaml.MergeStrict(parent, child);
			InheritanceTest(res.Nodes);
		}

		[TestCase(TestName = "MergeLiberal(MiniYaml, MiniYaml)")]
		public void MergeYamlB()
		{
			var res = MiniYaml.MergeLiberal(parent, child);
			InheritanceTest(res.Nodes);
		}

		[TestCase(TestName = "MergeStrict(List<MiniYamlNode>, List<MiniYamlNode>)")]
		public void MergeYamlC()
		{
			var res = MiniYaml.MergeStrict(parentList, childList).Last();
			Assert.That(res.Key, Is.EqualTo("Child"));
			InheritanceTest(res.Value.Nodes);
		}

		[TestCase(TestName = "MergeLiberal(List<MiniYamlNode>, List<MiniYamlNode>)")]
		public void MergeYamlD()
		{
			var res = MiniYaml.MergeLiberal(parentList, childList).Last();
			Assert.That(res.Key, Is.EqualTo("Child"));
			InheritanceTest(res.Value.Nodes);
		}
	}
}
