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
using OpenRA.Graphics;
using System.Reflection;

namespace OpenRA.Widgets
{
	public class CheckboxWidget : ButtonWidget
	{
		public string CheckType = "checked";
		public Color TextColor = Color.White;
		public Color ColorDisabled = Color.Gray;
		public Func<Color> GetColor;
		public Func<Color> GetColorDisabled;
		public Func<string> GetCheckType;
		public Func<bool> IsChecked = () => false;
		public int BaseLine = 1;
		public int CheckOffset = 2;
		public bool HasPressedState = ChromeMetrics.Get<bool>("CheckboxPressedState");

		public CheckboxWidget()
			: base()
		{
			GetCheckType = () => CheckType;
			GetColor = () => TextColor;
			GetColorDisabled = () => ColorDisabled;
		}

		protected CheckboxWidget(CheckboxWidget other)
			: base(other)
		{
			GetColor = other.GetColor;
			GetColorDisabled = other.GetColorDisabled;
			TextColor = other.TextColor;
			ColorDisabled = other.ColorDisabled;
			CheckType = other.CheckType;
			GetCheckType = other.GetCheckType;
			IsChecked = other.IsChecked;
			BaseLine = other.BaseLine;
			CheckOffset = other.CheckOffset;
			HasPressedState = other.HasPressedState;
		}

		public override void Draw()
		{
			var disabled = IsDisabled();
			var font = Game.Renderer.Fonts[Font];
			var color = GetColor();
			var colordisabled = GetColorDisabled();
			var rect = RenderBounds;
			var check = new Rectangle(rect.Location, new Size(Bounds.Height, Bounds.Height));
			var state = disabled ? "checkbox-disabled" :
						Depressed && HasPressedState ? "checkbox-pressed" :
						Ui.MouseOverWidget == this ? "checkbox-hover" :
						"checkbox";

			WidgetUtils.DrawPanel(state, check);

			var textSize = font.Measure(Text);
			font.DrawText(Text,
				new float2(rect.Left + rect.Height * 1.5f, RenderOrigin.Y - BaseLine + (Bounds.Height - textSize.Y)/2),
				disabled ? colordisabled : color);

			if (IsChecked() || (Depressed && HasPressedState && !disabled))
			{
				var checkType = GetCheckType();
				if (HasPressedState && (Depressed || disabled))
					checkType += "-disabled";

				var offset = new float2(rect.Left + CheckOffset, rect.Top + CheckOffset);
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("checkbox-bits", checkType), offset);
			}
		}

		public override Widget Clone() { return new CheckboxWidget(this); }
	}
}
