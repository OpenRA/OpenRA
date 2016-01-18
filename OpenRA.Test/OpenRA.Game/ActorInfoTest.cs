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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Traits;

namespace OpenRA.Test
{
	interface IMock : ITraitInfo { }
	class MockTraitInfo : ITraitInfo { public object Create(ActorInitializer init) { return null; } }
	class MockInheritInfo : MockTraitInfo { }
	class MockAInfo : MockInheritInfo, IMock { }
	class MockBInfo : MockTraitInfo, Requires<MockAInfo>, Requires<IMock>, Requires<MockInheritInfo> { }
	class MockCInfo : MockTraitInfo, Requires<MockBInfo> { }
	class MockDInfo : MockTraitInfo, Requires<MockEInfo> { }
	class MockEInfo : MockTraitInfo, Requires<MockFInfo> { }
	class MockFInfo : MockTraitInfo, Requires<MockDInfo> { }

	class MockA2Info : MockTraitInfo { }
	class MockB2Info : MockTraitInfo { }
	class MockC2Info : MockTraitInfo { }

	[TestFixture]
	public class ActorInfoTest
	{
		[SetUp]
		public void SetUp()
		{
		}

		[TestCase(TestName = "Sort traits in order of dependency")]
		public void TraitsInConstructOrderA()
		{
			var actorInfo = new ActorInfo("test", new MockCInfo(), new MockBInfo(), new MockAInfo());

			var i = new List<ITraitInfo>(actorInfo.TraitsInConstructOrder());

			Assert.That(i[0], Is.InstanceOf<MockAInfo>());
			Assert.That(i[1].GetType().Name, Is.EqualTo("MockBInfo"));
			Assert.That(i[2].GetType().Name, Is.EqualTo("MockCInfo"));
		}

		[TestCase(TestName = "Exception reports missing dependencies")]
		public void TraitsInConstructOrderB()
		{
			var actorInfo = new ActorInfo("test", new MockBInfo(), new MockCInfo());

			try
			{
				actorInfo.TraitsInConstructOrder();
				throw new Exception("Exception not thrown!");
			}
			catch (Exception e)
			{
				// Is.StringContaining is deprecated in NUnit 3, but we need to support NUnit 2 so we ignore the warning.
				#pragma warning disable CS0618
				Assert.That(e.Message, Is.StringContaining("MockA"));
				Assert.That(e.Message, Is.StringContaining("MockB"));
				Assert.That(e.Message, Is.StringContaining("MockC"));
				Assert.That(e.Message, Is.StringContaining("MockInherit"), "Should recognize base classes");
				Assert.That(e.Message, Is.StringContaining("IMock"), "Should recognize interfaces");
				#pragma warning restore CS0618
			}
		}

		[TestCase(TestName = "Exception reports cyclic dependencies")]
		public void TraitsInConstructOrderC()
		{
			var actorInfo = new ActorInfo("test", new MockDInfo(), new MockEInfo(), new MockFInfo());

			try
			{
				actorInfo.TraitsInConstructOrder();
				throw new Exception("Exception not thrown!");
			}
			catch (Exception e)
			{
				var count = (
					new Regex("MockD").Matches(e.Message).Count +
					new Regex("MockE").Matches(e.Message).Count +
					new Regex("MockF").Matches(e.Message).Count) / 3.0;

				Assert.That(count, Is.EqualTo(Math.Floor(count)), "Should be symmetrical");
			}
		}

		[TestCase(TestName = "Trait inheritance and removal can be composed")]
		public void TraitInheritanceAndRemovalCanBeComposed()
		{
			var baseYaml = @"
^BaseA:
	MockA2:
^BaseB:
	Inherits@a: ^BaseA
	MockB2:
";
			var extendedYaml = @"
Actor:
	Inherits@b: ^BaseB
	-MockA2:
";
			var mapYaml = @"
^BaseC:
	MockC2:
Actor:
	Inherits@c: ^BaseC
";

			var actorInfo = CreateActorInfoFromYaml("Actor", mapYaml, baseYaml, extendedYaml);
			Assert.IsFalse(actorInfo.HasTraitInfo<MockA2Info>(), "Actor should not have the MockA2 trait, but does.");
			Assert.IsTrue(actorInfo.HasTraitInfo<MockB2Info>(), "Actor should have the MockB2 trait, but does not.");
			Assert.IsTrue(actorInfo.HasTraitInfo<MockC2Info>(), "Actor should have the MockC2 trait, but does not.");
		}

		[TestCase(TestName = "Trait can be removed after multiple inheritance")]
		public void TraitCanBeRemovedAfterMultipleInheritance()
		{
			var baseYaml = @"
^BaseA:
	MockA2:
Actor:
	Inherits: ^BaseA
	MockA2:
";
			var overrideYaml = @"
Actor:
	-MockA2
";

			var actorInfo = CreateActorInfoFromYaml("Actor", null, baseYaml, overrideYaml);
			Assert.IsFalse(actorInfo.HasTraitInfo<MockA2Info>(), "Actor should not have the MockA2 trait, but does.");
		}

		// This needs to match the logic used in RulesetCache.LoadYamlRules
		ActorInfo CreateActorInfoFromYaml(string name, string mapYaml, params string[] yamls)
		{
			var initialNodes = mapYaml == null ? new List<MiniYamlNode>() : MiniYaml.FromString(mapYaml);
			var yaml = yamls
				.Select(s => MiniYaml.FromString(s))
				.Aggregate(initialNodes, MiniYaml.MergePartial);
			var allUnits = yaml.ToDictionary(node => node.Key, node => node.Value);
			var unit = allUnits[name];
			var creator = new ObjectCreator(new[] { typeof(ActorInfoTest).Assembly });
			return new ActorInfo(creator, name, unit, allUnits);
		}
	}
}
