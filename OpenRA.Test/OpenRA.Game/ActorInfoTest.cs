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
	public class ActorInfoTest
	{
		ActorInfo actorInfo;
		Dictionary<string, MiniYaml> allUnits;

		interface IMock : ITraitInfo { }
		class MockTrait : ITraitInfo { public object Create(ActorInitializer init) { return null; } }
		class MockInherit : MockTrait { }
		class MockA : MockInherit, IMock { }
		class MockB : MockTrait, Requires<MockA>, Requires<IMock>, Requires<MockInherit> { }
		class MockC : MockTrait, Requires<MockB> { }
		class MockD : MockTrait, Requires<MockE> { }
		class MockE : MockTrait, Requires<MockF> { }
		class MockF : MockTrait, Requires<MockD> { }

		[SetUp]
		public void SetUp()
		{
			allUnits = new Dictionary<string, MiniYaml>();
			actorInfo = new ActorInfo("", new MiniYaml(""), allUnits);
		}

		[TestCase(TestName = "Sort traits in order of dependency")]
		public void TraitsInConstructOrderA()
		{
			actorInfo.Traits.Add(new MockC());
			actorInfo.Traits.Add(new MockB());
			actorInfo.Traits.Add(new MockA());

			var i = new List<ITraitInfo> (actorInfo.TraitsInConstructOrder());

			Assert.That(i[0], Is.InstanceOf<MockA>());
			Assert.That(i[1], Is.InstanceOf<MockB>());
			Assert.That(i[2], Is.InstanceOf<MockC>());
		}

		[TestCase(TestName = "Exception reports missing dependencies")]
		public void TraitsInConstructOrderB()
		{
			actorInfo.Traits.Add(new MockB());
			actorInfo.Traits.Add(new MockC());

			try
			{
				var i = actorInfo.TraitsInConstructOrder();
				throw new Exception("Exception not thrown!");
			}
			catch (Exception e)
			{
				Assert.That(e.Message, Is.StringContaining("MockA"));
				Assert.That(e.Message, Is.StringContaining("MockB"));
				Assert.That(e.Message, Is.StringContaining("MockC"));
				Assert.That(e.Message, Is.StringContaining("MockInherit"), "Should recognize base classes");
				Assert.That(e.Message, Is.StringContaining("IMock"), "Should recognize interfaces");
			}
		}

		[TestCase(TestName = "Exception reports cyclic dependencies")]
		public void TraitsInConstructOrderC()
		{
			actorInfo.Traits.Add(new MockD());
			actorInfo.Traits.Add(new MockE());
			actorInfo.Traits.Add(new MockF());

			try
			{
				var i = actorInfo.TraitsInConstructOrder();
				throw new Exception("Exception not thrown!");
			}
			catch (Exception e)
			{
				var count = (
					new Regex("MockD").Matches(e.Message).Count +
					new Regex("MockE").Matches(e.Message).Count +
					new Regex("MockF").Matches(e.Message).Count
					) / 3.0;

				Assert.That(count, Is.EqualTo(Math.Floor(count)), "Should be symmetrical");
			}
		}
	}
}
