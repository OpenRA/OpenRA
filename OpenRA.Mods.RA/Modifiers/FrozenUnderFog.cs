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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class FrozenUnderFogInfo : ITraitInfo, Requires<BuildingInfo>, Requires<RenderSpritesInfo>
	{
		public object Create(ActorInitializer init) { return new FrozenUnderFog(init.self); }
	}

	public class FrozenUnderFog : IRenderModifier, IVisibilityModifier, ITickRender
	{
		FrozenActorProxy proxy;
		IEnumerable<CPos> footprint;
		bool visible;

		public FrozenUnderFog(Actor self)
		{
			footprint = FootprintUtils.Tiles(self);
			proxy = new FrozenActorProxy(self, footprint);
			self.World.AddFrameEndTask(w => w.Add(proxy));
		}

		public bool IsVisible(Actor self, Player byPlayer)
		{
			return byPlayer == null || footprint.Any(c => byPlayer.Shroud.IsVisible(c));
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (self.Destroyed)
				return;

			visible = IsVisible(self, self.World.RenderPlayer);
			if (visible)
				proxy.SetRenderables(self.Render(wr));
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return visible ? r : SpriteRenderable.None;
		}
	}
}