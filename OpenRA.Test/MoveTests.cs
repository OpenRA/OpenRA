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
using System.Linq;
using Moq;
using NUnit.Framework;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	public class MoveTests
	{
		Mock<IActor> actor;
		Mock<IActor> worldActor;
		Mock<IMobile> mobile;
		Mock<IMobileInfo> mobileInfo;
		Mock<IWorld> world;
		Mock<IPathFinder> pathFinder;
		Mock<IMap> map;
		Mock<IActorMap> actorMap;
		CPos mobileFrom;
		CPos mobileTo;

		[SetUp]
		public void SetUp()
		{
			mobileInfo = TestUtils.GenerateMobileInfoMock();
			mobile = GenerateMobileTraitMock(mobileInfo.Object);
			pathFinder = TestUtils.GeneratePathfinderTraitMock();
			actorMap = TestUtils.GenerateActorMapMock();
			worldActor = TestUtils.GenerateWorldActorMock(pathFinder.Object);
			map = TestUtils.GenerateMapMock(128, 128, TileShape.Rectangle);
			world = TestUtils.GenerateWorld(worldActor.Object, actorMap.Object, map.Object);
			actor = TestUtils.GenerateActor(mobile.Object, world.Object, mobileInfo.Object);
		}

		private Mock<IMobile> GenerateMobileTraitMock(IMobileInfo mobileInfo)
		{
			var mobile = new Mock<IMobile>();
			mobile.SetupAllProperties();
			mobile.SetupGet(x => x.Info).Returns(mobileInfo);

			// Kind of weird that a property called "ToCell" is used as
			// the source point of movement... shouldn't it be clearer to use
			// the actor Location?
			mobile.Setup(x => x.FromCell).Returns(() => mobileFrom);
			mobile.Setup(x => x.ToCell).Returns(() => mobileTo);
			mobile.Setup(x => x.CurrentLocation).Returns(() => mobileTo);
			mobile.Setup(x => x.SetLocation(It.IsAny<CPos>(), It.IsAny<SubCell>(), It.IsAny<CPos>(), It.IsAny<SubCell>()))
				.Callback((CPos a, SubCell b, CPos c, SubCell d) => SetLocation(a, b, c, d));

			// Tick immediately
			mobile.SetupGet(x => x.TicksBeforePathing).Returns(0);
			mobile.SetupGet(x => x.Facing).Returns(25);
			return mobile;
		}

		[Test]
		public void PathAvailableAndNoCollisionsReturnFirstHalfMove()
		{
			var startingpoint = DefaultPath.Last();
			SetActorCurrentLocation(startingpoint);

			// Stub a path
			pathFinder.Setup(x => x.FindUnitPath(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<IActor>()))
				.Returns(DefaultPath.ToList());

			// There won't be collisions in all path
			mobile.Setup(x => x.CollidesWithOtherActorsInCell(It.IsAny<CPos>(), It.IsAny<IActor>(), It.IsAny<bool>()))
				.Returns(false);

			// No turning in following the path
			map.Setup(x => x.FacingBetween(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<int>()))
				.Returns(() => mobile.Object.Facing);

			// By default it is not disabled by anything
			actor.Setup(x => x.TraitsImplementing<IDisableMove>()).Returns(new List<IDisableMove>());

			// Instantiate a Move activity that wants to reach a specific location,
			// but it's ok if it is within a certain range from it.
			var sut = new Move(actor.Object, DefaultPath.First(), WRange.FromCells(2));

			// Quite weird that we have to pass an actor as a parameter when we already are passing it
			// as a parameter in the constructor. Maybe we should rethink this...
			var resultActivity = sut.Tick(actor.Object);

			Assert.IsTrue(resultActivity is Move.MoveFirstHalf);
			Assert.AreEqual(DefaultPath[DefaultPath.Count - 2], mobile.Object.ToCell);
			Assert.AreEqual(startingpoint, mobile.Object.FromCell);
		}

		[Test]
		public void ReturnItselfIfStillTicksRemaining()
		{
			var disableMove = new Mock<IDisableMove>();
			disableMove.Setup(x => x.MoveDisabled(It.IsAny<Actor>())).Returns(true);

			// By default it is not disabled by anything
			actor.Setup(x => x.TraitsImplementing<IDisableMove>()).Returns(
				new List<IDisableMove>
				{
					disableMove.Object
				});

			// Instantiate a Move activity that wants to reach a specific location,
			// but it's ok if it is within a certain range from it.
			var sut = new Move(actor.Object, new CPos(1, 1), WRange.FromCells(2));

			// Quite weird that we have to pass an actor as a parameter when we already are passing it
			// as a parameter in the constructor. Maybe we should rethink this...
			var resultActivity = sut.Tick(actor.Object);

			Assert.AreEqual(sut, resultActivity);
		}

		[Test]
		public void ReturnItselfIfDisabled()
		{
			var disableMove = new Mock<IDisableMove>();
			disableMove.Setup(x => x.MoveDisabled(It.IsAny<Actor>())).Returns(true);

			// By default it is not disabled by anything
			actor.Setup(x => x.TraitsImplementing<IDisableMove>()).Returns(
				new List<IDisableMove>
				{
					disableMove.Object
				});

			// Instantiate a Move activity that wants to reach a specific location,
			// but it's ok if it is within a certain range from it.
			var sut = new Move(actor.Object, new CPos(1, 1), WRange.FromCells(2));

			// Quite weird that we have to pass an actor as a parameter when we already are passing it
			// as a parameter in the constructor. Maybe we should rethink this...
			var resultActivity = sut.Tick(actor.Object);

			Assert.AreEqual(sut, resultActivity);
		}

		[Test]
		public void ReturnItselfIfNoPathAvailable()
		{
			mobileTo = new CPos(2, 2);

			// Stub a path
			pathFinder.Setup(x => x.FindUnitPath(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<IActor>()))
				.Returns(new List<CPos>());

			// There won't be collisions in all path
			mobile.Setup(x => x.CollidesWithOtherActorsInCell(It.IsAny<CPos>(), It.IsAny<IActor>(), It.IsAny<bool>()))
				.Returns(false);

			// No turning in following the path
			map.Setup(x => x.FacingBetween(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<int>()))
				.Returns(() => mobile.Object.Facing);

			// By default it is not disabled by anything
			actor.Setup(x => x.TraitsImplementing<IDisableMove>()).Returns(new List<IDisableMove>());

			// Instantiate a Move activity that wants to reach a specific location,
			// but it's ok if it is within a certain range from it.
			var sut = new Move(actor.Object, new CPos(1, 1), WRange.FromCells(2));

			// Quite weird that we have to pass an actor as a parameter when we already are passing it
			// as a parameter in the constructor. Maybe we should rethink this...
			var resultActivity = sut.Tick(actor.Object);

			Assert.AreEqual(sut, resultActivity);
		}

		[Test]
		public void IfUnitDisabledReturnItself()
		{
			mobileTo = new CPos(2, 2);

			// Stub a path
			pathFinder.Setup(x => x.FindUnitPath(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<IActor>()))
				.Returns(new List<CPos> { new CPos(1, 1), new CPos(1, 2), new CPos(1, 3), new CPos(2, 2) });

			// By default it is not disabled by anything
			actor.Setup(x => x.TraitsImplementing<IDisableMove>()).Returns(new List<IDisableMove>());

			// Instantiate a Move activity that wants to reach a specific location,
			// but it's ok if it is within a certain range from it.
			var sut = new Move(actor.Object, new CPos(1, 1), WRange.FromCells(2));

			mobile.SetupGet(x => x.TicksBeforePathing).Returns(1);

			// Quite weird that we have to pass an actor as a parameter when we already are passing it
			// as a parameter in the constructor. Maybe we should rethink this...
			var resultActivity = sut.Tick(actor.Object);

			Assert.AreEqual(sut, resultActivity);
			pathFinder.Verify(x => x.FindUnitPath(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<IActor>()), Times.Never);
		}

		[Test]
		public void IfMovementRequiresFaceturnReturnTurnActivity()
		{
			mobileTo = new CPos(2, 2);

			// Stub a path
			pathFinder.Setup(x => x.FindUnitPath(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<IActor>()))
				.Returns(new List<CPos> { new CPos(1, 1), new CPos(1, 2), new CPos(1, 3), new CPos(2, 2) });

			// There won't be collisions in all path
			mobile.Setup(x => x.CollidesWithOtherActorsInCell(It.IsAny<CPos>(), It.IsAny<IActor>(), It.IsAny<bool>()))
				.Returns(false);

			// No turning in following the path
			map.Setup(x => x.FacingBetween(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<int>()))
				.Returns(() => mobile.Object.Facing + 20);

			// By default it is not disabled by anything
			actor.Setup(x => x.TraitsImplementing<IDisableMove>()).Returns(new List<IDisableMove>());

			// Instantiate a Move activity that wants to reach a specific location,
			// but it's ok if it is within a certain range from it.
			var sut = new Move(actor.Object, new CPos(1, 1), WRange.FromCells(2));

			// Quite weird that we have to pass an actor as a parameter when we already are passing it
			// as a parameter in the constructor. Maybe we should rethink this...
			var resultActivity = sut.Tick(actor.Object);

			Assert.IsTrue(resultActivity is Turn);
			Assert.IsTrue(resultActivity.NextActivity is Move);
		}

		[Test]
		public void IfCollisionButInRangeReturnNextActivityOnNextMovementTick()
		{
			mobileTo = new CPos(2, 2);

			// Instantiate a Move activity that wants to reach a specific location,
			// but it's ok if it is within a certain range from it.
			var nextActivity = new Mock<Activity>().Object;

			// Stub a path
			pathFinder.Setup(x => x.FindUnitPath(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<IActor>()))
				.Returns(new List<CPos> { new CPos(1, 1), new CPos(1, 2), new CPos(1, 3), new CPos(2, 2) });

			// There won't be collisions in all path
			mobile.Setup(x => x.CollidesWithOtherActorsInCell(It.IsAny<CPos>(), It.IsAny<IActor>(), It.IsAny<bool>()))
				.Returns(true);

			// No turning in following the path
			map.Setup(x => x.FacingBetween(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<int>()))
				.Returns(() => mobile.Object.Facing);

			// By default it is not disabled by anything
			actor.Setup(x => x.TraitsImplementing<IDisableMove>()).Returns(new List<IDisableMove>());

			// Instantiate a Move activity that wants to reach a specific location,
			// but it's ok if it is within a certain range from it.
			var sut = new Move(actor.Object, new CPos(1, 1), WRange.FromCells(5));
			sut.NextActivity = nextActivity;

			// Quite weird that we have to pass an actor as a parameter when we already are passing it
			// as a parameter in the constructor. Maybe we should rethink this...
			var resultActivity = sut.Tick(actor.Object);
			Assert.IsTrue(resultActivity is Move);

			resultActivity = sut.Tick(actor.Object);
			Assert.IsTrue(resultActivity is Move);

			resultActivity = sut.Tick(actor.Object);
			Assert.AreEqual(nextActivity, resultActivity);
		}

		[Test]
		public void CancelActivity()
		{
			var sut = new Move(actor.Object, new CPos(1, 1), WRange.FromCells(5));
			sut.NextActivity = new Mock<Activity>().Object;
			sut.Cancel(null);
			Assert.IsNull(sut.NextActivity);
		}

		[Test]
		public void PathAvailableAndCollisionsReturnItselfUntilNewPathCalculated()
		{
			mobileTo = new CPos(2, 2);

			// Stub a path
			pathFinder.Setup(x => x.FindUnitPath(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<IActor>()))
				.Returns(new List<CPos> { new CPos(1, 1), new CPos(1, 2), new CPos(1, 3), new CPos(2, 2) });

			// There will be collisions in all path
			mobile.Setup(x => x.CollidesWithOtherActorsInCell(It.IsAny<CPos>(), It.IsAny<IActor>(), It.IsAny<bool>()))
				.Returns(true);

			// We'll have to wait 2 turns before repathing
			mobileInfo.Setup(x => x.WaitAverage).Returns(2);
			mobileInfo.Setup(x => x.WaitSpread).Returns(0);

			// No turning in following the path
			map.Setup(x => x.FacingBetween(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<int>()))
				.Returns(() => mobile.Object.Facing);

			// By default it is not disabled by anything
			actor.Setup(x => x.TraitsImplementing<IDisableMove>()).Returns(new List<IDisableMove>());

			// Instantiate a Move activity that wants to reach a specific location,
			// but it's ok if it is within a certain range from it.
			var sut = new Move(actor.Object, new CPos(1, 1), WRange.FromCells(0));

			var resultActivity = sut.Tick(actor.Object);
			Assert.IsTrue(resultActivity is Move);
			pathFinder.Verify(x => x.FindUnitPath(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<IActor>()), Times.Once);

			resultActivity = sut.Tick(actor.Object);
			pathFinder.Verify(x => x.FindUnitPath(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<IActor>()), Times.Once);
			Assert.IsTrue(resultActivity is Move);

			resultActivity = sut.Tick(actor.Object);
			pathFinder.Verify(x => x.FindUnitPath(It.IsAny<CPos>(), It.IsAny<CPos>(), It.IsAny<IActor>()), Times.Exactly(2));
			Assert.IsTrue(resultActivity is Move);
		}

		[Test]
		public void IfAlreadyInDestinationThenProceedNextActivity()
		{
			mobileTo = new CPos(2, 2);

			// Instantiate a Move activity that wants to reach a specific location,
			// but it's ok if it is within a certain range from it.
			var sut = new Move(actor.Object, new CPos(2, 2), WRange.FromCells(0));
			var nextActivity = new Mock<Activity>().Object;
			sut.NextActivity = nextActivity;

			var resultActivity = sut.Tick(actor.Object);

			Assert.AreEqual(nextActivity, resultActivity);
		}

		[Test]
		public void IsMovingTest()
		{
			actor.Setup(a => a.GetCurrentActivity()).Returns(new Move(actor.Object, new CPos(0, 0)));

			Assert.IsTrue(actor.Object.IsMoving());
		}

		#region Support Methods

		static readonly List<CPos> DefaultPath = new List<CPos>
			{
				new CPos(1, 1), new CPos(1, 2), new CPos(1, 3), new CPos(2, 2),
				new CPos(2, 3), new CPos(3, 4), new CPos(4, 5), new CPos(5, 6)
			};

		void SetActorCurrentLocation(CPos pos)
		{
			mobileTo = pos;
			mobileFrom = pos;
		}

		void SetLocation(CPos from, SubCell fromSub, CPos to, SubCell toSub)
		{
			var mob = mobile.Object;

			if (mob.FromCell == from && mob.ToCell == to && mob.FromSubCell == fromSub && mob.ToSubCell == toSub)
				return;

			mobileFrom = from;
			mobileTo = to;
			mob.FromSubCell = fromSub;
			mob.ToSubCell = toSub;
		}

		#endregion
	}
}
