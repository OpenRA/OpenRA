#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Common.MapGenerator;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class CoordinateTest
	{
		[TestCase(TestName = "Test CPos to MPos conversion and back again.")]
		public void CPosConversionRoundtrip()
		{
			foreach (var gridType in Enum.GetValues<MapGridType>())
			{
				var expected = new CellCoordsRegion(new CPos(-12, -12), new CPos(12, 12));
				var actual = expected.Select(pos => pos.ToMPos(gridType).ToCPos(gridType)).ToArray();

				Assert.That(expected, Is.EqualTo(actual));
			}
		}

		[TestCase(TestName = "Test MPos to CPos conversion and back again.")]
		public void MPosConversionRoundtrip()
		{
			foreach (var gridType in Enum.GetValues<MapGridType>())
			{
				var expected = new MapCoordsRegion(new MPos(-12, -12), new MPos(12, 12));
				var actual = expected.Select(pos => pos.ToCPos(gridType).ToMPos(gridType)).ToArray();

				Assert.That(expected, Is.EqualTo(actual));
			}
		}

		[TestCase(TestName = "Test directional movement of ToCPos.")]
		public void TestIsometricCPosConversion()
		{
			const MapGridType Isometric = MapGridType.RectangularIsometric;
			Assert.That(new CPos(0, 0), Is.EqualTo(new MPos(0, 0).ToCPos(Isometric)));

			Assert.That(new CPos(1, 1), Is.EqualTo(new MPos(0, 2).ToCPos(Isometric)));
			Assert.That(new CPos(2, 2), Is.EqualTo(new MPos(0, 4).ToCPos(Isometric)));
			Assert.That(new CPos(3, 3), Is.EqualTo(new MPos(0, 6).ToCPos(Isometric)));

			Assert.That(new CPos(1, 0), Is.EqualTo(new MPos(0, 1).ToCPos(Isometric)));
			Assert.That(new CPos(2, 0), Is.EqualTo(new MPos(1, 2).ToCPos(Isometric)));
			Assert.That(new CPos(3, 0), Is.EqualTo(new MPos(1, 3).ToCPos(Isometric)));

			Assert.That(new CPos(0, 1), Is.EqualTo(new MPos(-1, 1).ToCPos(Isometric)));
			Assert.That(new CPos(0, 2), Is.EqualTo(new MPos(-1, 2).ToCPos(Isometric)));
			Assert.That(new CPos(0, 3), Is.EqualTo(new MPos(-2, 3).ToCPos(Isometric)));

			Assert.That(new CPos(1, -1), Is.EqualTo(new MPos(1, 0).ToCPos(Isometric)));
			Assert.That(new CPos(2, -2), Is.EqualTo(new MPos(2, 0).ToCPos(Isometric)));
			Assert.That(new CPos(3, -3), Is.EqualTo(new MPos(3, 0).ToCPos(Isometric)));
		}

		[TestCase(TestName = "Test directional movement of ToMPos.")]
		public void TestIsometricMPosConversion()
		{
			const MapGridType Isometric = MapGridType.RectangularIsometric;
			Assert.That(new MPos(0, 0), Is.EqualTo(new CPos(0, 0).ToMPos(Isometric)));

			Assert.That(new MPos(0, 2), Is.EqualTo(new CPos(1, 1).ToMPos(Isometric)));
			Assert.That(new MPos(0, 4), Is.EqualTo(new CPos(2, 2).ToMPos(Isometric)));
			Assert.That(new MPos(0, 6), Is.EqualTo(new CPos(3, 3).ToMPos(Isometric)));

			Assert.That(new MPos(0, 1), Is.EqualTo(new CPos(1, 0).ToMPos(Isometric)));
			Assert.That(new MPos(1, 2), Is.EqualTo(new CPos(2, 0).ToMPos(Isometric)));
			Assert.That(new MPos(1, 3), Is.EqualTo(new CPos(3, 0).ToMPos(Isometric)));

			Assert.That(new MPos(-1, 1), Is.EqualTo(new CPos(0, 1).ToMPos(Isometric)));
			Assert.That(new MPos(-1, 2), Is.EqualTo(new CPos(0, 2).ToMPos(Isometric)));
			Assert.That(new MPos(-2, 3), Is.EqualTo(new CPos(0, 3).ToMPos(Isometric)));

			Assert.That(new MPos(1, 0), Is.EqualTo(new CPos(1, -1).ToMPos(Isometric)));
			Assert.That(new MPos(2, 0), Is.EqualTo(new CPos(2, -2).ToMPos(Isometric)));
			Assert.That(new MPos(3, 0), Is.EqualTo(new CPos(3, -3).ToMPos(Isometric)));
		}

		[TestCase(TestName = "Test BoundingRegion.")]
		public void TestBoundingRegion()
		{
			foreach (var gridType in Enum.GetValues<MapGridType>())
			{
				var cellRegion = new CellCoordsRegion(new CPos(0, 0), new CPos(5, 5));
				var betterCellRegion = CellRegion.BoundingRegion(gridType, cellRegion.ToArray());
				Assert.That(betterCellRegion, Is.SupersetOf(cellRegion));

				var cellRegion2 = new CellCoordsRegion(new CPos(0, 0), new CPos(0, 5));
				var betterCellRegion2 = CellRegion.BoundingRegion(gridType, cellRegion2.ToArray());
				Assert.That(betterCellRegion2, Is.SupersetOf(cellRegion2));

				var cellRegion3 = new CellCoordsRegion(new CPos(0, 0), new CPos(5, 0));
				var betterCellRegion3 = CellRegion.BoundingRegion(gridType, cellRegion3.ToArray());
				Assert.That(betterCellRegion3, Is.SupersetOf(cellRegion3));

				var cellRegion4 = new CellCoordsRegion(new CPos(-3, -3), new CPos(2, 2));
				var betterCellRegion4 = CellRegion.BoundingRegion(gridType, cellRegion4.ToArray());
				Assert.That(betterCellRegion4, Is.SupersetOf(cellRegion4));
			}
		}

		[TestCase(TestName = "Test BoundingCellLayer with offset.")]
		public void TestBoundingCellLayerWithOffset()
		{
			foreach (var gridType in Enum.GetValues<MapGridType>())
			{
				var cellRegion = new CellCoordsRegion(new CPos(0, 0), new CPos(5, 5));
				var betterCellRegion = CellLayerUtils.BoundingCellLayer<bool>(gridType, cellRegion, out var offset);

				var regionWithOffset = new CellCoordsRegion(
					cellRegion.TopLeft + offset,
					cellRegion.BottomRight + offset);

				Assert.That(betterCellRegion.CellRegion.MapCoords, Is.SupersetOf(regionWithOffset.Select(pos => pos.ToMPos(gridType))));

				var cellRegion2 = new CellCoordsRegion(new CPos(0, 0), new CPos(0, 5));
				var betterCellRegion2 = CellLayerUtils.BoundingCellLayer<bool>(gridType, cellRegion2, out var offset2);
				var regionWithOffset2 = new CellCoordsRegion(
					cellRegion2.TopLeft + offset2,
					cellRegion2.BottomRight + offset2);

				Assert.That(betterCellRegion2.CellRegion.MapCoords, Is.SupersetOf(regionWithOffset2.Select(pos => pos.ToMPos(gridType))));

				var cellRegion3 = new CellCoordsRegion(new CPos(0, 0), new CPos(5, 0));
				var betterCellRegion3 = CellLayerUtils.BoundingCellLayer<bool>(gridType, cellRegion3, out var offset3);
				var regionWithOffset3 = new CellCoordsRegion(
					cellRegion3.TopLeft + offset3,
					cellRegion3.BottomRight + offset3);

				Assert.That(betterCellRegion3.CellRegion.MapCoords, Is.SupersetOf(regionWithOffset3.Select(pos => pos.ToMPos(gridType))));

				var cellRegion4 = new CellCoordsRegion(new CPos(-3, -3), new CPos(2, 2));
				var betterCellRegion4 = CellLayerUtils.BoundingCellLayer<bool>(gridType, cellRegion4, out var offset4);
				var regionWithOffset4 = new CellCoordsRegion(
					cellRegion4.TopLeft + offset4,
					cellRegion4.BottomRight + offset4);

				Assert.That(betterCellRegion4.CellRegion.MapCoords, Is.SupersetOf(regionWithOffset4.Select(pos => pos.ToMPos(gridType))));

				var cellRegion5 = new CellCoordsRegion(new CPos(-1, -1), new CPos(0, 0));
				var betterCellRegion5 = CellLayerUtils.BoundingCellLayer<bool>(gridType, cellRegion5, out var offset5);
				var regionWithOffset5 = new CellCoordsRegion(
					cellRegion5.TopLeft + offset5,
					cellRegion5.BottomRight + offset5);

				Assert.That(betterCellRegion5.CellRegion.MapCoords, Is.SupersetOf(regionWithOffset5.Select(pos => pos.ToMPos(gridType))));
			}
		}
	}
}
