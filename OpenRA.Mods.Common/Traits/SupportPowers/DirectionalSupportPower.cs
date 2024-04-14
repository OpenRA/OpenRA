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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class DirectionalSupportPowerInfo : SupportPowerInfo
	{
		[Desc("Enables the player directional targeting")]
		public readonly bool UseDirectionalTarget = false;

		[SequenceReference(nameof(DirectionArrowAnimation), allowNullImage: true)]
		public readonly string[] Arrows = { "arrow-t", "arrow-tl", "arrow-l", "arrow-bl", "arrow-b", "arrow-br", "arrow-r", "arrow-tr" };

		[Desc("Animation used to render the direction arrows.")]
		public readonly string DirectionArrowAnimation = null;

		[PaletteReference]
		[Desc("Palette for direction cursor animation.")]
		public readonly string DirectionArrowPalette = "chrome";
	}

	public class DirectionalSupportPower : SupportPower
	{
		readonly DirectionalSupportPowerInfo info;

		public DirectionalSupportPower(Actor self, DirectionalSupportPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			if (info.UseDirectionalTarget)
				self.World.OrderGenerator = new SelectDirectionalTarget(self.World, order, manager, info);
			else
				base.SelectTarget(self, order, manager);
		}
	}
}
