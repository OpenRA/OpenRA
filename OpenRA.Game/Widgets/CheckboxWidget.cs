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
	public class CheckboxWidget : Widget
	{
		public string Text = "";
		public int baseLine = 1;
		public bool Bold = false;
		public Func<bool> IsChecked = () => false;
		public event Action<bool> OnChange = _ => {};
		
		object boundObject;
		bool boundReadOnly;
		FieldInfo boundField;
		
		public override void DrawInner()
		{
			var font = Bold ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var pos = RenderOrigin;
			var rect = RenderBounds;
			var check = new Rectangle(rect.Location,
					new Size(Bounds.Height, Bounds.Height));
			WidgetUtils.DrawPanel("dialog3", check);

			var textSize = font.Measure(Text);
			font.DrawText(Text,
				new float2(rect.Left + rect.Height * 1.5f, 
					pos.Y - baseLine + (Bounds.Height - textSize.Y)/2), Color.White);

			if ((boundObject != null && (bool)boundField.GetValue(boundObject)) || IsChecked())
				WidgetUtils.DrawRGBA(
					ChromeProvider.GetImage("checkbox", "checked"),
					new float2(rect.Left + 2, rect.Top + 2));
		}
		
		public void Bind(object obj, string field) { Bind(obj, field, false); }
		public void BindReadOnly(object obj, string field) { Bind(obj, field, true); }

		void Bind(object obj, string field, bool readOnly)
		{
			boundObject = obj;
			boundReadOnly = readOnly;
			boundField = obj.GetType().GetField(field);
		}

		// TODO: CheckboxWidget doesn't support raising events for mouse input
		public override bool HandleMouseInput(MouseInput mi)
		{
			// Checkboxes require lmb
			if (mi.Button != MouseButton.Left || mi.Event != MouseInputEvent.Down)
				return false;
			
			bool newVal = !IsChecked();
			if (boundObject != null && !boundReadOnly)
			{
				newVal = !(bool)boundField.GetValue(boundObject);
				boundField.SetValue(boundObject, newVal);
			}

			OnChange(newVal);
			return true;
		}

		public CheckboxWidget() : base() { }

		protected CheckboxWidget(CheckboxWidget other)
			: base(other)
		{
			Text = other.Text;
		}

		public override Widget Clone() { return new CheckboxWidget(this); }
	}
}