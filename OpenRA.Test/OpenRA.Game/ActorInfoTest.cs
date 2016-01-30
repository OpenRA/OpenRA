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

	class MockStringInfo : MockTraitInfo { public string AString = null; }

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

		// This needs to match the logic used in RulesetCache.LoadYamlRules
		ActorInfo CreateActorInfoFromYaml(string name, string mapYaml, params string[] yamls)
		{
			var nodes = mapYaml == null ? new List<MiniYamlNode>() : MiniYaml.FromString(mapYaml);
			var sources = yamls.ToList();
			if (mapYaml != null)
				sources.Add(mapYaml);

			var yaml = MiniYaml.Merge(sources.Select(s => MiniYaml.FromString(s)));
			var allUnits = yaml.ToDictionary(node => node.Key, node => node.Value);
			var unit = allUnits[name];
			var creator = new ObjectCreator(new[] { typeof(ActorInfoTest).Assembly });
			return new ActorInfo(creator, name, unit);
		}
	}
}
