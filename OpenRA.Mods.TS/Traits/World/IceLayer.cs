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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits
{
	[Desc("Attach this to the world layer for regrowable ice terrain.")]
	class IceLayerInfo : ITraitInfo
	{
		[Desc("Tileset IDs where the trait is activated.")]
		public readonly string[] Tilesets = { "SNOW" };

		public readonly string ImpassableTerrainType = "Water";

		public readonly Dictionary<ushort, int> StrengthPerTile = new Dictionary<ushort, int>
		{
			// Ice 01
			{ 439, 5 },

			{ 440, 4 }, { 441, 4 }, { 442, 4 }, { 443, 4 }, { 444, 4 }, { 445, 4 }, { 446, 4 }, { 447, 4 },
			{ 448, 4 }, { 449, 4 }, { 450, 4 }, { 451, 4 }, { 452, 4 }, { 453, 4 }, { 454, 4 }, { 455, 4 },

			{ 456, 3 }, { 457, 3 }, { 458, 3 }, { 459, 3 }, { 460, 3 }, { 461, 3 }, { 462, 3 }, { 463, 3 },
			{ 464, 3 }, { 465, 3 }, { 466, 3 }, { 467, 3 }, { 468, 3 }, { 469, 3 }, { 470, 3 }, { 471, 3 },
			{ 472, 3 }, { 473, 3 }, { 474, 3 }, { 475, 3 }, { 476, 3 }, { 477, 3 }, { 478, 3 }, { 479, 3 },
			{ 480, 3 }, { 481, 3 }, { 482, 3 }, { 483, 3 }, { 484, 3 }, { 485, 3 }, { 486, 3 },

			{ 487, 2 }, { 488, 2 }, { 489, 2 }, { 490, 2 }, { 491, 2 }, { 492, 2 }, { 493, 2 }, { 494, 2 },
			{ 495, 2 }, { 496, 2 }, { 497, 2 }, { 498, 2 }, { 499, 2 }, { 500, 2 }, { 501, 2 },

			{ 502, 1 },

			// Ice 02
			{ 503, 5 },

			{ 504, 4 }, { 505, 4 }, { 506, 4 }, { 507, 4 }, { 508, 4 }, { 509, 4 }, { 510, 4 }, { 511, 4 },
			{ 512, 4 }, { 513, 4 }, { 514, 4 }, { 515, 4 }, { 516, 4 }, { 517, 4 }, { 518, 4 }, { 519, 4 },

			{ 520, 3 }, { 521, 3 }, { 522, 3 }, { 523, 3 }, { 524, 3 }, { 525, 3 }, { 526, 3 }, { 527, 3 },
			{ 528, 3 }, { 529, 3 }, { 530, 3 }, { 531, 3 }, { 532, 3 }, { 533, 3 }, { 534, 3 }, { 535, 3 },
			{ 536, 3 }, { 537, 3 }, { 538, 3 }, { 539, 3 }, { 540, 3 }, { 541, 3 }, { 542, 3 }, { 543, 3 },
			{ 544, 3 }, { 545, 3 }, { 546, 3 }, { 547, 3 }, { 548, 3 }, { 549, 3 }, { 550, 3 },

			{ 551, 2 }, { 552, 2 }, { 553, 2 }, { 554, 2 }, { 555, 2 }, { 556, 2 }, { 557, 2 }, { 558, 2 },
			{ 559, 2 }, { 560, 2 }, { 561, 2 }, { 562, 2 }, { 563, 2 }, { 564, 2 }, { 565, 2 },

			{ 566, 1 },

			// Ice 03
			{ 567, 5 },

			{ 568, 4 }, { 569, 4 }, { 570, 4 }, { 571, 4 }, { 572, 4 }, { 573, 4 }, { 574, 4 }, { 575, 4 },
			{ 576, 4 }, { 577, 4 }, { 578, 4 }, { 579, 4 }, { 580, 4 }, { 581, 4 }, { 582, 4 }, { 583, 4 },

			{ 584, 3 }, { 585, 3 }, { 586, 3 }, { 587, 3 }, { 588, 3 }, { 589, 3 }, { 590, 3 }, { 591, 3 },
			{ 592, 3 }, { 593, 3 }, { 594, 3 }, { 595, 3 }, { 596, 3 }, { 597, 3 }, { 598, 3 }, { 599, 3 },
			{ 600, 3 }, { 601, 3 }, { 602, 3 }, { 603, 3 }, { 604, 3 }, { 605, 3 }, { 606, 3 }, { 607, 3 },
			{ 608, 3 }, { 609, 3 }, { 610, 3 }, { 611, 3 }, { 612, 3 }, { 613, 3 }, { 614, 3 },

			{ 615, 2 }, { 616, 2 }, { 617, 2 }, { 618, 2 }, { 619, 2 }, { 620, 2 }, { 621, 2 }, { 622, 2 },
			{ 623, 2 }, { 624, 2 }, { 625, 2 }, { 626, 2 }, { 627, 2 }, { 628, 2 }, { 629, 2 },

			{ 630, 1 }
		};

		public object Create(ActorInitializer init) { return new IceLayer(init.Self, this); }
	}

	class IceLayer : IWorldLoaded
	{
		readonly IceLayerInfo info;

		public readonly CellLayer<int> Strength;

		public IceLayer(Actor self, IceLayerInfo info)
		{
			this.info = info;

			Strength = new CellLayer<int>(self.World.Map);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (!info.Tilesets.Contains(w.Map.Tileset))
				return;

			var updatedCells = new List<CPos>();

			var mapTiles = w.Map.MapTiles.Value;
			foreach (var cell in w.Map.AllCells)
			{
				var tile = mapTiles[cell];
				var template = w.TileSet.Templates[tile.Type];
				if (info.StrengthPerTile.ContainsKey(template.Id))
				{
					var strength = info.StrengthPerTile[template.Id];
					Strength[cell] = strength;

					if (strength <= 2)
					{
						w.Map.CustomTerrain[cell] = w.TileSet.GetTerrainIndex(info.ImpassableTerrainType);
						updatedCells.Add(cell);
					}
				}
			}

			var domainIndex = w.WorldActor.TraitOrDefault<DomainIndex>();
			if (domainIndex != null)
				domainIndex.UpdateCells(w, updatedCells);
		}
	}
}
