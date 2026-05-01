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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ColorBlockWidget : Widget
	{
		public Color Color { get; set; }
		public Func<Color> GetColor;
		public Action<MouseInput> OnMouseDown = _ => { };
		public Action<MouseInput> OnMouseUp = _ => { };

		// Called when the swatch is activated via keyboard (ENTER/SPACE)
		public Action OnKeyboardSelect;

		// Callback for arrow key navigation
		public Func<Keycode, bool> OnArrowKey;

		// Callback when this swatch gains TAB focus (for updating preview)
		public Action OnSwatchFocusGained;

		public string ClickSound = null;

		readonly Ruleset modRules;

		[ObjectCreator.UseCtor]
		public ColorBlockWidget(ModData modData)
		{
			modRules = modData.DefaultRules;
			GetColor = () => Color;

			// Not focusable by default; only enabled in ColorPickerLogic for palette swatches
			IsFocusable = false;
		}

		protected ColorBlockWidget(ColorBlockWidget widget)
			: base(widget)
		{
			modRules = widget.modRules;
			GetColor = widget.GetColor;
			ClickSound = widget.ClickSound;
			OnKeyboardSelect = widget.OnKeyboardSelect;
			OnArrowKey = widget.OnArrowKey;
			OnSwatchFocusGained = widget.OnSwatchFocusGained;
		}

		public override ColorBlockWidget Clone()
		{
			return new ColorBlockWidget(this);
		}

		public override void Draw()
		{
			WidgetUtils.FillRectWithColor(RenderBounds, GetColor());
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return false;

			if (HasMouseFocus && mi.Event == MouseInputEvent.Up)
			{
				// Only fire the onMouseUp event if we successfully lost focus, and were pressed
				OnMouseUp(mi);

				return YieldMouseFocus(mi);
			}

			if (mi.Event == MouseInputEvent.Down)
			{
				// OnMouseDown returns false if the button shouldn't be pressed
				OnMouseDown(mi);

				Game.Sound.PlayNotification(modRules, null, "Sounds", ClickSound, null);
			}

			return false;
		}

		public override bool OnTabFocusActivate(KeyInput e)
		{
			if (OnKeyboardSelect != null)
			{
				Game.Sound.PlayNotification(modRules, null, "Sounds", ClickSound, null);
				OnKeyboardSelect();
				return true;
			}

			// Fallback: trigger the mouse up handler with a dummy input
			if (OnMouseUp != null)
			{
				Game.Sound.PlayNotification(modRules, null, "Sounds", ClickSound, null);
				OnMouseUp(default);
				return true;
			}

			return false;
		}

		public override bool OnTabFocusKeyPress(KeyInput e)
		{
			if (e.Key == Keycode.LEFT || e.Key == Keycode.RIGHT ||
				e.Key == Keycode.UP || e.Key == Keycode.DOWN)
			{
				return OnArrowKey?.Invoke(e.Key) ?? false;
			}

			return false;
		}

		public override void OnTabFocusGained()
		{
			base.OnTabFocusGained();

			// Notify that this swatch has gained focus (for updating preview color)
			OnSwatchFocusGained?.Invoke();
		}
	}
}
