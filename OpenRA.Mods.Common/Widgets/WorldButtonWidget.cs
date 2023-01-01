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
	public class WorldButtonWidget : ButtonWidget
	{
		readonly World world;

		[ObjectCreator.UseCtor]
		public WorldButtonWidget(ModData modData, World world)
			: base(modData)
		{
			this.world = world;
		}

		protected WorldButtonWidget(WorldButtonWidget other)
			: base(other)
		{
			world = other.world;
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null || GetTooltipText() == null)
				return;

			tooltipContainer.Value.SetTooltip(TooltipTemplate,
				new WidgetArgs { { "button", this }, { "getText", GetTooltipText }, { "getDesc", GetTooltipDesc }, { "world", world } });
		}

		public override Widget Clone() { return new WorldButtonWidget(this); }
	}
}
