#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ColorPaletteSwatchWidget : BackgroundWidget
    {
		public Func<Color> GetColor;

        public Action<MouseInput> OnMouseDown = _ => { };
        public Action<MouseInput> OnMouseUp = _ => { };

        public Action OnClick = () => { };

        //protected bool CtrlPressed = false;

        public ColorPaletteSwatchWidget()
		{
			GetColor = () => Color.White;
            OnMouseUp = _ => OnClick();
        }

        public override bool HandleKeyPress(KeyInput e)
        {
            //if (e.Key == Keycode.LCTRL)
            //CtrlPressed = e.Modifiers.HasModifier(Modifiers.Ctrl);

            return false;
        }

        protected ColorPaletteSwatchWidget(ColorPaletteSwatchWidget widget)
			: base(widget)
		{
			GetColor = widget.GetColor;
            OnMouseUp = _ => OnClick();
        }

		public override Widget Clone()
		{
			return new ColorPaletteSwatchWidget(this);
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
                //Depressed = true;
                //Game.Sound.PlayNotification(ModRules, null, "Sounds", "ClickSound", null);
            }

            //return Depressed;
            return false;
        }

    }
}
