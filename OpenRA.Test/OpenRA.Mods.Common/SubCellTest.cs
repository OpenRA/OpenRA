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

using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class SubCellTest
	{
		[TestCase(TestName = "Test actor subcell conversion.")]
		public void Conversion()
		{
			var actorSubcellAny = MultiBrush.ToActorSubCell(SubCell.Any);
			Assert.That(actorSubcellAny, Is.EqualTo(MultiBrush.ActorSubCell.Any));

			var actorSubcell1 = MultiBrush.ToActorSubCell(SubCell.First);
			Assert.That(actorSubcell1, Is.EqualTo((MultiBrush.ActorSubCell)0x01));

			var actorSubcell2 = MultiBrush.ToActorSubCell((SubCell)2);
			Assert.That(actorSubcell2, Is.EqualTo((MultiBrush.ActorSubCell)0x02));

			var actorSubcell3 = MultiBrush.ToActorSubCell((SubCell)3);
			Assert.That(actorSubcell3, Is.EqualTo((MultiBrush.ActorSubCell)0x04));

			var actorSubcell4 = MultiBrush.ToActorSubCell((SubCell)4);
			Assert.That(actorSubcell4, Is.EqualTo((MultiBrush.ActorSubCell)0x08));
		}

		const string MapGridYaml =
@"MapGrid:
	EnableDepthBuffer: True
	Type: RectangularIsometric
	MaximumTerrainHeight: 16
	SubCellOffsets: 0,0,0, -362,0,0, 0,362,0, 362,0,0
	DefaultSubCell: 2
";

		[TestCase(TestName = "Test bit filling.")]
		public void BitFilling()
		{
			var grid = new MapGrid(MiniYaml.FromString(MapGridYaml, "MapGrid").First().Value);
			var full = MultiBrush.FullSubCell(grid);

			Assert.That(full, Is.EqualTo((MultiBrush.ActorSubCell)0x07));
		}

		[TestCase(TestName = "Test free bit selection.")]
		public void GetFreeBit()
		{
			var grid = new MapGrid(MiniYaml.FromString(MapGridYaml, "MapGrid").First().Value);

			var mask = MultiBrush.ActorSubCell.Any;
			var freeBit = MultiBrush.FreeSubCell(grid, mask);
			Assert.That(freeBit, Is.EqualTo(SubCell.First));

			mask |= (MultiBrush.ActorSubCell)0x01;
			freeBit = MultiBrush.FreeSubCell(grid, mask);
			Assert.That(freeBit, Is.EqualTo((SubCell)0x02));

			mask |= (MultiBrush.ActorSubCell)0x02;
			freeBit = MultiBrush.FreeSubCell(grid, mask);
			Assert.That(freeBit, Is.EqualTo((SubCell)0x03));
		}
	}
}
