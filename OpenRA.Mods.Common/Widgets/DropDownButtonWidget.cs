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
using OpenRA.Mods.Common.Orders;
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
		bool allowWorldClicks;
		CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getMarkerImage;
		CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getSeparatorImage;

		public Func<KeyInput, bool> AdditionalKeyHandler;

		public bool IsPanelOpen => panel != null;

		public override bool HandleKeyPress(KeyInput e)
		{
			if (AdditionalKeyHandler != null && AdditionalKeyHandler(e))
				return true;

			if (panel != null && e.Event == KeyInputEvent.Down && e.Key == Keycode.ESCAPE && e.Modifiers == Modifiers.None)
			{
				RemovePanel();
				return true;
			}

			return base.HandleKeyPress(e);
		}

		public override bool YieldKeyboardFocus()
		{
			RemovePanel();
			return base.YieldKeyboardFocus();
		}

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
			AdditionalKeyHandler = widget.AdditionalKeyHandler;
		}

		public override void Draw()
		{
			base.Draw();

			if (string.IsNullOrEmpty(Decorations))
				return;

			var stateOffset = Depressed ? new int2(VisualHeight, VisualHeight) : new int2(0, 0);

			var rb = RenderBounds;
			var isDisabled = IsDisabled();
			var isHover = Ui.MouseOverWidget == this || Children.Any(c => c == Ui.MouseOverWidget);

			getMarkerImage ??= WidgetUtils.GetCachedStatefulImage(Decorations, DecorationMarker);

			var arrowImage = getMarkerImage.Update((isDisabled, Depressed, isHover, false, IsHighlighted()));
			WidgetUtils.DrawSprite(
				arrowImage,
				stateOffset + new float2(
					rb.Right - (int)((rb.Height + arrowImage.Size.X) / 2),
					rb.Top + (int)((rb.Height - arrowImage.Size.Y) / 2)));

			if (!string.IsNullOrEmpty(Separators))
			{
				getSeparatorImage ??= WidgetUtils.GetCachedStatefulImage(Separators, SeparatorImage);

				var separatorImage = getSeparatorImage.Update((isDisabled, Depressed, isHover, false, IsHighlighted()));
				if (separatorImage != null)
					WidgetUtils.DrawSprite(
						separatorImage,
						stateOffset + new float2(-3, 0) + new float2(rb.Right - rb.Height + 4,
						rb.Top + (int)((rb.Height - separatorImage.Size.Y) / 2)));
			}
		}

		public override DropDownButtonWidget Clone() { return new DropDownButtonWidget(this); }

		// This is crap
		public override int UsableWidth => Bounds.Width - Bounds.Height; /* space for button */

		public override void Hidden()
		{
			base.Hidden();
			RemovePanel();
		}

		public override void Removed()
		{
			// Do not detach the panel synchronously: Ui.ResetAll() iterates children and
			// RemoveChild during Removed() throws "Collection was modified".
			var p = panel;
			var mask = fullscreenMask;
			var root = panelRoot;
			panel = null;
			fullscreenMask = null;
			panelRoot = null;
			allowWorldClicks = false;

			FormationPreferences.ClearFormationDropdown(this);

			if (p != null && root != null)
			{
				Game.RunAfterTick(() =>
				{
					if (mask != null && mask.Parent == root)
						root.RemoveChild(mask);
					if (p.Parent == root)
						root.RemoveChild(p);
				});
			}

			base.Removed();
		}

		public void RemovePanel()
		{
			if (panel == null)
				return;

			if (fullscreenMask != null)
				panelRoot.RemoveChild(fullscreenMask);
			panelRoot.RemoveChild(panel);
			panel = fullscreenMask = null;
			allowWorldClicks = false;

			YieldKeyboardFocus();
			Ui.ResetTooltips();
		}

		public void AttachPanel(Widget p) { AttachPanel(p, null); }
		public override bool HandleMouseInput(MouseInput mi)
		{
			if (allowWorldClicks && panel != null && mi.Event == MouseInputEvent.Down && !RenderBounds.Contains(mi.Location))
			{
				if (HasMouseFocus)
					YieldMouseFocus(mi);

				return false;
			}

			// Clicking the button while open closes it instead of attaching a second panel.
			if (panel != null && mi.Event == MouseInputEvent.Down && mi.Button == MouseButton.Left && RenderBounds.Contains(mi.Location))
			{
				RemovePanel();
				return true;
			}

			return base.HandleMouseInput(mi);
		}

		public void AttachPanel(Widget p, Action onCancel, bool dismissOnMaskClick = true, bool blockWorldClicks = true)
		{
			if (panel != null)
				RemovePanel();
			panel = p;
			allowWorldClicks = !blockWorldClicks;
			if (blockWorldClicks)
				TakeKeyboardFocus();

			panelRoot = PanelRoot == null ? Ui.Root : Ui.Root.Get(PanelRoot);

			if (blockWorldClicks)
			{
				// Mask to prevent any clicks from being sent to other widgets
				fullscreenMask = new MaskWidget
				{
					Bounds = new WidgetBounds(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height),
					ClickPassThrough = pos => RenderBounds.Contains(pos)
				};

				fullscreenMask.OnMouseDown += mi => { Game.Sound.PlayNotification(ModRules, null, "Sounds", ClickSound, null); };
				if (dismissOnMaskClick)
					fullscreenMask.OnMouseDown += _ => RemovePanel();
				if (onCancel != null)
					fullscreenMask.OnMouseDown += _ => onCancel();

				panelRoot.AddChild(fullscreenMask);
			}
			else
				fullscreenMask = null;

			var oldBounds = panel.Bounds;
			var panelX = RenderOrigin.X - panelRoot.RenderOrigin.X;
			if (PanelAlign == TextAlign.Right)
				panelX += Bounds.Width - oldBounds.Width;
			else if (PanelAlign == TextAlign.Center)
				panelX += (Bounds.Width - oldBounds.Width) / 2;

			var panelY = RenderOrigin.Y + Bounds.Height - panelRoot.RenderOrigin.Y;
			if (panelY + oldBounds.Height > Game.Renderer.Resolution.Height)
				panelY -= Bounds.Height + oldBounds.Height;

			var buttonRightEdge = RenderOrigin.X + Bounds.Width - panelRoot.RenderOrigin.X;
			if (panelX + oldBounds.Width > Game.Renderer.Resolution.Width)
				panelX = buttonRightEdge - oldBounds.Width;

			panel.Bounds = new WidgetBounds(
				panelX,
				panelY,
				oldBounds.Width,
				oldBounds.Height);
			panelRoot.AddChild(panel);

			(panel as ScrollPanelWidget)?.ScrollToSelectedItem();

			// Opening click leaves mouse focus on the button, which would swallow map orders.
			if (allowWorldClicks)
				YieldMouseFocus(default);
		}

		public void ShowDropDown<T>(
			string panelTemplate, int maxHeight, IEnumerable<T> options, Func<T, ScrollItemWidget, ScrollItemWidget> setupItem,
			bool closeOnSelect = true, bool dismissOnMaskClick = true, bool blockWorldClicks = true)
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
				item.OnClick = () => OnDropDownItemClicked(onClick, closeOnSelect, blockWorldClicks);

				panel.AddChild(item);
			}

			panel.Bounds.Height = Math.Min(maxHeight, panel.ContentHeight);
			AttachPanel(panel, null, dismissOnMaskClick, blockWorldClicks);
		}

		public void ShowDropDown<T>(
			string panelTemplate, int height, Dictionary<string, IEnumerable<T>> groups, Func<T, ScrollItemWidget, ScrollItemWidget> setupItem,
			bool closeOnSelect = true, bool dismissOnMaskClick = true, bool blockWorldClicks = true)
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
					item.OnClick = () => OnDropDownItemClicked(onClick, closeOnSelect, blockWorldClicks);

					panel.AddChild(item);
				}
			}

			panel.Bounds.Height = Math.Min(height, panel.ContentHeight);
			AttachPanel(panel, null, dismissOnMaskClick, blockWorldClicks);
		}

		void OnDropDownItemClicked(Action onClick, bool closeOnSelect, bool blockWorldClicks)
		{
			onClick();
			if (closeOnSelect)
				RemovePanel();
			else if (!blockWorldClicks)
				YieldMouseFocus(default);
		}
	}

	public class MaskWidget : Widget
	{
		public event Action<MouseInput> OnMouseDown = _ => { };
		public Func<int2, bool> ClickPassThrough = _ => false;

		public MaskWidget() { }
		public MaskWidget(MaskWidget other)
			: base(other)
		{
			OnMouseDown = other.OnMouseDown;
			ClickPassThrough = other.ClickPassThrough;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				return false;

			if (mi.Event == MouseInputEvent.Down && ClickPassThrough(mi.Location))
				return false;

			if (mi.Event == MouseInputEvent.Down)
				OnMouseDown(mi);

			return true;
		}

		public override string GetCursor(int2 pos) { return null; }
		public override MaskWidget Clone() { return new MaskWidget(this); }
	}
}
