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
		public string ClickSound = null;

		readonly Ruleset modRules;

		[ObjectCreator.UseCtor]
		public ColorBlockWidget(ModData modData)
		{
			modRules = modData.DefaultRules;
			GetColor = () => Color;
		}

		protected ColorBlockWidget(ColorBlockWidget widget)
			: base(widget)
		{
			modRules = widget.modRules;
			GetColor = widget.GetColor;
			ClickSound = widget.ClickSound;
		}

		public override Widget Clone()
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
	}
}
