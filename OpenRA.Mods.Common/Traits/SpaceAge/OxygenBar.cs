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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Draws an oxygen level bar above the unit. Requires the Oxygen trait.",
		"Rendered by the standard SelectionBarsDecoration (ISelectionBar provider).")]
	public class OxygenBarInfo : TraitInfo, Requires<OxygenInfo>
	{
		[Desc("Colour of the oxygen bar (cyan by default).")]
		public readonly Color Color = Color.FromArgb(80, 200, 255);

		[Desc("Draw the bar even when oxygen is full.")]
		public readonly bool AlwaysDisplay = false;

		public override object Create(ActorInitializer init) { return new OxygenBar(init, this); }
	}

	public class OxygenBar : ISelectionBar
	{
		readonly OxygenBarInfo info;
		readonly Actor self;
		Oxygen oxygen;

		public OxygenBar(ActorInitializer init, OxygenBarInfo info)
		{
			this.info = info;
			self = init.Self;
		}

		// Resolve the Oxygen trait lazily so we don't depend on trait construction order.
		Oxygen O2 => oxygen ??= self.TraitOrDefault<Oxygen>();

		float ISelectionBar.GetValue()
		{
			var o2 = O2;
			if (o2 == null)
				return 0f;

			// Hide the bar at full O2 unless AlwaysDisplay, matching health-bar behaviour.
			if (!info.AlwaysDisplay && o2.Fraction >= 1f)
				return 0f;

			return o2.Fraction;
		}

		Color ISelectionBar.GetColor() { return info.Color; }

		bool ISelectionBar.DisplayWhenEmpty => info.AlwaysDisplay;
	}
}
