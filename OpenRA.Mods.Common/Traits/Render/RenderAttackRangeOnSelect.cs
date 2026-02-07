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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Draw a circle indicating the unit's attack range when selected and hotkey is held. " +
		"Only renders if the actor has an AttackBase trait.")]
	public class RenderAttackRangeOnSelectInfo : TraitInfo
	{
		[Desc("Which circle to show. Valid values are `Maximum`, and `Minimum`.")]
		public readonly RangeCircleMode RangeCircleMode = RangeCircleMode.Maximum;

		public override object Create(ActorInitializer init) { return new RenderAttackRangeOnSelect(init.Self, this); }
	}

	public class RenderAttackRangeOnSelect : IRenderAnnotationsWhenSelected
	{
		readonly RenderAttackRangeOnSelectInfo info;
		AttackBase attack;
		AttackRangeCirclesOptions options;

		public RenderAttackRangeOnSelect(Actor self, RenderAttackRangeOnSelectInfo info)
		{
			this.info = info;
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			attack ??= self.TraitOrDefault<AttackBase>();
			if (attack == null)
				yield break;

			options ??= self.World.WorldActor.TraitOrDefault<AttackRangeCirclesOptions>();
			if (options == null || !options.ShouldShowCircles)
				yield break;

			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var range = info.RangeCircleMode == RangeCircleMode.Minimum
				? attack.GetMinimumRange()
				: attack.GetMaximumRange();

			if (range == WDist.Zero)
				yield break;

			yield return new RangeCircleAnnotationRenderable(
				self.CenterPosition,
				range,
				0,
				options.CircleColor,
				options.CircleWidth,
				options.CircleBorderColor,
				options.CircleBorderWidth);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;
	}
}
