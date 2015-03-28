#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Moq;
using NUnit.Framework;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	public class MobileTests
	{
		Mock<IWorld> world;
		Mock<IActor> actor;
		Mock<IMobileInfo> mobileInfo;
		Mock<IMap> map;

		[SetUp]
		public void SetUp()
		{
			var worldActor = TestUtils.GenerateWorldActorMock(TestUtils.GeneratePathfinderTraitMock().Object);
			var actorMap = TestUtils.GenerateActorMapMock();
			mobileInfo = TestUtils.GenerateMobileInfoMock();
			map = TestUtils.GenerateMapMock(128, 128, TileShape.Rectangle);
			world = TestUtils.GenerateWorld(worldActor.Object, actorMap.Object, map.Object);
			var mobile = new Mock<IMobile>();
			mobile.Setup(x => x.ToCell).Returns(new CPos(1, 1));
			actor = TestUtils.GenerateActor(mobile.Object, world.Object, mobileInfo.Object);
		}

		[Test]
		public void InstantiateMobile()
		{
			var actorInitializer = new Mock<IActorInitializer>();
			actorInitializer.SetupGet(x => x.Self).Returns(actor.Object);

			new Mobile(actorInitializer.Object, mobileInfo.Object);
		}
	}
}
