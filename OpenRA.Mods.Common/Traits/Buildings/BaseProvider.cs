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

		[Desc("Range circle color when operational.")]
		public readonly Color CircleReadyColor = Color.FromArgb(128, Color.White);

		[Desc("Range circle color when inactive.")]
		public readonly Color CircleBlockedColor = Color.FromArgb(128, Color.Red);

		[Desc("Range circle line width.")]
		public readonly float CircleWidth = 1;

		[Desc("Range circle border color.")]
		public readonly Color CircleBorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float CircleBorderWidth = 3;

		public override object Create(ActorInitializer init) { return new BaseProvider(init.Self, this); }
	}

	public class BaseProvider : PausableConditionalTrait<BaseProviderInfo>, ITick, IRenderAnnotationsWhenSelected, ISelectionBar
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

		public IEnumerable<IRenderable> RangeCircleRenderables()
		{
			if (IsTraitDisabled)
				yield break;

			// Visible to player and allies
			if (!ValidRenderPlayer())
				yield break;

			yield return new RangeCircleAnnotationRenderable(
				self.CenterPosition,
				Info.Range,
				0,
				Ready() ? Info.CircleReadyColor : Info.CircleBlockedColor,
				Info.CircleWidth,
				Info.CircleBorderColor,
				Info.CircleBorderWidth);
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RangeCircleRenderables();
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;

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
		bool ISelectionBar.DisplayWhenEmpty => false;
	}
}
