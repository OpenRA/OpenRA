#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
		public Func<bool> IsChecked = () => false;
		public int BaseLine = 1;
		
		object boundObject;
		bool boundReadOnly;
		FieldInfo boundField;
		[Obsolete] public event Action<bool> OnChange = _ => {};
		
		public CheckboxWidget()
			: base()
		{
			OnClick = OldClickBehavior;
			IsChecked = () => (boundObject != null && (bool)boundField.GetValue(boundObject));
		}
		
		protected CheckboxWidget(CheckboxWidget widget)
			: base(widget)
		{
			OnClick = OldClickBehavior;
			IsChecked = () => (boundObject != null && (bool)boundField.GetValue(boundObject));
		}
		
		[Obsolete] public void Bind(object obj, string field) { Bind(obj, field, false); }
		[Obsolete] public void BindReadOnly(object obj, string field) { Bind(obj, field, true); }

		void Bind(object obj, string field, bool readOnly)
		{
			boundObject = obj;
			boundReadOnly = readOnly;
			boundField = obj.GetType().GetField(field);
		}
		
		void OldClickBehavior()
		{
			bool newVal = !IsChecked();
			if (boundObject != null && !boundReadOnly)
			{
				newVal = !(bool)boundField.GetValue(boundObject);
				boundField.SetValue(boundObject, newVal);
			}

			OnChange(newVal);
		}
		
		public override void DrawInner()
		{
			var font = Game.Renderer.Fonts[Font];
			var rect = RenderBounds;
			var check = new Rectangle(rect.Location, new Size(Bounds.Height, Bounds.Height));
			var state = IsDisabled() ? "checkbox-disabled" : 
						Depressed ? "checkbox-pressed" : 
						RenderBounds.Contains(Viewport.LastMousePos) ? "checkbox-hover" : 
						"checkbox";
			
			WidgetUtils.DrawPanel(state, check);
			
			var textSize = font.Measure(Text);
			font.DrawText(Text,
				new float2(rect.Left + rect.Height * 1.5f, RenderOrigin.Y - BaseLine + (Bounds.Height - textSize.Y)/2), Color.White);

			if (IsChecked() || Depressed)
				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage("checkbox-bits", Depressed ? "pressed" : "checked"),
					new float2(rect.Left + 2, rect.Top + 2));
		}

		public override Widget Clone() { return new CheckboxWidget(this); }
	}
}