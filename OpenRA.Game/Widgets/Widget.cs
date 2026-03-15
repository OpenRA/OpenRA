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
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	/// <summary>
	/// Implemented by scroll containers that support keyboard-driven scrolling
	/// (PAGE UP, PAGE DOWN, HOME, END) when a child widget has focus.
	/// </summary>
	public interface IKeyboardScrollable
	{
		bool HandleScrollKeyPress(KeyInput e);

		// Scrolls the container so that the given widget is visible.
		void ScrollIntoView(Widget widget);
	}

	public static class Ui
	{
		public const int Timestep = 40;

		public static Widget Root = new ContainerWidget();

		public static TickTime LastTickTime = new(() => Timestep, Game.RunTime);

		static readonly Stack<Widget> WindowList = [];

		public static Widget MouseFocusWidget;
		public static Widget KeyboardFocusWidget;
		public static Widget MouseOverWidget;

		// Currently focused widget for TAB navigation (separate from KeyboardFocusWidget used for text input)
		public static Widget TabFocusWidget;

		// Whether the current tab focus was obtained via keyboard (true) or mouse (false).
		// Focus indicators are only shown when focus was obtained via keyboard.
		public static bool TabFocusFromKeyboard;

		static readonly Mediator Mediator = new();
		static ModData modData;

		public static void Initialize(ModData modData)
		{
			Ui.modData = modData;
		}

		// Handles TAB and SHIFT+TAB navigation between focusable widgets
		public static bool HandleTabNavigation(KeyInput e)
		{
			if (e.Key != Keycode.TAB || e.Event != KeyInputEvent.Down)
				return false;

			var reverse = e.Modifiers.HasModifier(Modifiers.Shift);

			// Determine the scope for TAB navigation:
			// If the current TAB focus widget has a parent that is not part of CurrentWindow,
			// navigate within that parent's tree (e.g., dropdown panels added to Root)
			Widget navigationScope;
			if (TabFocusWidget != null)
			{
				// Find the topmost parent that contains focusable widgets
				navigationScope = FindNavigationScope(TabFocusWidget);
			}
			else
			{
				navigationScope = CurrentWindow() ?? Root;
			}

			// Collect all focusable widgets in tab order
			var focusableWidgets = new List<Widget>();
			CollectFocusableWidgets(navigationScope, focusableWidgets);

			if (focusableWidgets.Count == 0)
				return false;

			// Sort by TabIndex, then by document position (stable order unaffected by scroll).
			focusableWidgets.Sort((a, b) =>
			{
				var tabIndexCompare = a.TabIndex.CompareTo(b.TabIndex);
				if (tabIndexCompare != 0)
					return tabIndexCompare;

				// If same TabIndex, sort by document Y then X (not RenderBounds, which changes with scroll).
				var yCompare = GetDocumentY(a).CompareTo(GetDocumentY(b));
				if (yCompare != 0)
					return yCompare;

				return GetDocumentX(a).CompareTo(GetDocumentX(b));
			});

			// Find current focused widget index
			var currentIndex = -1;
			if (TabFocusWidget != null)
				currentIndex = focusableWidgets.IndexOf(TabFocusWidget);

			// Calculate next index
			int nextIndex;
			if (reverse)
			{
				nextIndex = currentIndex <= 0 ? focusableWidgets.Count - 1 : currentIndex - 1;
			}
			else
			{
				nextIndex = currentIndex >= focusableWidgets.Count - 1 ? 0 : currentIndex + 1;
			}

			var newFocusWidget = focusableWidgets[nextIndex];

			// Allow the current widget to perform cleanup before focus changes
			// (e.g. closing an open dropdown panel).
			var oldFocusWidget = TabFocusWidget;
			oldFocusWidget?.OnBeforeTabNavigation();

			// Set new TabFocusWidget before calling OnTabFocusLost so that
			// OnTabFocusLost can check where focus is going.
			TabFocusWidget = newFocusWidget;
			TabFocusFromKeyboard = true;

			oldFocusWidget?.OnTabFocusLost();

			// Revoke KeyboardFocus from the previous widget if it was a focusable widget
			// (e.g., leaving a text field via TAB should release its keyboard capture)
			if (KeyboardFocusWidget != null && KeyboardFocusWidget != newFocusWidget
				&& KeyboardFocusWidget.IsFocusable)
				KeyboardFocusWidget.YieldKeyboardFocus();

			// Set focus to new widget
			newFocusWidget.OnTabFocusGained();

			// Scroll the new focus widget into view if it is inside a scroll container.
			ScrollTabFocusIntoView(newFocusWidget);

			return true;
		}

		// Handles ENTER and SPACE to activate the currently tab-focused widget
		public static bool HandleTabFocusActivation(KeyInput e)
		{
			if (TabFocusWidget == null || e.Event != KeyInputEvent.Down)
				return false;

			if (e.Key != Keycode.RETURN && e.Key != Keycode.KP_ENTER && e.Key != Keycode.SPACE)
				return false;

			// Ensure the widget is still visible before activating
			// If it has TAB focus, it was navigable when it received focus, so allow activation
			if (!TabFocusWidget.IsVisible())
				return false;

			return TabFocusWidget.OnTabFocusActivate(e);
		}

		// Handles arrow keys and other navigation keys for the currently tab-focused widget
		// This allows widgets like sliders to handle arrow keys even when another widget has keyboard focus
		public static bool HandleTabFocusKeyPress(KeyInput e)
		{
			if (TabFocusWidget == null || e.Event != KeyInputEvent.Down)
				return false;

			// Let the tab-focused widget handle navigation keys (arrows, home, end, etc.)
			if (TabFocusWidget.OnTabFocusKeyPress(e))
				return true;

			// For PAGE UP/DOWN/HOME/END, propagate to parent scroll container if not handled.
			if (e.Key == Keycode.PAGEUP || e.Key == Keycode.PAGEDOWN ||
				e.Key == Keycode.HOME || e.Key == Keycode.END)
			{
				var parent = TabFocusWidget.Parent;
				while (parent != null)
				{
					if (parent is IKeyboardScrollable scrollable && scrollable.HandleScrollKeyPress(e))
						return true;

					parent = parent.Parent;
				}
			}

			return false;
		}

		// Returns the sum of Bounds.Y up the parent chain, giving a stable document Y
		// position that is unaffected by scroll offsets of ancestor scroll containers.
		static int GetDocumentY(Widget widget)
		{
			var y = 0;
			var current = widget;
			while (current != null)
			{
				y += current.Bounds.Y;
				current = current.Parent;
			}

			return y;
		}

		// Returns the sum of Bounds.X up the parent chain, giving a stable document X position.
		static int GetDocumentX(Widget widget)
		{
			var x = 0;
			var current = widget;
			while (current != null)
			{
				x += current.Bounds.X;
				current = current.Parent;
			}

			return x;
		}

		// Scrolls a scroll container ancestor to bring the given widget into view.
		static void ScrollTabFocusIntoView(Widget widget)
		{
			var parent = widget.Parent;
			while (parent != null)
			{
				if (parent is IKeyboardScrollable scrollable)
				{
					scrollable.ScrollIntoView(widget);
					return;
				}

				parent = parent.Parent;
			}
		}

		// Clears the current TAB focus
		public static void ClearTabFocus()
		{
			if (TabFocusWidget != null)
			{
				var old = TabFocusWidget;
				TabFocusWidget = null;
				TabFocusFromKeyboard = false;
				old.OnTabFocusLost();
			}
		}

		// Sets the initial TAB focus to the first focusable widget in a window
		public static void SetInitialFocus(Widget window)
		{
			var focusableWidgets = new List<Widget>();
			CollectFocusableWidgets(window, focusableWidgets);

			if (focusableWidgets.Count == 0)
				return;

			// Sort by TabIndex, then by document position
			focusableWidgets.Sort((a, b) =>
			{
				var tabIndexCompare = a.TabIndex.CompareTo(b.TabIndex);
				if (tabIndexCompare != 0)
					return tabIndexCompare;

				var yCompare = GetDocumentY(a).CompareTo(GetDocumentY(b));
				if (yCompare != 0)
					return yCompare;

				return GetDocumentX(a).CompareTo(GetDocumentX(b));
			});

			var newFocusWidget = focusableWidgets[0];

			// Clear focus from previous widget, but set new TabFocusWidget first
			// so that OnTabFocusLost can check where focus is going
			var oldFocusWidget = TabFocusWidget;
			TabFocusWidget = newFocusWidget;

			oldFocusWidget?.OnTabFocusLost();

			// Set focus to new widget
			newFocusWidget.OnTabFocusGained();
		}

		static void CollectFocusableWidgets(Widget widget, List<Widget> result)
		{
			if (!widget.IsVisible())
				return;

			if (widget.IsTabNavigable())
				result.Add(widget);

			foreach (var child in widget.Children)
				CollectFocusableWidgets(child, result);
		}

		// Find the appropriate navigation scope for a widget
		// Returns the topmost container that should be used for TAB navigation
		static Widget FindNavigationScope(Widget widget)
		{
			// Walk up the parent chain to find the navigation scope
			var current = widget;
			while (current.Parent != null)
			{
				// If we reach a child of Root that is not the current window,
				// that child is our navigation scope (e.g., a dropdown panel)
				if (current.Parent == Root)
				{
					var currentWindow = CurrentWindow();
					if (currentWindow != null && current != currentWindow)
						return current;
				}

				current = current.Parent;
			}

			// Default to current window or root
			return CurrentWindow() ?? Root;
		}

		public static void CloseWindow()
		{
			if (WindowList.Count > 0)
			{
				var hidden = WindowList.Pop();
				Root.RemoveChild(hidden);
				if (hidden.LogicObjects != null)
					foreach (var l in hidden.LogicObjects)
						l.BecameHidden();
			}

			if (WindowList.Count > 0)
			{
				var restore = WindowList.Peek();
				Root.AddChild(restore);

				if (restore.LogicObjects != null)
					foreach (var l in restore.LogicObjects)
						l.BecameVisible();
			}
		}

		public static Widget OpenWindow(string id)
		{
			return OpenWindow(id, []);
		}

		public static Widget OpenWindow(string id, WidgetArgs args)
		{
			if (!args.ContainsKey("modData"))
				args = new WidgetArgs(args) { { "modData", modData } };

			var window = Game.ModData.WidgetLoader.LoadWidget(args, Root, id);
			if (WindowList.Count > 0)
				Root.HideChild(WindowList.Peek());
			WindowList.Push(window);

			// Set initial TAB focus to the first focusable widget in the new window
			SetInitialFocus(window);

			return window;
		}

		public static Widget CurrentWindow()
		{
			return WindowList.Count > 0 ? WindowList.Peek() : null;
		}

		public static T LoadWidget<T>(string id, Widget parent, WidgetArgs args) where T : Widget
		{
			if (LoadWidget(id, parent, args) is T widget)
				return widget;

			throw new InvalidOperationException($"Widget {id} is not of type {typeof(T).Name}");
		}

		public static Widget LoadWidget(string id, Widget parent, WidgetArgs args)
		{
			if (!args.ContainsKey("modData"))
				args = new WidgetArgs(args) { { "modData", modData } };

			return Game.ModData.WidgetLoader.LoadWidget(args, parent, id);
		}

		public static void Tick() { Root.TickOuter(); }

		public static void PrepareRenderables() { Root.PrepareRenderablesOuter(); }

		public static void Draw()
		{
			Root.DrawOuter();

			// Draw focus indicator on top of everything, only when focus was obtained via keyboard.
			if (TabFocusWidget != null && TabFocusFromKeyboard && TabFocusWidget.IsVisible())
				DrawFocusIndicator(TabFocusWidget);
		}

		static void DrawFocusIndicator(Widget widget)
		{
			if (!ChromeMetrics.TryGet("TabFocusColor", out Color focusColor))
				focusColor = Color.FromArgb(128, 255, 255, 255);

			if (!ChromeMetrics.TryGet("TabFocusWidth", out int focusWidth))
				focusWidth = 2;

			var rect = widget.GetFocusIndicatorBounds();
			var l = rect.Left - focusWidth;
			var t = rect.Top - focusWidth;
			var r = rect.Right;
			var b = rect.Bottom;
			var cr = Game.Renderer.RgbaColorRenderer;

			// Top border.
			cr.FillRect(new float2(l, t), new float2(r + focusWidth, t + focusWidth), focusColor);

			// Bottom border.
			cr.FillRect(new float2(l, b), new float2(r + focusWidth, b + focusWidth), focusColor);

			// Left border.
			cr.FillRect(new float2(l, t), new float2(l + focusWidth, b + focusWidth), focusColor);

			// Right border.
			cr.FillRect(new float2(r, t), new float2(r + focusWidth, b + focusWidth), focusColor);
		}

		public static bool HandleInput(MouseInput mi)
		{
			var wasMouseOver = MouseOverWidget;

			if (mi.Event == MouseInputEvent.Move)
				MouseOverWidget = null;

			var handled = false;
			if (MouseFocusWidget != null && MouseFocusWidget.HandleMouseInputOuter(mi))
				handled = true;

			if (!handled && Root.HandleMouseInputOuter(mi))
				handled = true;

			if (mi.Event == MouseInputEvent.Move)
			{
				Viewport.LastMousePos = mi.Location;
				Viewport.LastMoveRunTime = Game.RunTime;
			}

			if (wasMouseOver != MouseOverWidget)
			{
				wasMouseOver?.MouseExited();

				MouseOverWidget?.MouseEntered();
			}

			return handled;
		}

		/// <summary>Possibly handle keyboard input (if this widget has keyboard focus).</summary>
		/// <returns><c>true</c>, if keyboard input was handled, <c>false</c> if the input should bubble to the parent widget.</returns>
		/// <param name="e">Key input data.</param>
		public static bool HandleKeyPress(KeyInput e)
		{
			// The widget with KeyboardFocus has absolute priority
			// (allows tab autocomplete in text fields, etc.)
			if (KeyboardFocusWidget != null && KeyboardFocusWidget.HandleKeyPressOuter(e))
				return true;

			// Handle TAB navigation
			if (e.Key == Keycode.TAB && HandleTabNavigation(e))
				return true;

			// Handle ENTER/SPACE activation of tab-focused widget
			if (TabFocusWidget != null && HandleTabFocusActivation(e))
				return true;

			// Handle arrow keys and other navigation for tab-focused widget (e.g., sliders)
			if (TabFocusWidget != null && HandleTabFocusKeyPress(e))
				return true;

			if (KeyboardFocusWidget != null)
				return KeyboardFocusWidget.HandleKeyPressOuter(e);

			return Root.HandleKeyPressOuter(e);
		}

		public static bool HandleTextInput(string text)
		{
			if (KeyboardFocusWidget != null)
				return KeyboardFocusWidget.HandleTextInputOuter(text);

			return Root.HandleTextInputOuter(text);
		}

		public static void ResetAll()
		{
			ClearTabFocus();
			Root.RemoveChildren();

			while (WindowList.Count > 0)
				CloseWindow();
		}

		public static void ResetTooltips()
		{
			// Issue a no-op mouse move to force any tooltips to be recalculated
			HandleInput(new MouseInput(MouseInputEvent.Move, MouseButton.None,
				Viewport.LastMousePos, int2.Zero, Modifiers.None, 0));
		}

		public static void Subscribe<T>(T instance)
		{
			Mediator.Subscribe(instance);
		}

		public static void Unsubscribe<T>(T instance)
		{
			Mediator.Unsubscribe(instance);
		}

		public static void Send<T>(T notification) => Mediator.Send(notification);
	}

	public class ChromeLogic : IDisposable
	{
		public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
		public virtual void Tick() { }
		public virtual void BecameHidden() { }
		public virtual void BecameVisible() { }
		protected virtual void Dispose(bool disposing) { }
	}

	public struct WidgetBounds(int x, int y, int width, int height)
	{
		public int X = x, Y = y, Width = width, Height = height;
		public readonly int Left => X;
		public readonly int Right => X + Width;
		public readonly int Top => Y;
		public readonly int Bottom => Y + Height;

		public readonly Rectangle ToRectangle()
		{
			return new Rectangle(X, Y, Width, Height);
		}
	}

	public abstract class Widget
	{
		string defaultCursor = null;

		public readonly List<Widget> Children = [];

		// Info defined in YAML
		public string Id = null;
		public IntegerExpression X;
		public IntegerExpression Y;
		public IntegerExpression Width;
		public IntegerExpression Height;
		public ImmutableArray<string> Logic = [];
		public ImmutableArray<ChromeLogic> LogicObjects { get; private set; }
		public bool Visible = true;
		public bool IgnoreMouseOver;
		public bool IgnoreChildMouseOver;

		// TAB navigation properties
		// TabIndex determines the order of focus when pressing TAB (lower values come first)
		public int TabIndex = 0;

		// Whether this widget can receive TAB focus
		public bool IsFocusable = false;

		// Whether this widget currently has TAB focus (derived from Ui.TabFocusWidget)
		public bool HasTabFocus => Ui.TabFocusWidget == this;

		// Calculated internally
		public WidgetBounds Bounds;
		public Widget Parent = null;
		public Func<bool> IsVisible;

		protected Widget() { IsVisible = () => Visible; }

		protected Widget(Widget widget)
		{
			Id = widget.Id;
			X = widget.X;
			Y = widget.Y;
			Width = widget.Width;
			Height = widget.Height;
			Logic = widget.Logic;
			Visible = widget.Visible;

			Bounds = widget.Bounds;
			Parent = widget.Parent;

			IsVisible = widget.IsVisible;
			IgnoreChildMouseOver = widget.IgnoreChildMouseOver;
			IgnoreMouseOver = widget.IgnoreMouseOver;

			// Copy TAB navigation properties
			TabIndex = widget.TabIndex;
			IsFocusable = widget.IsFocusable;

			defaultCursor = widget.defaultCursor;

			foreach (var child in widget.Children)
				AddChild(child.Clone());
		}

		public virtual Widget Clone()
		{
			throw new InvalidOperationException($"Widget type `{GetType().Name}` is not cloneable.");
		}

		public virtual int2 RenderOrigin
		{
			get
			{
				var offset = (Parent == null) ? int2.Zero : Parent.ChildOrigin;
				return new int2(Bounds.X, Bounds.Y) + offset;
			}
		}

		public virtual int2 ChildOrigin => RenderOrigin;

		public virtual Rectangle RenderBounds
		{
			get
			{
				var ro = RenderOrigin;
				return new Rectangle(ro.X, ro.Y, Bounds.Width, Bounds.Height);
			}
		}

		public virtual void Initialize(WidgetArgs args)
		{
			defaultCursor = ChromeMetrics.Get<string>("DefaultCursor");

			// Parse the YAML equations to find the widget bounds
			var parentBounds = (Parent == null)
				? new WidgetBounds(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height)
				: Parent.Bounds;

			var substitutions = args.TryGetValue("substitutions", out var subs) ?
				new Dictionary<string, int>((Dictionary<string, int>)subs) :
				[];

			substitutions.Add("WINDOW_WIDTH", Game.Renderer.Resolution.Width);
			substitutions.Add("WINDOW_HEIGHT", Game.Renderer.Resolution.Height);
			substitutions.Add("PARENT_WIDTH", parentBounds.Width);
			substitutions.Add("PARENT_HEIGHT", parentBounds.Height);

			var readOnlySubstitutions = new ReadOnlyDictionary<string, int>(substitutions);
			var width = Width?.Evaluate(readOnlySubstitutions) ?? 0;
			var height = Height?.Evaluate(readOnlySubstitutions) ?? 0;

			substitutions.Add("WIDTH", width);
			substitutions.Add("HEIGHT", height);

			var x = X?.Evaluate(readOnlySubstitutions) ?? 0;
			var y = Y?.Evaluate(readOnlySubstitutions) ?? 0;
			Bounds = new WidgetBounds(x, y, width, height);
		}

		public void PostInit(WidgetArgs args)
		{
			if (Logic.Length == 0)
				return;

			args["widget"] = this;

			LogicObjects = Logic.Select(l => Game.ModData.ObjectCreator.CreateObject<ChromeLogic>(l, args))
				.ToImmutableArray();

			foreach (var logicObject in LogicObjects)
				Ui.Subscribe(logicObject);

			args.Remove("widget");
		}

		public virtual Rectangle EventBounds => RenderBounds;

		public virtual bool EventBoundsContains(int2 location)
		{
			// PERF: Avoid LINQ.
			if (EventBounds.Contains(location))
				return true;

			foreach (var child in Children)
				if (child.IsVisible() && child.EventBoundsContains(location))
					return true;

			return false;
		}

		public bool HasMouseFocus => Ui.MouseFocusWidget == this;
		public bool HasKeyboardFocus => Ui.KeyboardFocusWidget == this;

		public virtual bool TakeMouseFocus(MouseInput mi)
		{
			if (HasMouseFocus)
				return true;

			if (Ui.MouseFocusWidget != null && !Ui.MouseFocusWidget.YieldMouseFocus(mi))
				return false;

			Ui.MouseFocusWidget = this;
			return true;
		}

		// Remove focus from this widget; return false to hint that you don't want to give it up
		public virtual bool YieldMouseFocus(MouseInput mi)
		{
			if (Ui.MouseFocusWidget == this)
				Ui.MouseFocusWidget = null;

			return true;
		}

		void ForceYieldMouseFocus()
		{
			if (Ui.MouseFocusWidget == this && !YieldMouseFocus(default))
				Ui.MouseFocusWidget = null;
		}

		public virtual bool TakeKeyboardFocus()
		{
			if (HasKeyboardFocus)
				return true;

			if (Ui.KeyboardFocusWidget != null && !Ui.KeyboardFocusWidget.YieldKeyboardFocus())
				return false;

			Ui.KeyboardFocusWidget = this;
			return true;
		}

		public virtual bool YieldKeyboardFocus()
		{
			if (Ui.KeyboardFocusWidget == this)
				Ui.KeyboardFocusWidget = null;

			return true;
		}

		void ForceYieldKeyboardFocus()
		{
			if (Ui.KeyboardFocusWidget == this && !YieldKeyboardFocus())
				Ui.KeyboardFocusWidget = null;
		}

		public virtual string GetCursor(int2 pos) { return defaultCursor; }
		public string GetCursorOuter(int2 pos)
		{
			// Is the cursor on top of us?
			if (!(IsVisible() && EventBoundsContains(pos)))
				return null;

			// Do any of our children specify a cursor?
			// PERF: Avoid LINQ.
			for (var i = Children.Count - 1; i >= 0; --i)
			{
				var cc = Children[i].GetCursorOuter(pos);
				if (cc != null)
					return cc;
			}

			return EventBounds.Contains(pos) ? GetCursor(pos) : null;
		}

		public virtual void MouseEntered() { }
		public virtual void MouseExited() { }

		// TAB navigation virtual methods
		// Called when this widget gains TAB focus
		public virtual void OnTabFocusGained() { }

		// Called when this widget loses TAB focus
		public virtual void OnTabFocusLost() { }

		// Called before TAB navigation moves focus away from this widget.
		// Unlike OnTabFocusLost, this is called before TabFocusWidget is changed,
		// allowing cleanup (e.g. closing dropdowns) without interfering with navigation.
		public virtual void OnBeforeTabNavigation() { }

		// Called when ENTER or SPACE is pressed while this widget has TAB focus
		// Returns true if the activation was handled
		public virtual bool OnTabFocusActivate(KeyInput e) { return false; }

		// Called when arrow keys or other navigation keys are pressed while this widget has TAB focus
		// Override to handle keys like LEFT/RIGHT for sliders, etc.
		// Returns true if the key was handled
		public virtual bool OnTabFocusKeyPress(KeyInput e) { return false; }

		// Returns true if this widget can currently receive TAB focus
		// Override to add additional conditions (e.g., not disabled)
		public virtual bool IsTabNavigable() { return IsFocusable && IsVisible(); }

		// Returns the bounds used for drawing the focus indicator.
		// Override to customize (e.g., ColorBlockWidget uses inner bounds).
		public virtual Rectangle GetFocusIndicatorBounds() { return RenderBounds; }

		/// <summary>Possibly handles mouse input (click, drag, scroll, etc).</summary>
		/// <returns><c>true</c>, if mouse input was handled, <c>false</c> if the input should bubble to the parent widget.</returns>
		/// <param name="mi">Mouse input data.</param>
		public virtual bool HandleMouseInput(MouseInput mi) { return false; }

		public bool HandleMouseInputOuter(MouseInput mi)
		{
			// Are we able to handle this event?
			if (!(HasMouseFocus || (IsVisible() && EventBoundsContains(mi.Location))))
				return false;

			var oldMouseOver = Ui.MouseOverWidget;

			// Send the event to the deepest children first and bubble up if unhandled
			// PERF: Avoid LINQ.
			for (var i = Children.Count - 1; i >= 0; --i)
				if (Children[i].HandleMouseInputOuter(mi))
					return true;

			if (IgnoreChildMouseOver)
				Ui.MouseOverWidget = oldMouseOver;

			if (mi.Event == MouseInputEvent.Move && Ui.MouseOverWidget == null && !IgnoreMouseOver)
				Ui.MouseOverWidget = this;

			return HandleMouseInput(mi);
		}

		public virtual bool HandleKeyPress(KeyInput e) { return false; }

		public virtual bool HandleKeyPressOuter(KeyInput e)
		{
			if (!IsVisible())
				return false;

			// Can any of our children handle this?
			// PERF: Avoid LINQ.
			for (var i = Children.Count - 1; i >= 0; --i)
				if (Children[i].HandleKeyPressOuter(e))
					return true;

			// Do any widgety behavior
			var handled = HandleKeyPress(e);

			return handled;
		}

		public virtual bool HandleTextInput(string text) { return false; }

		public virtual bool HandleTextInputOuter(string text)
		{
			if (!IsVisible())
				return false;

			// Can any of our children handle this?
			// PERF: Avoid LINQ.
			for (var i = Children.Count - 1; i >= 0; --i)
				if (Children[i].HandleTextInputOuter(text))
					return true;

			// Do any widgety behavior (enter text etc)
			var handled = HandleTextInput(text);

			return handled;
		}

		public virtual void PrepareRenderables() { }

		public virtual void PrepareRenderablesOuter()
		{
			if (IsVisible())
			{
				PrepareRenderables();
				foreach (var child in Children)
					child.PrepareRenderablesOuter();
			}
		}

		public virtual void Draw() { }

		public virtual void DrawOuter()
		{
			if (IsVisible())
			{
				Draw();
				foreach (var child in Children)
					child.DrawOuter();
			}
		}

		public virtual void Tick() { }

		public virtual void TickOuter()
		{
			if (IsVisible())
			{
				Tick();
				foreach (var child in Children)
					child.TickOuter();

				if (LogicObjects != null)
					foreach (var l in LogicObjects)
						l.Tick();
			}
		}

		public virtual void AddChild(Widget child)
		{
			child.Parent = this;
			Children.Add(child);
		}

		public virtual void RemoveChild(Widget child)
		{
			if (child != null)
			{
				Children.Remove(child);
				child.Removed();
			}
		}

		public virtual void HideChild(Widget child)
		{
			if (child != null)
			{
				Children.Remove(child);
				child.Hidden();
			}
		}

		public virtual void RemoveChildren()
		{
			foreach (var child in Children)
				child?.Removed();

			Children.Clear();
		}

		public virtual void Hidden()
		{
			// Using the forced versions because the widgets
			// have been removed
			ForceYieldKeyboardFocus();
			ForceYieldMouseFocus();

			// Clear TAB focus if this widget had it
			if (Ui.TabFocusWidget == this)
				Ui.ClearTabFocus();

			// PERF: Avoid LINQ.
			for (var i = Children.Count - 1; i >= 0; --i)
				Children[i].Hidden();
		}

		public virtual void Removed()
		{
			// Using the forced versions because the widgets
			// have been removed
			ForceYieldKeyboardFocus();
			ForceYieldMouseFocus();

			// Clear TAB focus if this widget had it
			if (Ui.TabFocusWidget == this)
				Ui.ClearTabFocus();

			// PERF: Avoid LINQ.
			for (var i = Children.Count - 1; i >= 0; --i)
				Children[i].Removed();

			if (LogicObjects != null)
			{
				foreach (var l in LogicObjects)
				{
					Ui.Unsubscribe(l);
					l.Dispose();
				}
			}
		}

		public Widget GetOrNull(string id)
		{
			if (Id == id)
				return this;

			foreach (var child in Children)
			{
				var w = child.GetOrNull(id);
				if (w != null)
					return w;
			}

			return null;
		}

		public T GetOrNull<T>(string id) where T : Widget
		{
			return (T)GetOrNull(id);
		}

		public T Get<T>(string id) where T : Widget
		{
			var t = GetOrNull<T>(id);
			if (t == null)
				throw new InvalidOperationException($"Widget {Id} has no child {id} of type {typeof(T).Name}");
			return t;
		}

		public Widget Get(string id) { return Get<Widget>(id); }
	}

	public class ContainerWidget : Widget
	{
		public readonly bool ClickThrough = true;

		public ContainerWidget() { IgnoreMouseOver = true; }
		public ContainerWidget(ContainerWidget other)
			: base(other)
		{
			ClickThrough = other.ClickThrough;
			IgnoreMouseOver = true;
		}

		public override string GetCursor(int2 pos) { return null; }
		public override ContainerWidget Clone() { return new ContainerWidget(this); }

		public override bool HandleMouseInput(MouseInput mi)
		{
			return !ClickThrough && EventBounds.Contains(mi.Location);
		}
	}

	public class InputWidget : Widget
	{
		public bool Disabled = false;
		public Func<bool> IsDisabled = () => false;

		public InputWidget()
		{
			IsDisabled = () => Disabled;

			// InputWidgets are focusable by default for TAB navigation
			IsFocusable = true;
		}

		public InputWidget(InputWidget other)
			: base(other)
		{
			IsDisabled = () => other.Disabled;

			// InputWidgets are focusable by default for TAB navigation
			IsFocusable = true;
		}

		public override InputWidget Clone() { return new InputWidget(this); }

		// InputWidgets are not tab navigable when disabled
		public override bool IsTabNavigable()
		{
			return base.IsTabNavigable() && !IsDisabled();
		}
	}

	public class WidgetArgs : Dictionary<string, object>
	{
		public WidgetArgs() { }
		public WidgetArgs(Dictionary<string, object> args)
			: base(args) { }
		public void Add(string key, Action val) { base.Add(key, val); }
	}

	public sealed class Mediator
	{
		readonly TypeDictionary types = [];

		public void Subscribe<T>(T instance)
		{
			types.Add(instance);
		}

		public void Unsubscribe<T>(T instance)
		{
			types.Remove(instance);
		}

		public void Send<T>(T notification)
		{
			var handlers = types.WithInterface<INotificationHandler<T>>();

			foreach (var handler in handlers)
				handler.Handle(notification);
		}
	}

	public interface INotificationHandler<T>
	{
		void Handle(T notification);
	}
}
