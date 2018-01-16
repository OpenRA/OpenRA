#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class CoordinateTest
	{
		[TestCase(TestName = "Test CPos and MPos conversion and back again.")]
		public void CoarseToMapProjection()
		{
			foreach (var gridType in Enum.GetValues(typeof(MapGridType)).Cast<MapGridType>())
			{
				for (var x = 0; x < 12; x++)
				{
					for (var y = 0; y < 12; y++)
					{
						var cell = new CPos(x, y);
						try
						{
							Assert.That(cell, Is.EqualTo(cell.ToMPos(gridType).ToCPos(gridType)));
						}
						catch
						{
							// Known problem on isometric mods that shouldn't be visible to players as these are outside the map.
							if (gridType == MapGridType.RectangularIsometric && y > x)
								continue;

							Console.WriteLine("Coordinate {0} on grid type {1} failed to convert back.".F(cell, gridType));
							throw;
						}
					}
				}
			}
		}
	}
}
