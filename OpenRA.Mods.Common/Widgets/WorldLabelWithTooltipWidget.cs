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

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class WorldLabelWithTooltipWidget : LabelWithTooltipWidget
	{
		readonly World world;

		[ObjectCreator.UseCtor]
		public WorldLabelWithTooltipWidget(World world)
			: base()
		{
			this.world = world;
		}

		protected WorldLabelWithTooltipWidget(WorldLabelWithTooltipWidget other)
			: base(other)
		{
			world = other.world;
		}

		public override Widget Clone() { return new WorldLabelWithTooltipWidget(this); }

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			if (GetTooltipText != null)
				tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", GetTooltipText }, { "world", world } });
		}
	}
}
