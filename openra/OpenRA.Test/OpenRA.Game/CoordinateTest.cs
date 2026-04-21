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
	}
}
