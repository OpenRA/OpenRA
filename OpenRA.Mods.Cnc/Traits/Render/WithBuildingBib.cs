#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class WithBuildingBibInfo : TraitInfo, Requires<BuildingInfo>, IRenderActorPreviewSpritesInfo, IActorPreviewInitInfo, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		public readonly string Sequence = "bib";

		[PaletteReference]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		public readonly bool HasMinibib = false;

		public override object Create(ActorInitializer init) { return new WithBuildingBib(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (init.Contains<HideBibPreviewInit>(this))
				yield break;

			if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			var bi = init.Actor.TraitInfo<BuildingInfo>();

			var rows = HasMinibib ? 1 : 2;
			var width = bi.Dimensions.X;
			var bibOffset = bi.Dimensions.Y - rows;
			var centerOffset = bi.CenterOffset(init.World);
			var map = init.World.Map;
			var location = init.GetValue<LocationInit, CPos>(CPos.Zero);

			for (var i = 0; i < rows * width; i++)
			{
				var index = i;
				var anim = new Animation(init.World, image);
				var cellOffset = new CVec(i % width, i / width + bibOffset);
				var cell = location + cellOffset;

				// Some mods may define terrain-specific bibs
				var sequence = Sequence;
				if (map.Tiles.Contains(cell))
				{
					var terrain = map.GetTerrainTileInfo(cell).Type;
					var testSequence = Sequence + "-" + terrain;
					if (anim.HasSequence(testSequence))
						sequence = testSequence;
				}

				anim.PlayFetchIndex(sequence, () => index);
				anim.IsDecoration = true;

				// Z-order is one set to the top of the footprint
				var offset = map.CenterOfCell(cell) - map.CenterOfCell(location) - centerOffset;
				yield return new SpriteActorPreview(anim, () => offset, () => -(offset.Y + centerOffset.Y + 512), p);
			}
		}

		IEnumerable<ActorInit> IActorPreviewInitInfo.ActorPreviewInits(ActorInfo ai, ActorPreviewType type)
		{
			yield return new HideBibPreviewInit();
		}
	}

	public class WithBuildingBib : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly WithBuildingBibInfo info;
		readonly RenderSprites rs;
		readonly BuildingInfo bi;
		readonly List<AnimationWithOffset> anims = new List<AnimationWithOffset>();

		public WithBuildingBib(Actor self, WithBuildingBibInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSprites>();
			bi = self.Info.TraitInfo<BuildingInfo>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var rows = info.HasMinibib ? 1 : 2;
			var width = bi.Dimensions.X;
			var bibOffset = bi.Dimensions.Y - rows;
			var centerOffset = bi.CenterOffset(self.World);
			var location = self.Location;
			var map = self.World.Map;

			for (var i = 0; i < rows * width; i++)
			{
				var index = i;
				var anim = new Animation(self.World, rs.GetImage(self));
				var cellOffset = new CVec(i % width, i / width + bibOffset);
				var cell = location + cellOffset;

				// Some mods may define terrain-specific bibs
				var terrain = map.GetTerrainTileInfo(cell).Type;
				var testSequence = info.Sequence + "-" + terrain;
				var sequence = anim.HasSequence(testSequence) ? testSequence : info.Sequence;
				anim.PlayFetchIndex(sequence, () => index);
				anim.IsDecoration = true;

				// Z-order is one set to the top of the footprint
				var offset = self.World.Map.CenterOfCell(cell) - self.World.Map.CenterOfCell(location) - centerOffset;
				var awo = new AnimationWithOffset(anim, () => offset, null, -(offset.Y + centerOffset.Y + 512));
				anims.Add(awo);
				rs.Add(awo, info.Palette);
			}
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			foreach (var a in anims)
				rs.Remove(a);

			anims.Clear();
		}
	}

	class HideBibPreviewInit : RuntimeFlagInit { }
}
