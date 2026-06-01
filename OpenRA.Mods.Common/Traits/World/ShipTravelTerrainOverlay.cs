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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	public class ShipTravelTerrainOverlayInfo : TraitInfo
	{
		[Desc("Locomotor used to determine ship passable terrain.")]
		public readonly string Locomotor = "naval";

		[PaletteReference]
		[Desc("Palette to use for rendering the overlay sprite.")]
		public readonly string Palette = "effect";

		[Desc("Sprite definition.")]
		public readonly string Image = "overlay";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence to use for the cell overlay.")]
		public readonly string Sequence = "build-valid";

		[Desc("Custom opacity to apply to the overlay sprite.")]
		public readonly float Alpha = 0.75f;

		public override object Create(ActorInitializer init)
		{
			return new ShipTravelTerrainOverlay(init.Self, this);
		}
	}

	public class ShipTravelTerrainOverlay : IRenderAboveWorld, IRenderAnnotations, IWorldLoaded
	{
		readonly ShipTravelTerrainOverlayInfo info;
		readonly World world;
		readonly Sprite overlaySprite;
		readonly float overlayScale;
		Locomotor locomotor;
		PaletteReference palette;

		public bool Enabled = false;

		public ShipTravelTerrainOverlay(Actor self, ShipTravelTerrainOverlayInfo info)
		{
			this.info = info;
			world = self.World;
			overlaySprite = EditorCellStatusOverlay.GetOverlaySprite(world, info.Image, info.Sequence);
			overlayScale = EditorCellStatusOverlay.GetOverlayScale(world, info.Image, info.Sequence);

			var locomotorInfo = self.World.Map.Rules.Actors[SystemActors.World].TraitInfos<LocomotorInfo>()
				.SingleOrDefault(li => li.Name == info.Locomotor);

			if (locomotorInfo == null)
				throw new YamlException($"A locomotor named '{info.Locomotor}' doesn't exist.");
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			locomotor = w.WorldActor.TraitsImplementing<Locomotor>()
				.SingleOrDefault(l => l.Info.Name == info.Locomotor);
		}

		bool IsTravelable(CPos cell) => EditorWalkability.IsCellWalkable(world, locomotor, cell);

		void IRenderAboveWorld.RenderAboveWorld(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				return;

			palette ??= wr.Palette(info.Palette);
			EditorCellStatusOverlay.RenderCells(wr, IsTravelable, overlaySprite, overlayScale, info.Alpha, palette);
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				yield break;

			foreach (var r in EditorOverlayCellGrid.Render(wr))
				yield return r;
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
