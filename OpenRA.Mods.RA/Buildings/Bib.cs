#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	public class BibInfo : ITraitInfo, Requires<BuildingInfo>, Requires<RenderSpritesInfo>
	{
		public readonly string Sequence = "bib";
		public readonly string Palette = "terrain";

		public object Create(ActorInitializer init) { return new Bib(init.self, this); }
	}

	public class Bib : IRender, INotifyAddedToWorld
	{
		readonly BibInfo info;
		List<AnimationWithOffset> tiles;

		public Bib(Actor self, BibInfo info)
		{
			this.info = info;
		}

		public void AddedToWorld(Actor self)
		{
			var rs = self.Trait<RenderSprites>();
			var building = self.Info.Traits.Get<BuildingInfo>();
			var width = building.Dimensions.X;
			var bibOffset = building.Dimensions.Y - 1;
			var centerOffset = FootprintUtils.CenterOffset(building);
			var location = self.Location;
			tiles = new List<AnimationWithOffset>();
			for (var i = 0; i < 2*width; i++)
			{
				var index = i;
				var anim = new Animation(rs.GetImage(self));
				var cellOffset = new CVec(i % width, i / width + bibOffset);

				// Some mods may define terrain-specific bibs
				var terrain = self.World.GetTerrainType(location + cellOffset);
				var testSequence = info.Sequence + "-" + terrain;
				var sequence = anim.HasSequence(testSequence) ? testSequence : info.Sequence;
				anim.PlayFetchIndex(sequence, () => index);
				anim.IsDecoration = true;

				// Z-order is one set to the top of the footprint
				var offset = cellOffset.ToWVec() - centerOffset;
				tiles.Add(new AnimationWithOffset(anim, () => offset, null, -(offset.Y + centerOffset.Y + 512)));
			}
		}

		bool paletteInitialized;
		PaletteReference palette;
		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			if (!paletteInitialized)
			{
				palette = wr.Palette(info.Palette);
				paletteInitialized = true;
			}

			return tiles.SelectMany(t => t.Render(self, wr, palette, 1f));
		}
	}
}
