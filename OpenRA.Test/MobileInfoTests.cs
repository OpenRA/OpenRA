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
using Moq;
using NUnit.Framework;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	public class MobileInfoTests
	{
		Mock<IWorld> world;
		Mock<IActor> actor;
		Mock<IActor> ignoreActor;
		Mock<IActorMap> actorMap;
		Mock<IMobile> mobile;

		[SetUp]
		public void SetUp()
		{
			actor = new Mock<IActor>();
			mobile = new Mock<IMobile>();
			actor.Setup(m => m.TraitOrDefault<IMobile>()).Returns(mobile.Object);

			ignoreActor = new Mock<IActor>();
			actorMap = new Mock<IActorMap>();

			world = new Mock<IWorld>();
			world.SetupGet(m => m.ActorMap).Returns(actorMap.Object);
		}

		[Test]
		public void NoActorsInCellShouldEnter()
		{
			var mobileInfo = new MobileInfo(true, new string[0]);
			actorMap.Setup(m => m.HasFreeSubCell(It.IsAny<CPos>(), true)).Returns(true);
			actorMap.Setup(m => m.GetActorsAt(It.IsAny<CPos>())).Returns(new List<IActor>());

			var result = mobileInfo.CanMoveFreelyInto(world.Object, actor.Object,
				new CPos(1, 1), actor.Object, CellConditions.All);

			Assert.IsTrue(result);
		}

		/// <summary>
		/// If actor doesn't share cell and there's a free subcell, OK
		/// </summary>
		[Test]
		public void ActorDoesNotShareCell()
		{
			var mobileInfo = new MobileInfo(false, new string[0]);
			actorMap.Setup(m => m.HasFreeSubCell(It.IsAny<CPos>(), true)).Returns(true);
			actorMap.Setup(m => m.GetActorsAt(It.IsAny<CPos>())).Returns(new List<IActor>());

			var result = mobileInfo.CanMoveFreelyInto(world.Object, actor.Object,
				new CPos(1, 1), actor.Object, CellConditions.All);

			Assert.IsTrue(result);
		}

		/// <summary>
		/// If actor doesn't share cell and there's not a free subcell, KO
		/// </summary>
		[Test]
		public void ActorDoesNotShareCellAndCellNotFree()
		{
			var mobileInfo = new MobileInfo(false, new string[0]);
			actorMap.Setup(m => m.HasFreeSubCell(It.IsAny<CPos>(), true)).Returns(true);

			// By logic there must be something in the cell...
			var actor1 = new Mock<IActor>();
			var actor2 = new Mock<IActor>();
			actorMap.Setup(m => m.GetActorsAt(It.IsAny<CPos>())).Returns(new List<IActor>()
			{
				actor1.Object,
				actor2.Object
			});

			var result = mobileInfo.CanMoveFreelyInto(world.Object, actor.Object,
				new CPos(1, 1), actor.Object, CellConditions.All);

			Assert.IsFalse(result);
		}
	}
}
