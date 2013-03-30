﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class DebugMuzzlePositionsInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new DebugFiringOffsets(init.self); }
	}

	public class DebugFiringOffsets : IPostRender
	{
		Lazy<IEnumerable<Armament>> armaments;
		DeveloperMode devMode;

		public DebugFiringOffsets(Actor self)
		{
			armaments = Lazy.New(() => self.TraitsImplementing<Armament>());

			var localPlayer = self.World.LocalPlayer;
			devMode = localPlayer != null ? localPlayer.PlayerActor.Trait<DeveloperMode>() : null;
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (devMode == null || !devMode.ShowMuzzles)
				return;

			var wlr = Game.Renderer.WorldLineRenderer;
			var c = Color.White;

			foreach (var a in armaments.Value)
				foreach (var b in a.Barrels)
				{
					var muzzle = self.CenterPosition + a.MuzzleOffset(self, b);
					var dirOffset = new WVec(0,-224,0).Rotate(a.MuzzleOrientation(self, b));

					var sm = wr.ScreenPosition(muzzle);
					var sd = wr.ScreenPosition(muzzle + dirOffset);
					wlr.DrawLine(sm, sd, c, c);
					wlr.DrawLine(sm + new float2(-1, -1), sm + new float2(-1, 1), c, c);
					wlr.DrawLine(sm + new float2(-1, 1), sm + new float2(1, 1), c, c);
					wlr.DrawLine(sm + new float2(1, 1), sm + new float2(1, -1), c, c);
					wlr.DrawLine(sm + new float2(1, -1), sm + new float2(-1, -1), c, c);
				}
		}
	}
}
