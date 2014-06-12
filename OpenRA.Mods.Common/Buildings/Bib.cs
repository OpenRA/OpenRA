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

namespace OpenRA.Mods.Common.Buildings
{
	public class BibInfo : ITraitInfo, Requires<BuildingInfo>, Requires<RenderSpritesInfo>
	{
		public readonly string Sequence = "bib";
		public readonly string Palette = "terrain";
		public readonly bool HasMinibib = false;

		public object Create(ActorInitializer init) { return new Bib(init.self, this); }
	}

	public class Bib : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly BibInfo info;
		readonly RenderSprites rs;
		readonly BuildingInfo bi;

		public Bib(Actor self, BibInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSprites>();
			bi = self.Info.Traits.Get<BuildingInfo>();
		}

		public void AddedToWorld(Actor self)
		{
			var width = bi.Dimensions.X;
			var bibOffset = bi.Dimensions.Y - 1;
			var centerOffset = FootprintUtils.CenterOffset(bi);
			var location = self.Location;
			var rows = info.HasMinibib ? 1 : 2;

			for (var i = 0; i < rows * width; i++)
			{
				var index = i;
				var anim = new Animation(self.World, rs.GetImage(self));
				var cellOffset = new CVec(i % width, i / width + bibOffset);

				// Some mods may define terrain-specific bibs
				var terrain = self.World.GetTerrainType(location + cellOffset);
				var testSequence = info.Sequence + "-" + terrain;
				var sequence = anim.HasSequence(testSequence) ? testSequence : info.Sequence;
				anim.PlayFetchIndex(sequence, () => index);
				anim.IsDecoration = true;

				// Z-order is one set to the top of the footprint
				var offset = cellOffset.ToWVec() - centerOffset;
				var awo = new AnimationWithOffset(anim, () => offset, null, -(offset.Y + centerOffset.Y + 512));
				rs.Add("bib_{0}".F(i), awo, info.Palette);
			}
		}

		public void RemovedFromWorld(Actor self)
		{
			var width = bi.Dimensions.X;
			var rows = info.HasMinibib ? 1 : 2;

			for (var i = 0; i < rows * width; i++)
				rs.Remove("bib_{0}".F(i));
		}
	}
}
