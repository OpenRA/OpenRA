#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Limits the zone where buildings can be constructed to a radius around this actor.")]
	public class BaseProviderInfo : PausableConditionalTraitInfo
	{
		public readonly WDist Range = WDist.FromCells(10);
		public readonly int Cooldown = 0;
		public readonly int InitialDelay = 0;

		public override object Create(ActorInitializer init) { return new BaseProvider(init.Self, this); }
	}

	public class BaseProvider : PausableConditionalTrait<BaseProviderInfo>, ITick, IRenderAboveShroudWhenSelected, ISelectionBar
	{
		readonly DeveloperMode devMode;
		readonly Actor self;
		readonly bool allyBuildEnabled;
		readonly bool buildRadiusEnabled;

		int total;
		int progress;

		public BaseProvider(Actor self, BaseProviderInfo info)
			: base(info)
		{
			this.self = self;
			devMode = self.Owner.PlayerActor.Trait<DeveloperMode>();
			progress = total = info.InitialDelay;
			var mapBuildRadius = self.World.WorldActor.TraitOrDefault<MapBuildRadius>();
			allyBuildEnabled = mapBuildRadius != null && mapBuildRadius.AllyBuildRadiusEnabled;
			buildRadiusEnabled = mapBuildRadius != null && mapBuildRadius.BuildRadiusEnabled;
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
			if (IsTraitDisabled || IsTraitPaused)
				return false;

			return devMode.FastBuild || progress == 0;
		}

		bool ValidRenderPlayer()
		{
			return buildRadiusEnabled && (self.Owner == self.World.RenderPlayer || (allyBuildEnabled && self.Owner.IsAlliedWith(self.World.RenderPlayer)));
		}

		public IEnumerable<IRenderable> RangeCircleRenderables(WorldRenderer wr)
		{
			if (IsTraitDisabled)
				yield break;

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

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return false; } }

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled)
				return 0f;

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
