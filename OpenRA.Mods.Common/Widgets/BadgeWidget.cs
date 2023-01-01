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
	public class BadgeWidget : Widget
	{
		public PlayerBadge Badge;
		readonly PlayerDatabase playerDatabase;

		[ObjectCreator.UseCtor]
		public BadgeWidget(ModData modData)
		{
			playerDatabase = modData.Manifest.Get<PlayerDatabase>();
		}

		protected BadgeWidget(BadgeWidget other)
			: base(other)
		{
			Badge = other.Badge;
			playerDatabase = other.playerDatabase;
		}

		public override Widget Clone() { return new BadgeWidget(this); }

		public override void Draw()
		{
			if (Badge == null)
				return;

			var icon = playerDatabase.GetIcon(Badge);
			if (icon != null)
				WidgetUtils.DrawSprite(icon, RenderOrigin);
		}
	}
}
