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

using System;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class HotkeyEntryWidget : InputWidget
	{
		public Hotkey Key;

		public int VisualHeight = 1;
		public int LeftMargin = 5;
		public int RightMargin = 5;

		public Action<KeyInput> OnEscKey = _ => { };
		public Action OnLoseFocus = () => { };

		public Func<bool> IsValid = () => false;
		public string Font = ChromeMetrics.Get<string>("HotkeyFont");
		public Color TextColor = ChromeMetrics.Get<Color>("HotkeyColor");
		public Color TextColorDisabled = ChromeMetrics.Get<Color>("HotkeyColorDisabled");
		public Color TextColorInvalid = ChromeMetrics.Get<Color>("HotkeyColorInvalid");

		public HotkeyEntryWidget() { }
		protected HotkeyEntryWidget(HotkeyEntryWidget widget)
			: base(widget)
		{
			Font = widget.Font;
			TextColor = widget.TextColor;
			TextColorDisabled = widget.TextColorDisabled;
			TextColorInvalid = widget.TextColorInvalid;
			VisualHeight = widget.VisualHeight;
		}

		public override bool TakeKeyboardFocus()
		{
			return base.TakeKeyboardFocus();
		}

		public override bool YieldKeyboardFocus()
		{
			OnLoseFocus();
			if (!IsValid())
				return false;

			return base.YieldKeyboardFocus();
		}

		public bool ForceYieldKeyboardFocus()
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

			blinkCycle = 15;

			return true;
		}

		static readonly Keycode[] IgnoreKeys = new Keycode[]
		{
			Keycode.RSHIFT, Keycode.LSHIFT,
			Keycode.RCTRL, Keycode.LCTRL,
			Keycode.RALT, Keycode.LALT,
			Keycode.RGUI, Keycode.LGUI,
		};

		public override bool HandleKeyPress(KeyInput e)
		{
			if (IsDisabled() || e.Event == KeyInputEvent.Up)
				return false;

			if (!HasKeyboardFocus || IgnoreKeys.Contains(e.Key))
				return false;

			switch (e.Key)
			{
				case Keycode.ESCAPE:
					OnEscKey(e);
					break;

				default:
					Key = Hotkey.FromKeyInput(e);
					break;
			}

			YieldKeyboardFocus();

			return true;
		}

		protected int blinkCycle = 15;
		protected bool showEntry = true;

		public override void Tick()
		{
			if (HasKeyboardFocus && --blinkCycle <= 0)
			{
				blinkCycle = 15;
				showEntry ^= true;
			}
		}

		public override void Draw()
		{
			var apparentText = Key.DisplayString();

			var font = Game.Renderer.Fonts[Font];
			var pos = RenderOrigin;

			var textSize = font.Measure(apparentText);

			var disabled = IsDisabled();
			var valid = IsValid();
			var state = WidgetUtils.GetStatefulImageName("textfield", disabled, false, Ui.MouseOverWidget == this, HasKeyboardFocus);

			WidgetUtils.DrawPanel(state, RenderBounds);

			// Blink the current entry to indicate focus
			if (HasKeyboardFocus && !showEntry)
				return;

			// Inset text by the margin and center vertically
			var textPos = pos + new int2(LeftMargin, (Bounds.Height - textSize.Y) / 2 - VisualHeight);

			// Scissor when the text overflows
			var isTextOverflowing = textSize.X > Bounds.Width - LeftMargin - RightMargin;
			if (isTextOverflowing)
			{
				Game.Renderer.EnableScissor(new Rectangle(pos.X + LeftMargin, pos.Y,
					Bounds.Width - LeftMargin - RightMargin, Bounds.Bottom));
			}

			var color = disabled ? TextColorDisabled : !valid ? TextColorInvalid : TextColor;
			font.DrawText(apparentText, textPos, color);

			if (isTextOverflowing)
				Game.Renderer.DisableScissor();
		}

		public override Widget Clone() { return new HotkeyEntryWidget(this); }
	}
}
