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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class DropDownButtonWidget : ButtonWidget
	{
		public readonly string Decorations = "dropdown-decorations";
		public readonly string DecorationMarker = "marker";
		public readonly string Separators = "dropdown-separators";
		public readonly string SeparatorImage = "separator";
		public readonly TextAlign PanelAlign = TextAlign.Left;
		public string PanelRoot;

		Widget panel;
		MaskWidget fullscreenMask;
		Widget panelRoot;
		CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getMarkerImage;
		CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getSeparatorImage;

		[ObjectCreator.UseCtor]
		public DropDownButtonWidget(ModData modData)
			: base(modData) { }

		protected DropDownButtonWidget(DropDownButtonWidget widget)
			: base(widget)
		{
			PanelRoot = widget.PanelRoot;
			Decorations = widget.Decorations;
			DecorationMarker = widget.DecorationMarker;
			Separators = widget.Separators;
			SeparatorImage = widget.SeparatorImage;
		}

		public override void Draw()
		{
			base.Draw();
			var stateOffset = Depressed ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);

			var rb = RenderBounds;
			var isDisabled = IsDisabled();
			var isHover = Ui.MouseOverWidget == this || Children.Any(c => c == Ui.MouseOverWidget);

			if (getMarkerImage == null)
				getMarkerImage = WidgetUtils.GetCachedStatefulImage(Decorations, DecorationMarker);

			var arrowImage = getMarkerImage.Update((isDisabled, Depressed, isHover, false, IsHighlighted()));
			WidgetUtils.DrawSprite(arrowImage, stateOffset + new float2(rb.Right - (int)((rb.Height + arrowImage.Size.X) / 2), rb.Top + (int)((rb.Height - arrowImage.Size.Y) / 2)));

			if (getSeparatorImage == null)
				getSeparatorImage = WidgetUtils.GetCachedStatefulImage(Separators, SeparatorImage);

			var separatorImage = getSeparatorImage.Update((isDisabled, Depressed, isHover, false, IsHighlighted()));
			if (separatorImage != null)
				WidgetUtils.DrawSprite(separatorImage, stateOffset + new float2(-3, 0) + new float2(rb.Right - rb.Height + 4, rb.Top + (int)((rb.Height - separatorImage.Size.Y) / 2)));
		}

		public override Widget Clone() { return new DropDownButtonWidget(this); }

		// This is crap
		public override int UsableWidth => Bounds.Width - Bounds.Height; /* space for button */

		public override void Hidden()
		{
			base.Hidden();
			RemovePanel();
		}

		public override void Removed()
		{
			base.Removed();
			RemovePanel();
		}

		public void RemovePanel()
		{
			if (panel == null)
				return;

			panelRoot.RemoveChild(fullscreenMask);
			panelRoot.RemoveChild(panel);
			panel = fullscreenMask = null;

			Ui.ResetTooltips();
		}

		public void AttachPanel(Widget p) { AttachPanel(p, null); }
		public void AttachPanel(Widget p, Action onCancel)
		{
			if (panel != null)
				throw new InvalidOperationException("Attempted to attach a panel to an open dropdown");
			panel = p;

			// Mask to prevent any clicks from being sent to other widgets
			fullscreenMask = new MaskWidget
			{
				Bounds = new Rectangle(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height)
			};

			fullscreenMask.OnMouseDown += mi => { Game.Sound.PlayNotification(ModRules, null, "Sounds", ClickSound, null); RemovePanel(); };
			if (onCancel != null)
				fullscreenMask.OnMouseDown += _ => onCancel();

			panelRoot = PanelRoot == null ? Ui.Root : Ui.Root.Get(PanelRoot);

			panelRoot.AddChild(fullscreenMask);

			var oldBounds = panel.Bounds;
			var panelX = RenderOrigin.X - panelRoot.RenderOrigin.X;
			if (PanelAlign == TextAlign.Right)
				panelX += Bounds.Width - oldBounds.Width;
			else if (PanelAlign == TextAlign.Center)
				panelX += (Bounds.Width - oldBounds.Width) / 2;

			var panelY = RenderOrigin.Y + Bounds.Height - panelRoot.RenderOrigin.Y;
			if (panelY + oldBounds.Height > Game.Renderer.Resolution.Height)
				panelY -= (Bounds.Height + oldBounds.Height);

			panel.Bounds = new Rectangle(
				panelX,
				panelY,
				oldBounds.Width,
				oldBounds.Height);
			panelRoot.AddChild(panel);

			(panel as ScrollPanelWidget)?.ScrollToSelectedItem();
		}

		public void ShowDropDown<T>(string panelTemplate, int maxHeight, IEnumerable<T> options, Func<T, ScrollItemWidget, ScrollItemWidget> setupItem)
		{
			var substitutions = new Dictionary<string, int>() { { "DROPDOWN_WIDTH", Bounds.Width } };
			var panel = (ScrollPanelWidget)Ui.LoadWidget(panelTemplate, null, new WidgetArgs() { { "substitutions", substitutions } });

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
			var substitutions = new Dictionary<string, int>() { { "DROPDOWN_WIDTH", Bounds.Width } };
			var panel = (ScrollPanelWidget)Ui.LoadWidget(panelTemplate, null, new WidgetArgs() { { "substitutions", substitutions } });

			var headerTemplate = panel.GetOrNull<ScrollItemWidget>("HEADER");
			var itemTemplate = panel.Get<ScrollItemWidget>("TEMPLATE");
			panel.RemoveChildren();

			foreach (var kv in groups)
			{
				var group = kv.Key;
				if (group.Length > 0 && headerTemplate != null)
				{
					var header = ScrollItemWidget.Setup(headerTemplate, () => false, () => { });
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
		public event Action<MouseInput> OnMouseDown = _ => { };
		public MaskWidget() { }
		public MaskWidget(MaskWidget other)
			: base(other)
		{
			OnMouseDown = other.OnMouseDown;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				return false;

			if (mi.Event == MouseInputEvent.Down)
				OnMouseDown(mi);

			return true;
		}

		public override string GetCursor(int2 pos) { return null; }
		public override Widget Clone() { return new MaskWidget(this); }
	}
}
