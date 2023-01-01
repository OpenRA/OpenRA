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

using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	public class CPosTest
	{
		[TestCase(TestName = "Packing x,y and layer into int")]
		public void PackUnpackBits()
		{
			var values = new int[] { -2048, -1024, 0, 1024, 2047 };
			var layerValues = new byte[] { 0, 128, 255 };

			foreach (var x in values)
			{
				foreach (var y in values)
				{
					foreach (var layer in layerValues)
					{
						var cell = new CPos(x, y, layer);

						Assert.AreEqual(x, cell.X);
						Assert.AreEqual(y, cell.Y);
						Assert.AreEqual(layer, cell.Layer);
					}
				}
			}
		}
	}
}
