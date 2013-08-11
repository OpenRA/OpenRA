#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	public class BaseProviderInfo : ITraitInfo
	{
		public readonly int Range = 10;
		public readonly int Cooldown = 0;
		public readonly int InitialDelay = 0;

		public object Create(ActorInitializer init) { return new BaseProvider(init.self, this); }
	}

	public class BaseProvider : ITick, IPostRenderSelection, ISelectionBar
	{
		public readonly BaseProviderInfo Info;
		DeveloperMode devMode;
		Actor self;
		int total;
		int progress;

		public BaseProvider(Actor self, BaseProviderInfo info)
		{
			Info = info;
			this.self = self;
			devMode = self.Owner.PlayerActor.Trait<DeveloperMode>();
			progress = total = info.InitialDelay;
		}

		public void Tick(Actor self)
		{
			if (progress > 0)
				progress--;
		}

		public void BeginCooldown()
		{
			progress = total = Info.Cooldown;
		}

		public bool Ready()
		{
			return devMode.FastBuild || progress == 0;
		}

		// Range circle
		public void RenderAfterWorld(WorldRenderer wr)
		{
			// Visible to player and allies
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return;

			wr.DrawRangeCircleWithContrast(
				Color.FromArgb(128, Ready() ? Color.White : Color.Red),
				wr.ScreenPxPosition(self.CenterPosition), Info.Range,
				Color.FromArgb(96, Color.Black), 1);
		}

		// Selection bar
		public float GetValue()
		{
			// Visible to player and allies
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0f;

			// Ready or delay disabled
			if (progress == 0 || total == 0 || devMode.FastBuild)
				return 0f;

			return (float)progress / total;
		}

		public Color GetColor() { return Color.Purple; }
	}
}
