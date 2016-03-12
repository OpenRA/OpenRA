#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public abstract class EditorCellRegionLayerInfo : ITraitInfo
	{
		[PaletteReference]
		[Desc("Palette to use for rendering the placement sprite.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[Desc("Sequence image where the selection overlay types are defined.")]
		public readonly string Image = "editor-overlay";

		public abstract object Create(ActorInitializer init);
	}

	public abstract class EditorCellRegionLayer : IWorldLoaded, IPostRender
	{
		protected readonly EditorCellRegionLayerInfo Info;
		protected readonly Map Map;
		protected PaletteReference palette;

		public EditorCellRegionLayer(Actor self, EditorCellRegionLayerInfo info)
		{
			if (self.World.Type != WorldType.Editor)
				return;

			Info = info;
			Map = self.World.Map;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (w.Type != WorldType.Editor)
				return;

			palette = wr.Palette(Info.Palette);
		}

		public abstract void Clear();

		public virtual void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (wr.World.Type != WorldType.Editor)
				return;
		}
	}
}
