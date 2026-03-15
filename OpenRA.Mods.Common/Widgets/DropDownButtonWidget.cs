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
		Widget previousKeyboardFocusWidget;
		Widget ownerWindow;
		CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getMarkerImage;
		CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getSeparatorImage;

		public override bool HandleKeyPress(KeyInput e)
		{
			if (HasKeyboardFocus && e.Event == KeyInputEvent.Down && e.Key == Keycode.ESCAPE)
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

		/// <summary>
		/// Invoked after the dropdown panel has been removed.
		/// </summary>
		public Action DropDownClosed;

		public bool IsDropDownOpen => panel != null;

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
			var isHover = Ui.MouseOverWidget == this || Children.Any(c => c == Ui.MouseOverWidget) || HasTabFocus;

			getMarkerImage ??= WidgetUtils.GetCachedStatefulImage(Decorations, DecorationMarker);

			var arrowImage = getMarkerImage.Update((isDisabled, Depressed, isHover, false, IsHighlighted()));
			WidgetUtils.DrawSprite(
				arrowImage,
				stateOffset + new float2(
					rb.Right - (int)((rb.Height + arrowImage.Size.X) / 2),
					rb.Top + (int)((rb.Height - arrowImage.Size.Y) / 2)));

			getSeparatorImage ??= WidgetUtils.GetCachedStatefulImage(Separators, SeparatorImage);

			var separatorImage = getSeparatorImage.Update((isDisabled, Depressed, isHover, false, IsHighlighted()));
			if (separatorImage != null)
				WidgetUtils.DrawSprite(
					separatorImage,
					stateOffset + new float2(-3, 0) + new float2(rb.Right - rb.Height + 4,
					rb.Top + (int)((rb.Height - separatorImage.Size.Y) / 2)));
		}

		public override DropDownButtonWidget Clone() { return new DropDownButtonWidget(this); }

		// This is crap
		public override int UsableWidth => Bounds.Width - Bounds.Height; /* space for button */

		// TAB focus activation: open or close the dropdown panel
		public override bool OnTabFocusActivate(KeyInput e)
		{
			// If the dropdown is already open, close it (toggle behavior)
			if (IsDropDownOpen)
			{
				RemovePanel();
				return true;
			}

			// Use base implementation which handles both OnMouseDown and OnKeyPress/OnClick
			// This supports dropdowns using OnMouseDown (most dropdowns) and OnClick (e.g., Battlefield News)
			return base.OnTabFocusActivate(e);
		}

		// Close the dropdown panel when TAB focus is lost,
		// unless the new focus is within the panel itself
		public override void OnTabFocusLost()
		{
			if (!IsDropDownOpen)
				return;

			// Don't close if the new TAB focus is within the dropdown panel
			if (Ui.TabFocusWidget != null && IsWidgetWithinPanel(Ui.TabFocusWidget, panel))
				return;

			RemovePanel();
		}

		public override void Hidden()
		{
			base.Hidden();
			CloseDropdownUnlessInKeyboardNavigation();
		}

		public override void Removed()
		{
			base.Removed();
			CloseDropdownUnlessInKeyboardNavigation();
		}

		static bool IsWidgetWithinPanel(Widget widget, Widget panel)
		{
			for (var w = widget; w != null; w = w.Parent)
				if (ReferenceEquals(w, panel))
					return true;

			return false;
		}

		void CloseDropdownUnlessInKeyboardNavigation()
		{
			if (panel == null)
				return;

			// Always close if the dropdown belongs to a window that is no longer current.
			if (ownerWindow != null && !ReferenceEquals(ownerWindow, Ui.CurrentWindow()))
			{
				RemovePanel();
				return;
			}

			// Keep the panel open if the user is actively navigating it with the keyboard.
			// This prevents dropdowns from closing when the source widget is replaced by a UI refresh (e.g. selecting bots in the lobby).
			if (IsWidgetWithinPanel(Ui.KeyboardFocusWidget, panel))
				return;

			RemovePanel();
		}

		public void RemovePanel()
		{
			if (panel == null)
				return;

			panelRoot.RemoveChild(fullscreenMask);
			panelRoot.RemoveChild(panel);
			panel = fullscreenMask = null;
			ownerWindow = null;

			var focusToRestore = previousKeyboardFocusWidget;
			previousKeyboardFocusWidget = null;
			if (focusToRestore != null && focusToRestore.IsVisible())
				focusToRestore.TakeKeyboardFocus();

			// Restore TAB focus to this dropdown button
			if (IsVisible() && IsFocusable)
			{
				if (Ui.TabFocusWidget != null && Ui.TabFocusWidget != this)
					Ui.TabFocusWidget.OnTabFocusLost();

				Ui.TabFocusWidget = this;
				OnTabFocusGained();
			}

			YieldKeyboardFocus();
			Ui.ResetTooltips();
			DropDownClosed?.Invoke();
		}

		public void AttachPanel(Widget p) { AttachPanel(p, null); }
		public void AttachPanel(Widget p, Action onCancel)
		{
			if (panel != null)
				throw new InvalidOperationException("Attempted to attach a panel to an open dropdown");
			panel = p;
			previousKeyboardFocusWidget = Ui.KeyboardFocusWidget;
			TakeKeyboardFocus();

			// Mask to prevent any clicks from being sent to other widgets
			fullscreenMask = new MaskWidget
			{
				Bounds = new WidgetBounds(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height)
			};

			fullscreenMask.OnMouseDown += mi => { Game.Sound.PlayNotification(ModRules, null, "Sounds", ClickSound, null); RemovePanel(); };
			fullscreenMask.OnEscapeKey = () => { RemovePanel(); onCancel?.Invoke(); return true; };
			if (onCancel != null)
				fullscreenMask.OnMouseDown += _ => onCancel();

			panelRoot = PanelRoot == null ? Ui.Root : Ui.Root.Get(PanelRoot);

			ownerWindow = Ui.CurrentWindow();

			panelRoot.AddChild(fullscreenMask);

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

			if (panel is ScrollPanelWidget scrollPanel)
			{
				scrollPanel.OnEscapeKey = () => { RemovePanel(); onCancel?.Invoke(); return true; };
				scrollPanel.TakeKeyboardFocus();

				// Set TAB focus to the first focusable widget in the panel
				// This enables keyboard navigation for panels with checkboxes or other focusable widgets
				Ui.SetInitialFocus(panel);
			}
			else
			{
				// For non-scroll panels (like color picker), give focus to the mask
				// so it can handle ESC to close the dropdown.
				fullscreenMask.TakeKeyboardFocus();

				// Set TAB focus to the first focusable widget in the panel
				Ui.SetInitialFocus(panel);
			}
		}

		public void ShowDropDown<T>(
			string panelTemplate, int maxHeight, IEnumerable<T> options, Func<T, ScrollItemWidget, ScrollItemWidget> setupItem)
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

				var onKeyboardSelect = item.OnKeyboardSelect;
				item.OnKeyboardSelect = () => { onKeyboardSelect?.Invoke(); RemovePanel(); };

				panel.AddChild(item);
			}

			panel.Bounds.Height = Math.Min(maxHeight, panel.ContentHeight);
			AttachPanel(panel);
		}

		public void ShowDropDown<T>(
			string panelTemplate, int height, Dictionary<string, IEnumerable<T>> groups, Func<T, ScrollItemWidget, ScrollItemWidget> setupItem)
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
					// Headers are visual separators and should not be part of keyboard navigation.
					var header = ScrollItemWidget.SetupHeader(headerTemplate);
					header.Get<LabelWidget>("LABEL").GetText = () => group;
					panel.AddChild(header);
				}

				foreach (var option in kv.Value)
				{
					var o = option;

					var item = setupItem(o, itemTemplate);
					var onClick = item.OnClick;
					item.OnClick = () => { onClick(); RemovePanel(); };

					var onKeyboardSelect = item.OnKeyboardSelect;
					item.OnKeyboardSelect = () => { onKeyboardSelect?.Invoke(); RemovePanel(); };

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
		public Func<bool> OnEscapeKey;
		public MaskWidget() { }
		public MaskWidget(MaskWidget other)
			: base(other)
		{
			OnMouseDown = other.OnMouseDown;
			OnEscapeKey = other.OnEscapeKey;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				return false;

			if (mi.Event == MouseInputEvent.Down)
				OnMouseDown(mi);

			return true;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down && e.Key == Keycode.ESCAPE && OnEscapeKey != null)
				return OnEscapeKey();

			return false;
		}

		public override string GetCursor(int2 pos) { return null; }
		public override MaskWidget Clone() { return new MaskWidget(this); }
	}
}
