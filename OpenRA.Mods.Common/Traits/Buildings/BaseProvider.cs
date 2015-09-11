#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Limits the zone where buildings can be constructed to a radius around this actor.",
		"Provides \"base\" range type.")]
	public class BaseProviderInfo : ITraitInfo, IRanged, IProvidesRangesInfo
	{
		[FieldLoader.Ignore] public readonly IEnumerable<IRanged> AsRanges;

		public readonly int Range = 10;
		public readonly int Cooldown = 0;
		public readonly int InitialDelay = 0;

		public BaseProviderInfo() { AsRanges = new IRanged[] { this }; }
		public object Create(ActorInitializer init) { return new BaseProvider(init.Self, this); }
		public WDist GetMaximumRange(ActorInfo ai, World w) { return WDist.FromCells(Range); }
		public WDist GetMinimumRange(ActorInfo ai, World w) { return WDist.Zero; }
		public IEnumerable<IRanged> GetRanges(string type, string variant, ActorInfo ai, World w) { return AsRanges; }
		public bool ProvidesRanges(string type, string variant, ActorInfo ai, World w)
		{
			return type == "base" && (string.IsNullOrEmpty(variant) || variant == "ready");
		}
	}

	public class BaseProvider : ITick, IProvidesRanges, ISelectionBar
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

		bool ValidRenderPlayer()
		{
			var allyBuildRadius = self.World.LobbyInfo.GlobalSettings.AllyBuildRadius;
			return self.Owner == self.World.RenderPlayer || (allyBuildRadius && self.Owner.IsAlliedWith(self.World.RenderPlayer));
		}

		public bool ProvidesRanges(string type, string variant)
		{
			return type == "base" && (string.IsNullOrEmpty(variant) || variant == "ready" || variant == "busy");
		}

		public IEnumerable<IRanged> GetRanges(string type, string variant)
		{
			// Only visible to allies if "Ally build radius" is set
			if (self.Owner != self.World.RenderPlayer && !self.World.LobbyInfo.GlobalSettings.AllyBuildRadius)
				return Traits.ProvidesRanges.NoRanges;

			// Provide range only if no variant or variant matches state (both are ready or busy)
			return (string.IsNullOrEmpty(variant) || (variant[0] == 'r') == Ready()) ? Info.AsRanges : Traits.ProvidesRanges.NoRanges;
		}

		// Selection bar
		public float GetValue()
		{
			// Visible to player and allies
			if (!ValidRenderPlayer())
				return 0f;

			// Ready or delay disabled
			if (progress == 0 || total == 0 || devMode.FastBuild)
				return 0f;

			return (float)progress / total;
		}

		public Color GetColor() { return Color.Purple; }
	}
}
