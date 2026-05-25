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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ConfirmationDialogLogic : ChromeLogic
	{
		readonly Widget widget;
		readonly Widget buttonRow;
		int headerExpansion;
		Size lastResolution;

		[ObjectCreator.UseCtor]
		public ConfirmationDialogLogic(Widget widget)
		{
			this.widget = widget;
			buttonRow = widget.GetOrNull("BUTTON_ROW");
			lastResolution = Game.Renderer.Resolution;
		}

		public void SetHeaderExpansion(int expansion)
		{
			headerExpansion = expansion;
		}

		public override void Tick()
		{
			var currentResolution = Game.Renderer.Resolution;
			if (lastResolution == currentResolution)
				return;

			lastResolution = currentResolution;

			if (headerExpansion == 0)
				return;

			// RecalculateBounds() has reset widget.Bounds from YAML — re-apply the expansion
			// that ConfirmationDialogs.ButtonPrompt() applied at dialog-open time.
			widget.Bounds.Height += headerExpansion;
			widget.Bounds.Y -= headerExpansion / 2;

			if (buttonRow != null)
				buttonRow.Bounds.Y += headerExpansion;

			widget.MarkLayoutDirty();
		}
	}
}
