using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Moq;
using NUnit.Framework;
using OpenRA;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace PathfinderTests
{
	[TestFixture]
	public class PathfinderTests
	{
		private IMap BuildFakeMap(int mapWidth, int mapHeight)
		{
			var map = new Mock<IMap>();
			map.SetupGet(m => m.TileShape).Returns(TileShape.Rectangle);
			map.Setup(m => m.MapDimensions).Returns(new int2(mapWidth, mapHeight));
			map.Setup(m => m.Contains(It.Is<CPos>(pos => pos.X >= 0 && pos.X < mapWidth && pos.Y >= 0 && pos.Y < mapHeight))).Returns(true);

			return map.Object;
		}

		private IWorld BuildFakeWorld(IMap map)
		{
			var world = new Mock<IWorld>();
			world.SetupGet(m => m.IMap).Returns(map);
			world.SetupGet(m => m.IWorldActor).Returns(new Mock<IActor>().Object);
			return world.Object;
		}

		private static bool IsValidPos(CPos pos, int mapWidth, int mapHeight)
		{
			return pos.X >= 0 && pos.X < mapWidth && pos.Y >= 0 && pos.Y < mapHeight;
		}

		[Test]
		public void FindPathOnRoughTerrainTest()
		{
			// Arrange
			var mapWidth = 128;
			var mapHeight = 128;

			var map = BuildFakeMap(mapWidth, mapHeight);
			var world = BuildFakeWorld(map);
			var self = new Mock<IActor>();
			self.SetupGet(m => m.IWorld).Returns(world);

			// Create the MobileInfo Mock. Playing with this can help to
			// check the different paths and points a unit can walk into
			var mi = new Mock<IMobileInfo>();
			mi.Setup(m => m.CanEnterCell(It.IsAny<World>(), It.IsAny<Actor>(), It.Is<CPos>(pos => IsValidPos(pos, mapWidth, mapHeight)),
				It.IsAny<Actor>(), It.IsAny<CellConditions>()))
				.Returns(true);
			mi.Setup(m => m.CanEnterCell(It.IsAny<World>(), It.IsAny<Actor>(),
				It.Is<CPos>(pos => !IsValidPos(pos, mapWidth, mapHeight) ||
					(pos.X == 50 && pos.Y < 100) ||
					(pos.X == 100 && pos.Y > 50)
					), It.IsAny<Actor>(), It.IsAny<CellConditions>())).Returns(false);

			mi.Setup(m => m.MovementCostForCell(It.IsAny<World>(), It.Is<CPos>(pos => IsValidPos(pos, mapWidth, mapHeight))))
				.Returns(1);
			mi.Setup(m => m.MovementCostForCell(It.IsAny<World>(),
				It.Is<CPos>(pos => !IsValidPos(pos, mapWidth, mapHeight) ||
					(pos.X == 50 && pos.Y < 100) ||
					(pos.X == 100 && pos.Y > 50)
					)))
				.Returns(int.MaxValue);

			var log = new Mock<ILog>();

			var from = new CPos(1, 1);
			var target = new CPos(125, 75);

			var search = new PathSearch(mi.Object, self.Object, log.Object)
			{
				Heuristic = PathSearch.DefaultEstimator(target),
				CheckForBlocked = true,
			};

			search.AddInitialCell(from);

			var pathfinder = new PathFinder(world);

			// Act
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			var path = pathfinder.FindPath(search);
			Console.WriteLine("I took " + stopwatch.ElapsedMilliseconds + "ms");

			// Assert
			// Given the specified restrictions in mocks, at X = 50, Y should
			// be 100 if the path is correctly calculated.
			Assert.AreEqual(100, path.First(p => p.X == 50).Y);
		}

		[Test]
		public void FindBidiPathTest()
		{
			// Arrange
			var mapWidth = 128;
			var mapHeight = 128;

			var map = BuildFakeMap(mapWidth, mapHeight);
			var world = BuildFakeWorld(map);
			var self = new Mock<IActor>();
			self.SetupGet(m => m.IWorld).Returns(world);

			// Create the MobileInfo Mock. Playing with this can help to
			// check the different paths and points a unit can walk into
			var mi = new Mock<IMobileInfo>();
			mi.Setup(m => m.CanEnterCell(It.IsAny<World>(), It.IsAny<Actor>(), It.Is<CPos>(pos => IsValidPos(pos, mapWidth, mapHeight)),
				It.IsAny<Actor>(), It.IsAny<CellConditions>()))
				.Returns(true);
			mi.Setup(m => m.CanEnterCell(It.IsAny<World>(), It.IsAny<Actor>(),
				It.Is<CPos>(pos => !IsValidPos(pos, mapWidth, mapHeight) ||
					(pos.X == 50 && pos.Y < 100) ||
					(pos.X == 100 && pos.Y > 50)
					), It.IsAny<Actor>(), It.IsAny<CellConditions>())).Returns(false);

			mi.Setup(m => m.MovementCostForCell(It.IsAny<World>(), It.Is<CPos>(pos => IsValidPos(pos, mapWidth, mapHeight))))
				.Returns(1);
			mi.Setup(m => m.MovementCostForCell(It.IsAny<World>(),
				It.Is<CPos>(pos => !IsValidPos(pos, mapWidth, mapHeight) ||
					(pos.X == 50 && pos.Y < 100) ||
					(pos.X == 100 && pos.Y > 50)
					)))
				.Returns(int.MaxValue);

			var log = new Mock<ILog>();

			var from = new CPos(1, 1);
			var target = new CPos(125, 75);

			var search = new PathSearch(mi.Object, self.Object, log.Object)
			{
				Heuristic = PathSearch.DefaultEstimator(target),
				CheckForBlocked = true,
			};

			search.AddInitialCell(from);

			var search2 = new PathSearch(mi.Object, self.Object, log.Object)
			{
				Heuristic = PathSearch.DefaultEstimator(from),
				CheckForBlocked = true,
			};

			search2.AddInitialCell(target);

			var pathfinder = new PathFinder(world);

			// Act
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			var path = pathfinder.FindBidiPath(search, search2);
			Console.WriteLine("I took " + stopwatch.ElapsedMilliseconds + "ms");

			// Assert
			// Given the specified restrictions in mocks, at X = 50, Y should
			// be 100 if the path is correctly calculated.
			Assert.AreEqual(100, path.First(p => p.X == 50).Y);
		}
	}
}
