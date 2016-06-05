#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class BibInfo : ITraitInfo, Requires<BuildingInfo>, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>
	{
		[SequenceReference] public readonly string Sequence = "bib";
		[PaletteReference] public readonly string Palette = TileSet.TerrainPaletteInternalName;
		public readonly bool HasMinibib = false;

		public object Create(ActorInitializer init) { return new Bib(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (init.Contains<HideBibPreviewInit>() && init.Get<HideBibPreviewInit, bool>())
				yield break;

			if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			var bi = init.Actor.TraitInfo<BuildingInfo>();

			var width = bi.Dimensions.X;
			var bibOffset = bi.Dimensions.Y - 1;
			var centerOffset = FootprintUtils.CenterOffset(init.World, bi);
			var rows = HasMinibib ? 1 : 2;
			var map = init.World.Map;
			var location = CPos.Zero;

			if (init.Contains<LocationInit>())
				location = init.Get<LocationInit, CPos>();

			for (var i = 0; i < rows * width; i++)
			{
				var index = i;
				var anim = new Animation(init.World, image);
				var cellOffset = new CVec(i % width, i / width + bibOffset);
				var cell = location + cellOffset;

				// Some mods may define terrain-specific bibs
				var terrain = map.GetTerrainInfo(cell).Type;
				var testSequence = Sequence + "-" + terrain;
				var sequence = anim.HasSequence(testSequence) ? testSequence : Sequence;
				anim.PlayFetchIndex(sequence, () => index);
				anim.IsDecoration = true;

				// Z-order is one set to the top of the footprint
				var offset = map.CenterOfCell(cell) - map.CenterOfCell(location) - centerOffset;
				yield return new SpriteActorPreview(anim, () => offset, () => -(offset.Y + centerOffset.Y + 512), p, rs.Scale);
			}
		}
	}

	public class Bib : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly BibInfo info;
		readonly RenderSprites rs;
		readonly BuildingInfo bi;
		readonly List<AnimationWithOffset> anims = new List<AnimationWithOffset>();

		public Bib(Actor self, BibInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSprites>();
			bi = self.Info.TraitInfo<BuildingInfo>();
		}

		public void AddedToWorld(Actor self)
		{
			var width = bi.Dimensions.X;
			var bibOffset = bi.Dimensions.Y - 1;
			var centerOffset = FootprintUtils.CenterOffset(self.World, bi);
			var location = self.Location;
			var rows = info.HasMinibib ? 1 : 2;
			var map = self.World.Map;

			for (var i = 0; i < rows * width; i++)
			{
				var index = i;
				var anim = new Animation(self.World, rs.GetImage(self));
				var cellOffset = new CVec(i % width, i / width + bibOffset);
				var cell = location + cellOffset;

				// Some mods may define terrain-specific bibs
				var terrain = map.GetTerrainInfo(cell).Type;
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

		public void RemovedFromWorld(Actor self)
		{
			foreach (var a in anims)
				rs.Remove(a);

			anims.Clear();
		}
	}

	public class HideBibPreviewInit : IActorInit<bool>, ISuppressInitExport
	{
		[FieldFromYamlKey] readonly bool value = true;
		public HideBibPreviewInit() { }
		public HideBibPreviewInit(bool init) { value = init; }
		public bool Value(World world) { return value; }
	}
}
