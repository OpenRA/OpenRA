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

	[TestFixture]
	public class ActorInfoTest
	{
		[TestCase(TestName = "Trait ordering sorts in dependency order correctly")]
		public void TraitOrderingSortsCorrectly()
		{
			var actorInfo = new ActorInfo("test", new MockCInfo(), new MockBInfo(), new MockAInfo());

			var i = new List<ITraitInfo>(actorInfo.TraitsInConstructOrder());

			Assert.That(i[0], Is.InstanceOf<MockAInfo>());
			Assert.That(i[1].GetType().Name, Is.EqualTo("MockBInfo"));
			Assert.That(i[2].GetType().Name, Is.EqualTo("MockCInfo"));
		}

		[TestCase(TestName = "Trait ordering exception reports missing dependencies")]
		public void TraitOrderingReportsMissingDependencies()
		{
			var actorInfo = new ActorInfo("test", new MockBInfo(), new MockCInfo());
			var ex = Assert.Throws<Exception>(() => actorInfo.TraitsInConstructOrder());

			StringAssert.Contains(typeof(MockAInfo).Name, ex.Message, "Exception message did not report a missing dependency.");
			StringAssert.Contains(typeof(MockBInfo).Name, ex.Message, "Exception message did not report a missing dependency.");
			StringAssert.Contains(typeof(MockCInfo).Name, ex.Message, "Exception message did not report a missing dependency.");
			StringAssert.Contains(typeof(MockInheritInfo).Name, ex.Message, "Exception message did not report a missing dependency (from a base class).");
			StringAssert.Contains(typeof(IMock).Name, ex.Message, "Exception message did not report a missing dependency (from an interface).");
		}

		[TestCase(TestName = "Trait ordering exception reports cyclic dependencies")]
		public void TraitOrderingReportsCyclicDependencies()
		{
			var actorInfo = new ActorInfo("test", new MockDInfo(), new MockEInfo(), new MockFInfo());
			var ex = Assert.Throws<Exception>(() => actorInfo.TraitsInConstructOrder());

			StringAssert.Contains(typeof(MockDInfo).Name, ex.Message, "Exception message should report all cyclic dependencies.");
			StringAssert.Contains(typeof(MockEInfo).Name, ex.Message, "Exception message should report all cyclic dependencies.");
			StringAssert.Contains(typeof(MockFInfo).Name, ex.Message, "Exception message should report all cyclic dependencies.");
		}
	}
}
