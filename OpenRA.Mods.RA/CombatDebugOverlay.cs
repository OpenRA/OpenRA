#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CombatDebugOverlayInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new CombatDebugOverlay(init.self); }
	}

	public class CombatDebugOverlay : IPostRender
	{
		Lazy<IEnumerable<Armament>> armaments;
		Lazy<Health> health;
		DeveloperMode devMode;

		public CombatDebugOverlay(Actor self)
		{
			armaments = Lazy.New(() => self.TraitsImplementing<Armament>());
			health = Lazy.New(() => self.TraitOrDefault<Health>());

			var localPlayer = self.World.LocalPlayer;
			devMode = localPlayer != null ? localPlayer.PlayerActor.Trait<DeveloperMode>() : null;
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (devMode == null || !devMode.ShowCombatGeometry)
				return;

			if (health.Value != null)
				wr.DrawRangeCircle(self.CenterPosition, health.Value.Info.Radius, Color.Red);

			var wlr = Game.Renderer.WorldLineRenderer;
			var c = Color.White;

			foreach (var a in armaments.Value)
			{
				foreach (var b in a.Barrels)
				{
					var muzzle = self.CenterPosition + a.MuzzleOffset(self, b);
					var dirOffset = new WVec(0, -224, 0).Rotate(a.MuzzleOrientation(self, b));

					var sm = wr.ScreenPosition(muzzle);
					var sd = wr.ScreenPosition(muzzle + dirOffset);
					wlr.DrawLine(sm, sd, c, c);
					wr.DrawTargetMarker(c, sm);
				}
			}
		}
	}
}
