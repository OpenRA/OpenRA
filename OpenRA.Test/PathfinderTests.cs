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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Moq;
using NUnit.Framework;
using OpenRA;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Test;

namespace PathfinderTests
{
	[TestFixture]
	public class PathfinderTests
	{
		const int Width = 128;
		const int Height = 128;
		IWorld world;
		IMap map;
		IActor actor;

		[SetUp]
		public void Setup()
		{
			map = new FakeMap(Width, Height);
			var worldactor = new FakeActor();
			world = new FakeWorld(map, worldactor);
			actor = new FakeActor(world);
		}

		IMap BuildFakeMap(int mapWidth, int mapHeight)
		{
			var map = new Mock<IMap>();
			map.SetupGet(m => m.TileShape).Returns(TileShape.Rectangle);
			map.Setup(m => m.MapSize).Returns(new int2(mapWidth, mapHeight));
			map.Setup(m => m.Contains(It.Is<CPos>(pos => pos.X >= 0 && pos.X < mapWidth && pos.Y >= 0 && pos.Y < mapHeight))).Returns(true);

			return map.Object;
		}

		IWorld BuildFakeWorld(IMap map)
		{
			var world = new Mock<IWorld>();
			world.SetupGet(m => m.Map).Returns(map);
			world.SetupGet(m => m.WorldActor).Returns(new Mock<IActor>().Object);
			return world.Object;
		}

		static bool IsValidPos(CPos pos, int mapWidth, int mapHeight)
		{
			return pos.X >= 0 && pos.X < mapWidth && pos.Y >= 0 && pos.Y < mapHeight;
		}

		[Test]
		[Ignore]
		public void FindPathOnRoughTerrainTest()
		{
			// Arrange

			// Create the MobileInfo Mock. Playing with this can help to
			// check the different paths and points a unit can walk into
			var mi = new FakeMobileInfo(pos => !(!IsValidPos(pos, Width, Height) ||
					(pos.X == 50 && pos.Y < 100) ||
					(pos.X == 100 && pos.Y > 50)));

			var from = new CPos(1, 1);
			var target = new CPos(125, 75);

			IPathSearch search;
			Stopwatch stopwatch;
			List<CPos> path1 = null;
			List<CPos> path2 = null;
			List<CPos> path3 = null;
			List<CPos> path4 = null;
			List<CPos> path5 = null;
			List<CPos> path6 = null;
			List<CPos> path7 = null;
			List<CPos> path8 = null;
			var pathfinder = new PathFinder(world);

			// Act
			stopwatch = new Stopwatch();
			foreach (var a in Enumerable.Range(1, 50))
			{
				search = PathSearch.FromPoint(world, mi, actor, from, target, true);
				stopwatch.Start();
				path5 = pathfinder.FindPath(search);

				stopwatch.Stop();
				search = PathSearch.FromPoint(world, mi, actor, new CPos(0, 0), new CPos(51, 100), true);
				stopwatch.Start();
				path6 = pathfinder.FindPath(search);

				stopwatch.Stop();
				search = PathSearch.FromPoint(world, mi, actor, new CPos(0, 0), new CPos(49, 50), true);
				stopwatch.Start();
				path7 = pathfinder.FindPath(search);

				stopwatch.Stop();
				search = PathSearch.FromPoint(world, mi, actor, new CPos(127, 0), new CPos(50, 101), true);
				stopwatch.Start();
				path8 = pathfinder.FindPath(search);
			}

			Console.WriteLine("I took " + stopwatch.ElapsedMilliseconds + " ms with new pathfinder");

			IPathSearch search2;
			stopwatch = new Stopwatch();
			foreach (var a in Enumerable.Range(1, 50))
			{
				search = PathSearch.FromPoint(world, mi, actor, from, target, true);
				search2 = PathSearch.FromPoint(world, mi, actor, target, from, true).Reverse();
				stopwatch.Start();
				path5 = pathfinder.FindBidiPath(search, search2);

				stopwatch.Stop();
				search = PathSearch.FromPoint(world, mi, actor, new CPos(0, 0), new CPos(51, 100), true);
				search2 = PathSearch.FromPoint(world, mi, actor, new CPos(51, 100), new CPos(0, 0), true).Reverse();
				stopwatch.Start();
				path6 = pathfinder.FindBidiPath(search, search2);

				stopwatch.Stop();
				search = PathSearch.FromPoint(world, mi, actor, new CPos(0, 0), new CPos(49, 50), true);
				search2 = PathSearch.FromPoint(world, mi, actor, new CPos(49, 50), new CPos(0, 0), true).Reverse();
				stopwatch.Start();
				path7 = pathfinder.FindBidiPath(search, search2);

				stopwatch.Stop();
				search = PathSearch.FromPoint(world, mi, actor, new CPos(127, 0), new CPos(50, 101), true);
				search2 = PathSearch.FromPoint(world, mi, actor, new CPos(50, 101), new CPos(127, 0), true).Reverse();
				stopwatch.Start();
				path8 = pathfinder.FindBidiPath(search, search2);
			}

			Console.WriteLine("I took " + stopwatch.ElapsedMilliseconds + " ms with new FindBidipathfinder");
		}

