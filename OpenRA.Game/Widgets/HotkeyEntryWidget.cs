#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class HotkeyEntryWidget : Widget
	{
		public Hotkey Key;

		public int VisualHeight = 1;
		public int LeftMargin = 5;
		public int RightMargin = 5;

		public Action OnLoseFocus = () => { };

		public Func<bool> IsDisabled = () => false;
		public Color TextColor = Color.White;
		public Color DisabledColor = Color.Gray;
		public string Font = "Regular";

		public HotkeyEntryWidget() : base() {}
		protected HotkeyEntryWidget(HotkeyEntryWidget widget)
			: base(widget)
		{
			Font = widget.Font;
			TextColor = widget.TextColor;
			DisabledColor = widget.DisabledColor;
			VisualHeight = widget.VisualHeight;
		}

		public override bool YieldKeyboardFocus()
		{
			OnLoseFocus();
			return base.YieldKeyboardFocus();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (IsDisabled())
				return false;

			if (mi.Event != MouseInputEvent.Down)
				return false;

			// Attempt to take keyboard focus
			if (!RenderBounds.Contains(mi.Location) || !TakeKeyboardFocus())
				return false;

			return true;
		}

		static readonly Keycode[] IgnoreKeys = new Keycode[]
		{
			Keycode.RSHIFT, Keycode.LSHIFT,
			Keycode.RCTRL, Keycode.LCTRL,
			Keycode.RALT, Keycode.LALT,
			Keycode.RMETA, Keycode.LMETA
		};

		public override bool HandleKeyPress(KeyInput e)
		{
			if (IsDisabled() || e.Event == KeyInputEvent.Up)
				return false;

			if (!HasKeyboardFocus || IgnoreKeys.Contains(e.Key))
				return false;

			Key = Hotkey.FromKeyInput(e);

			return true;
		}

		public override void Draw()
		{
			var apparentText = Key.DisplayString();

			var font = Game.Renderer.Fonts[Font];
			var pos = RenderOrigin;

			var textSize = font.Measure(apparentText);

			var disabled = IsDisabled();
			var state = disabled ? "textfield-disabled" :
				HasKeyboardFocus ? "textfield-focused" :
					Ui.MouseOverWidget == this ? "textfield-hover" :
					"textfield";

			WidgetUtils.DrawPanel(state, RenderBounds);

			// Inset text by the margin and center vertically
			var textPos = pos + new int2(LeftMargin, (Bounds.Height - textSize.Y) / 2 - VisualHeight);

			// Scissor when the text overflows
			if (textSize.X > Bounds.Width - LeftMargin - RightMargin)
			{
				Game.Renderer.EnableScissor(pos.X + LeftMargin, pos.Y,
					Bounds.Width - LeftMargin - RightMargin, Bounds.Bottom);
			}

			var color = disabled ? DisabledColor : TextColor;
			font.DrawText(apparentText, textPos, color);

			if (textSize.X > Bounds.Width - LeftMargin - RightMargin)
				Game.Renderer.DisableScissor();
		}

		public override Widget Clone() { return new HotkeyEntryWidget(this); }
	}
}