#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	class LabelWithSelectionWidget : LabelWidget
	{
		// We don't provide a metrics.yaml property for this as it should really be either (in order of preference):
		// 1. A color explicitly given to this Widget e.g. from a chrome/**/*/yml file.
		// 2. A color configured for all LabelWithSelectionWidgets in metrics.yml
		// 3. A the text color given explcitly to the widget this inherits from.
		[Translate]
		public Color? TextColorSelected = ChromeMetrics.GetOrValue<Color?>("TextColorSelected", null);
		public Color BackgroundColorSelected = ChromeMetrics.Get<Color>("TextBackgroundColorSelected");

		static Selection selection = new Selection();

		public LabelWithSelectionWidget() { }

		protected LabelWithSelectionWidget(LabelWithSelectionWidget other)
			: base(other)
		{
			TextColorSelected = other.TextColorSelected;
			BackgroundColorSelected = other.BackgroundColorSelected;
		}

		protected override void DrawInner(string text, SpriteFont font, Color color, int2 position)
		{
			var selectedColor = TextColorSelected ?? color;
			var selectedBackgroundColor = BackgroundColorSelected;

			if (selection.State != Selection.States.Empty && selection.OwnedBy(this))
				font.DrawTextWithSelection(
						text,
						position,
						color,
						selectedColor,
						selectedBackgroundColor,
						selection.Start,
						selection.End);
			else
				base.DrawInner(text, font, color, position);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			switch (mi.Event)
			{
				case MouseInputEvent.Down:
					// clicking outside of the element should loose mouse focus and kill the selection
					if (HasMouseFocus && (!IsVisible() || !GetEventBounds().Contains(mi.Location)))
					{
						selection.HandleLooseMouseFocus();
						return YieldMouseFocus(mi);
					}

					TakeMouseFocus(mi);
					return selection.HandleMouseDown(this, mi.Location);
				case MouseInputEvent.Move:
					return selection.HandleMouseMove(mi.Location);
				case MouseInputEvent.Up:
					TakeKeyboardFocus();
					return selection.HandleMouseUp();
			}

			return false;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up)
				return false;

			var isOSX = Platform.CurrentPlatform == PlatformType.OSX;

			if ((!isOSX && !e.Modifiers.HasModifier(Modifiers.Ctrl)) || (isOSX && !e.Modifiers.HasModifier(Modifiers.Meta)))
				return false;

			if (e.Key != Keycode.C)
				return false;

			if (!selection.OwnedBy(this))
				return false;

			if (selection.State == Selection.States.Empty)
				return false;

			if (string.IsNullOrEmpty(selection.SelectedText))
				return false;

			Game.Renderer.SetClipboardText(selection.SelectedText);

			return true;
		}

		public override Widget Clone() { return new LabelWithSelectionWidget(this); }
	}
}
