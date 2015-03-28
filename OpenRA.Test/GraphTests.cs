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
	using System.Linq;

	using OpenRA.Mods.Common.Pathfinder;

	[TestFixture]
	public class GraphTests
	{
		Mock<IWorld> world;
		Mock<IActor> actor;
		Mock<IActorMap> actorMap;
		Mock<IMobile> mobile;

		private Mock<IMap> map;

		private Mock<IActor> worldActor;

		private Mock<IMobileInfo> mobileInfo;

		CellLayer<CellInfo> cellLayer;

		[SetUp]
		public void SetUp()
		{
			var pathfinder = TestUtils.GeneratePathfinderTraitMock();
			worldActor = TestUtils.GenerateWorldActorMock(pathfinder.Object);
			actorMap = TestUtils.GenerateActorMapMock();

			map = TestUtils.GenerateMapMock(128, 128, TileShape.Rectangle);
			world = TestUtils.GenerateWorld(worldActor.Object, actorMap.Object, map.Object);

			cellLayer = CellInfoLayerManager.Instance.NewLayer(map.Object);
			mobileInfo = TestUtils.GenerateMobileInfoMock();
			mobile = TestUtils.GenerateMobileMock();
			actor = TestUtils.GenerateActor(mobile.Object, world.Object, mobileInfo.Object);
		}

		static bool IsValidPos(CPos pos, int mapWidth, int mapHeight)
		{
			return pos.X >= 0 && pos.X < mapWidth && pos.Y >= 0 && pos.Y < mapHeight;
		}

		static readonly CVec[][] DirectedNeighbors = {
			new[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1), new CVec(-1, 0), new CVec(-1, 1) },
			new[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1) },
			new[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1), new CVec(1, 0), new CVec(1, 1) },
			new[] { new CVec(-1, -1), new CVec(-1, 0), new CVec(-1, 1) },
			CVec.Directions,
			new[] { new CVec(1, -1), new CVec(1, 0), new CVec(1, 1) },
			new[] { new CVec(-1, -1), new CVec(-1, 0), new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) },
			new[] { new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) },
			new[] { new CVec(1, -1), new CVec(1, 0), new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) },
		};

		[Test]
		public void GetConnectionsTest()
		{
			var graph = new PathGraph(cellLayer, mobileInfo.Object, actor.Object, world.Object, true);

			int dummy = 125;
			mobileInfo.Setup(
				x =>
				x.CanEnterCell(
					world.Object,
					actor.Object,
					It.Is<CPos>(pos => !(!IsValidPos(pos, 128, 128) ||
					(pos.X == 50))),
					out dummy,
					It.IsAny<IActor>(),
					It.IsAny<CellConditions>())).Returns(true);

			var cell = new CPos(10, 10);
			var connections = graph.GetConnections(cell);

			Assert.AreEqual(8, connections.Count());

			foreach (var vector in DirectedNeighbors[4])
			{
				CollectionAssert.Contains(connections.Select(x => x.Destination), cell + vector);
			}
		}
	}
}
