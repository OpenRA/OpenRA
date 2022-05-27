#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using NUnit.Framework;
using OpenRA.Traits;

namespace OpenRA.Test
{
	interface IMock : ITraitInfoInterface { }
	class MockTraitInfo : TraitInfo { public override object Create(ActorInitializer init) { return null; } }
	class MockInheritInfo : MockTraitInfo { }

	class MockAInfo : MockInheritInfo, IMock { }
	class MockBInfo : MockTraitInfo, Requires<IMock>, Requires<MockInheritInfo> { }
	class MockCInfo : MockTraitInfo, Requires<MockBInfo> { }

	class MockDInfo : MockTraitInfo, Requires<MockEInfo> { }
	class MockEInfo : MockTraitInfo, Requires<MockFInfo> { }
	class MockFInfo : MockTraitInfo, Requires<MockDInfo> { }

	class MockGInfo : MockInheritInfo, IMock, NotBefore<MockAInfo> { }
	class MockHInfo : MockTraitInfo, NotBefore<IMock>, NotBefore<MockInheritInfo>, NotBefore<MockBInfo> { }
	class MockIInfo : MockTraitInfo, NotBefore<MockHInfo>, NotBefore<MockCInfo> { }

	class MockJInfo : MockTraitInfo, NotBefore<MockKInfo> { }
	class MockKInfo : MockTraitInfo, NotBefore<MockLInfo> { }
	class MockLInfo : MockTraitInfo, NotBefore<MockJInfo> { }

	[TestFixture]
	public class ActorInfoTest
	{
		[TestCase(TestName = "Trait ordering sorts in dependency order correctly")]
		public void TraitOrderingSortsCorrectly()
		{
			var unorderedTraits = new TraitInfo[] { new MockBInfo(), new MockCInfo(), new MockAInfo(), new MockBInfo() };
			var actorInfo = new ActorInfo("test", unorderedTraits);
			var orderedTraits = actorInfo.TraitsInConstructOrder().ToArray();

			CollectionAssert.AreEquivalent(unorderedTraits, orderedTraits);

			for (var i = 0; i < orderedTraits.Length; i++)
			{
				var traitTypesThatMustOccurBeforeThisTrait =
					ActorInfo.PrerequisitesOf(orderedTraits[i]).Concat(ActorInfo.OptionalPrerequisitesOf(orderedTraits[i]));
				var traitTypesThatOccurAfterThisTrait = orderedTraits.Skip(i + 1).Select(ti => ti.GetType());
				var traitTypesThatShouldOccurEarlier = traitTypesThatOccurAfterThisTrait.Intersect(traitTypesThatMustOccurBeforeThisTrait);
				CollectionAssert.IsEmpty(traitTypesThatShouldOccurEarlier, "Dependency order has not been satisfied.");
			}
		}

		[TestCase(TestName = "Trait ordering sorts in optional dependency order correctly")]
		public void OptionalTraitOrderingSortsCorrectly()
		{
			var unorderedTraits = new TraitInfo[] { new MockHInfo(), new MockIInfo(), new MockGInfo(), new MockHInfo() };
			var actorInfo = new ActorInfo("test", unorderedTraits);
			var orderedTraits = actorInfo.TraitsInConstructOrder().ToArray();

			CollectionAssert.AreEquivalent(unorderedTraits, orderedTraits);

			for (var i = 0; i < orderedTraits.Length; i++)
			{
				var traitTypesThatMustOccurBeforeThisTrait =
					ActorInfo.PrerequisitesOf(orderedTraits[i]).Concat(ActorInfo.OptionalPrerequisitesOf(orderedTraits[i]));
				var traitTypesThatOccurAfterThisTrait = orderedTraits.Skip(i + 1).Select(ti => ti.GetType());
				var traitTypesThatShouldOccurEarlier = traitTypesThatOccurAfterThisTrait.Intersect(traitTypesThatMustOccurBeforeThisTrait);
				CollectionAssert.IsEmpty(traitTypesThatShouldOccurEarlier, "Dependency order has not been satisfied.");
			}
		}

		[TestCase(TestName = "Trait ordering exception reports missing dependencies")]
		public void TraitOrderingReportsMissingDependencies()
		{
			var actorInfo = new ActorInfo("test", new MockBInfo(), new MockCInfo());
			var ex = Assert.Throws<YamlException>(() => actorInfo.TraitsInConstructOrder());

			StringAssert.Contains(typeof(MockBInfo).Name, ex.Message, "Exception message did not report a missing dependency.");
			StringAssert.Contains(typeof(MockCInfo).Name, ex.Message, "Exception message did not report a missing dependency.");
			StringAssert.Contains(typeof(MockInheritInfo).Name, ex.Message, "Exception message did not report a missing dependency (from a base class).");
			StringAssert.Contains(typeof(IMock).Name, ex.Message, "Exception message did not report a missing dependency (from an interface).");
		}

		[TestCase(TestName = "Trait ordering allows optional dependencies to be missing")]
		public void TraitOrderingAllowsMissingOptionalDependencies()
		{
			var unorderedTraits = new TraitInfo[] { new MockHInfo(), new MockIInfo() };
			var actorInfo = new ActorInfo("test", unorderedTraits);
			var orderedTraits = actorInfo.TraitsInConstructOrder().ToArray();

			CollectionAssert.AreEquivalent(unorderedTraits, orderedTraits);
		}

		[TestCase(TestName = "Trait ordering exception reports cyclic dependencies")]
		public void TraitOrderingReportsCyclicDependencies()
		{
			var actorInfo = new ActorInfo("test", new MockDInfo(), new MockEInfo(), new MockFInfo());
			var ex = Assert.Throws<YamlException>(() => actorInfo.TraitsInConstructOrder());

			StringAssert.Contains(typeof(MockDInfo).Name, ex.Message, "Exception message should report all cyclic dependencies.");
			StringAssert.Contains(typeof(MockEInfo).Name, ex.Message, "Exception message should report all cyclic dependencies.");
			StringAssert.Contains(typeof(MockFInfo).Name, ex.Message, "Exception message should report all cyclic dependencies.");
		}

		[TestCase(TestName = "Trait ordering exception reports cyclic optional dependencies")]
		public void TraitOrderingReportsCyclicOptionalDependencies()
		{
			var actorInfo = new ActorInfo("test", new MockJInfo(), new MockKInfo(), new MockLInfo());
			var ex = Assert.Throws<YamlException>(() => actorInfo.TraitsInConstructOrder());

			StringAssert.Contains(typeof(MockJInfo).Name, ex.Message, "Exception message should report all cyclic dependencies.");
			StringAssert.Contains(typeof(MockKInfo).Name, ex.Message, "Exception message should report all cyclic dependencies.");
			StringAssert.Contains(typeof(MockLInfo).Name, ex.Message, "Exception message should report all cyclic dependencies.");
		}
	}
}
