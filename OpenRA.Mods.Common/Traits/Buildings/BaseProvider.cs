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
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Limits the zone where buildings can be constructed to a radius around this actor.")]
	public class BaseProviderInfo : ITraitInfo
	{
		public readonly WDist Range = WDist.FromCells(10);
		public readonly int Cooldown = 0;
		public readonly int InitialDelay = 0;

		public object Create(ActorInitializer init) { return new BaseProvider(init.Self, this); }
	}

	public class BaseProvider : ITick, INotifyCreated, IRenderAboveShroudWhenSelected, ISelectionBar
	{
		public readonly BaseProviderInfo Info;
		readonly DeveloperMode devMode;
		readonly Actor self;

		Building building;

		int total;
		int progress;
		bool allyBuildEnabled;

		public BaseProvider(Actor self, BaseProviderInfo info)
		{
			Info = info;
			this.self = self;
			devMode = self.Owner.PlayerActor.Trait<DeveloperMode>();
			progress = total = info.InitialDelay;
			allyBuildEnabled = self.World.WorldActor.Trait<MapBuildRadius>().AllyBuildRadiusEnabled;
		}

		void INotifyCreated.Created(Actor self)
		{
			building = self.TraitOrDefault<Building>();
		}

		void ITick.Tick(Actor self)
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
			if (building != null && building.Locked)
				return false;

			return devMode.FastBuild || progress == 0;
		}

		bool ValidRenderPlayer()
		{
			return self.Owner == self.World.RenderPlayer || (allyBuildEnabled && self.Owner.IsAlliedWith(self.World.RenderPlayer));
		}

		public IEnumerable<IRenderable> RangeCircleRenderables(WorldRenderer wr)
		{
			// Visible to player and allies
			if (!ValidRenderPlayer())
				yield break;

			yield return new RangeCircleRenderable(
				self.CenterPosition,
				Info.Range,
				0,
				Color.FromArgb(128, Ready() ? Color.White : Color.Red),
				Color.FromArgb(96, Color.Black));
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return RangeCircleRenderables(wr);
		}

		float ISelectionBar.GetValue()
		{
			// Visible to player and allies
			if (!ValidRenderPlayer())
				return 0f;

			// Ready or delay disabled
			if (progress == 0 || total == 0 || devMode.FastBuild)
				return 0f;

			return (float)progress / total;
		}

		Color ISelectionBar.GetColor() { return Color.Purple; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