		/// <summary>
		/// We can't rely on floating point math to be deterministic across all runtimes.
		/// The cases that use this will need to be changed to use integer math
		/// </summary>
		public const double Sqrt2 = 1.414;

		static int Est1(CPos here, CPos destination)
		{
			var diag = Math.Min(Math.Abs(here.X - destination.X), Math.Abs(here.Y - destination.Y));
			var straight = Math.Abs(here.X - destination.X) + Math.Abs(here.Y - destination.Y);

			// Min cost to arrive from once cell to an adjacent one
			// (125 according to tests)
			const int D = 100;

			// According to the information link, this is the shape of the function.
			// We just extract factors to simplify.
			var h = D * straight + (D * Sqrt2 - 2 * D) * diag;

			return (int)(h * 1.001);
		}

		static int Est2(CPos here, CPos destination)
		{
			var diag = Math.Min(Math.Abs(here.X - destination.X), Math.Abs(here.Y - destination.Y));
			var straight = Math.Abs(here.X - destination.X) + Math.Abs(here.Y - destination.Y);

			// HACK: this relies on fp and cell-size assumptions.
			var h = (100 * diag * Sqrt2) + 100 * (straight - (2 * diag));
			return (int)(h * 1.001);
		}

		/// <summary>
		/// Tests the refactor of the default heuristic for pathFinder
		/// </summary>
		[Test]
		public void EstimatorsTest()
		{
			Assert.AreEqual(Est1(new CPos(0, 0), new CPos(20, 30)), Est2(new CPos(0, 0), new CPos(20, 30)));
		}

		[Test]
		public void Remove1000StoredPaths()
		{
			var world = new Mock<IWorld>();
			world.SetupGet(m => m.WorldTick).Returns(50);
			var pathCacheStorage = new PathCacheStorage(world.Object);
			var stopwatch = new Stopwatch();
			for (var i = 0; i < 1100; i++)
			{
				if (i == 100)
				{
					// Let's make the world tick further so we can trigger the removals
					// when storing more stuff
					world.SetupGet(m => m.WorldTick).Returns(110);
					stopwatch.Start();
				}

				pathCacheStorage.Store(i.ToString(), new List<CPos>());
				if (i == 100)
				{
					stopwatch.Stop();
					Console.WriteLine("I took " + stopwatch.ElapsedMilliseconds + " ms to remove 1000 stored paths");
				}
			}
		}

		/// <summary>
		/// Test for the future feature of path smoothing for Pathfinder
		/// </summary>
		[Test]
		public void RayCastingTest()
		{
			// Arrange
			var sut = new RayCaster();
			CPos source = new CPos(1, 3);
			CPos target = new CPos(3, 0);

			// Act
			var valid = sut.RayCast(source, target);

			// Assert
		}
	}

	public class RayCaster
	{
		// Algorithm obtained in http://playtechs.blogspot.co.uk/2007/03/raytracing-on-grid.html
		public IEnumerable<CPos> RayCast(CPos source, CPos target)
		{
			int dx = Math.Abs(target.X - source.X);
			int dy = Math.Abs(target.Y - source.Y);
			int x = source.X;
			int y = source.Y;

			int x_inc = (target.X > source.X) ? 1 : -1;
			int y_inc = (target.Y > source.Y) ? 1 : -1;
			int error = dx - dy;
			dx *= 2;
			dy *= 2;

			for (int n = 1 + dx + dy; n > 0; --n)
			{
				yield return new CPos(x, y);

				if (error > 0)
				{
					x += x_inc;
					error -= dy;
				}
				else
				{
					y += y_inc;
					error += dx;
				}
			}
		}

		public bool RayClear()
		{
			return true;
		}
	}
}
