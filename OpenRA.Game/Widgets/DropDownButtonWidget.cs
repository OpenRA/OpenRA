#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class DropDownButtonWidget : ButtonWidget
	{
		Widget panel;
		MaskWidget fullscreenMask;

		[ObjectCreator.UseCtor]
		public DropDownButtonWidget(Ruleset modRules)
			: base(modRules) { }

		protected DropDownButtonWidget(DropDownButtonWidget widget)	: base(widget) { }

		public override void Draw()
		{
			base.Draw();
			var stateOffset = (Depressed) ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);

			var image = ChromeProvider.GetImage("scrollbar", IsDisabled() ? "down_pressed" : "down_arrow");
			var rb = RenderBounds;
			var color = GetColor();
			var colordisabled = GetColorDisabled();

			WidgetUtils.DrawRGBA( image,
				stateOffset + new float2( rb.Right - rb.Height + 4,
					rb.Top + (rb.Height - image.bounds.Height) / 2 ));

			WidgetUtils.FillRectWithColor(new Rectangle(stateOffset.X + rb.Right - rb.Height,
				stateOffset.Y + rb.Top + 3, 1, rb.Height - 6),
				IsDisabled() ? colordisabled : color);
		}

		public override Widget Clone() { return new DropDownButtonWidget(this); }

		// This is crap
		public override int UsableWidth { get { return Bounds.Width - Bounds.Height; } } /* space for button */

		public override void Removed()
		{
			base.Removed();
			RemovePanel();
		}

		public void RemovePanel()
		{
			if (!IsDisabled())
			{
				if (panel == null)
					return;

				Ui.Root.RemoveChild(fullscreenMask);
				Ui.Root.RemoveChild(panel);
				panel = fullscreenMask = null;
				Sound.PlayNotification(ModRules, null, "Sounds", "ClickSound", null);
			}
			else
				Sound.PlayNotification(ModRules, null, "Sounds", "ClickDisabledSound", null);
		}

		public void AttachPanel(Widget p) { AttachPanel(p, null); }
		public void AttachPanel(Widget p, Action onCancel)
		{
			if (panel != null)
				throw new InvalidOperationException("Attempted to attach a panel to an open dropdown");
			panel = p;

			// Mask to prevent any clicks from being sent to other widgets
			fullscreenMask = new MaskWidget();
			fullscreenMask.Bounds = new Rectangle(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
			fullscreenMask.OnMouseDown += mi => RemovePanel();
			if (onCancel != null)
				fullscreenMask.OnMouseDown += _ => onCancel();

			Ui.Root.AddChild(fullscreenMask);

			var oldBounds = panel.Bounds;
			panel.Bounds = new Rectangle(RenderOrigin.X, RenderOrigin.Y + Bounds.Height, oldBounds.Width, oldBounds.Height);
			Ui.Root.AddChild(panel);
		}

		public void ShowDropDown<T>(string panelTemplate, int maxHeight, IEnumerable<T> options, Func<T, ScrollItemWidget, ScrollItemWidget> setupItem)
		{
			var substitutions = new Dictionary<string,int>() {{ "DROPDOWN_WIDTH", Bounds.Width }};
			var panel = (ScrollPanelWidget)Ui.LoadWidget(panelTemplate, null, new WidgetArgs()
				{{ "substitutions", substitutions }});

			var itemTemplate = panel.Get<ScrollItemWidget>("TEMPLATE");
			panel.RemoveChildren();
			foreach (var option in options)
			{
				var o = option;

				var item = setupItem(o, itemTemplate);
				var onClick = item.OnClick;
				item.OnClick = () => { onClick(); RemovePanel(); };

				panel.AddChild(item);
			}

			panel.Bounds.Height = Math.Min(maxHeight, panel.ContentHeight);
			AttachPanel(panel);
		}

		public void ShowDropDown<T>(string panelTemplate, int height, Dictionary<string, IEnumerable<T>> groups, Func<T, ScrollItemWidget, ScrollItemWidget> setupItem)
		{
			var substitutions = new Dictionary<string,int>() {{ "DROPDOWN_WIDTH", Bounds.Width }};
			var panel = (ScrollPanelWidget)Ui.LoadWidget(panelTemplate, null, new WidgetArgs()
			                                             {{ "substitutions", substitutions }});

			var headerTemplate = panel.Get<ScrollItemWidget>("HEADER");
			var itemTemplate = panel.Get<ScrollItemWidget>("TEMPLATE");
			panel.RemoveChildren();

			foreach (var kv in groups)
			{
				var group = kv.Key;
				if (group.Length > 0)
				{
					var header = ScrollItemWidget.Setup(headerTemplate, () => true, () => {});
					header.Get<LabelWidget>("LABEL").GetText = () => group;
					panel.AddChild(header);
				}

				foreach (var option in kv.Value)
				{
					var o = option;

					var item = setupItem(o, itemTemplate);
					var onClick = item.OnClick;
					item.OnClick = () => { onClick(); RemovePanel(); };

					panel.AddChild(item);
				}
			}
			panel.Bounds.Height = Math.Min(height, panel.ContentHeight);
			AttachPanel(panel);
		}
	}

	public class MaskWidget : Widget
	{
		public event Action<MouseInput> OnMouseDown = _ => {};
		public MaskWidget() { }
		public MaskWidget(MaskWidget other)
			: base(other)
		{
			OnMouseDown = other.OnMouseDown;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event != MouseInputEvent.Down && mi.Event != MouseInputEvent.Up)
				return false;

			if (mi.Event == MouseInputEvent.Down)
				OnMouseDown(mi);

			return true;
		}

		public override string GetCursor(int2 pos) { return null; }
		public override Widget Clone() { return new MaskWidget(this); }
	}
}