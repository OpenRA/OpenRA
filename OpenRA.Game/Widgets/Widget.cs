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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	public static class Ui
	{
		public static Widget Root = new RootWidget();

		public static long LastTickTime = Game.RunTime;

		static readonly Stack<Widget> WindowList = new Stack<Widget>();

		public static Widget MouseFocusWidget;
		public static Widget KeyboardFocusWidget;
		public static Widget MouseOverWidget;

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
			return OpenWindow(id, new WidgetArgs());
		}

		public static Widget OpenWindow(string id, WidgetArgs args)
		{
			var window = Game.ModData.WidgetLoader.LoadWidget(args, Root, id);
			if (WindowList.Count > 0)
				Root.HideChild(WindowList.Peek());
			WindowList.Push(window);
			return window;
		}

		public static Widget CurrentWindow()
		{
			return WindowList.Count > 0 ? WindowList.Peek() : null;
		}

		public static T LoadWidget<T>(string id, Widget parent, WidgetArgs args) where T : Widget
		{
			var widget = LoadWidget(id, parent, args) as T;
			if (widget == null)
				throw new InvalidOperationException(
					"Widget {0} is not of type {1}".F(id, typeof(T).Name));
			return widget;
		}

		public static Widget LoadWidget(string id, Widget parent, WidgetArgs args)
		{
			return Game.ModData.WidgetLoader.LoadWidget(args, parent, id);
		}

		public static void Tick() { Root.TickOuter(); }

		public static void PrepareRenderables() { Root.PrepareRenderablesOuter(); }

		public static void Draw() { Root.DrawOuter(); }

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
				Viewport.TicksSinceLastMove = 0;
			}

			if (wasMouseOver != MouseOverWidget)
			{
				if (wasMouseOver != null)
					wasMouseOver.MouseExited();

				if (MouseOverWidget != null)
					MouseOverWidget.MouseEntered();
			}

			return handled;
		}

		/// <summary>Possibly handle keyboard input (if this widget has keyboard focus)</summary>
		/// <returns><c>true</c>, if keyboard input was handled, <c>false</c> if the input should bubble to the parent widget</returns>
		/// <param name="e">Key input data</param>
		public static bool HandleKeyPress(KeyInput e)
		{
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
			Root.RemoveChildren();

			while (WindowList.Count > 0)
				CloseWindow();
		}

		public static void ResetTooltips()
		{
			// Issue a no-op mouse move to force any tooltips to be recalculated
			HandleInput(new MouseInput(MouseInputEvent.Move, MouseButton.None, 0,
				Viewport.LastMousePos, Modifiers.None, 0));
		}
	}

	public class ChromeLogic : IDisposable
	{
		public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
		public virtual void Tick() { }
		public virtual void BecameHidden() { }
		public virtual void BecameVisible() { }
		protected virtual void Dispose(bool disposing) { }
	}

	public abstract class Widget
	{
		public readonly List<Widget> Children = new List<Widget>();

		// Info defined in YAML
		public string Id = null;
		public string X = "0";
		public string Y = "0";
		public string Width = "0";
		public string Height = "0";
		public string[] Logic = { };
		public ChromeLogic[] LogicObjects { get; private set; }
		public bool Visible = true;
		public bool IgnoreMouseOver;
		public bool IgnoreChildMouseOver;

		// Calculated internally
		public Rectangle Bounds;
		public Widget Parent = null;
		public Func<bool> IsVisible;
		public Widget() { IsVisible = () => Visible; }

		public Widget(Widget widget)
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

			foreach (var child in widget.Children)
				AddChild(child.Clone());
		}

		public virtual Widget Clone()
		{
			throw new InvalidOperationException("Widget type `{0}` is not cloneable.".F(GetType().Name));
		}

		public virtual int2 RenderOrigin
		{
			get
			{
				var offset = (Parent == null) ? int2.Zero : Parent.ChildOrigin;
				return new int2(Bounds.X, Bounds.Y) + offset;
			}
		}

		public virtual int2 ChildOrigin { get { return RenderOrigin; } }

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
			// Parse the YAML equations to find the widget bounds
			var parentBounds = (Parent == null)
				? new Rectangle(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height)
				: Parent.Bounds;

			var substitutions = args.ContainsKey("substitutions") ?
				new Dictionary<string, int>((Dictionary<string, int>)args["substitutions"]) :
				new Dictionary<string, int>();

			substitutions.Add("WINDOW_RIGHT", Game.Renderer.Resolution.Width);
			substitutions.Add("WINDOW_BOTTOM", Game.Renderer.Resolution.Height);
			substitutions.Add("PARENT_RIGHT", parentBounds.Width);
			substitutions.Add("PARENT_LEFT", parentBounds.Left);
			substitutions.Add("PARENT_TOP", parentBounds.Top);
			substitutions.Add("PARENT_BOTTOM", parentBounds.Height);
			var width = Evaluator.Evaluate(Width, substitutions);
			var height = Evaluator.Evaluate(Height, substitutions);

			substitutions.Add("WIDTH", width);
			substitutions.Add("HEIGHT", height);

			Bounds = new Rectangle(Evaluator.Evaluate(X, substitutions),
								   Evaluator.Evaluate(Y, substitutions),
								   width,
								   height);
		}

		public void PostInit(WidgetArgs args)
		{
			if (!Logic.Any())
				return;

			args["widget"] = this;

			LogicObjects = Logic.Select(l => Game.ModData.ObjectCreator.CreateObject<ChromeLogic>(l, args))
				.ToArray();

			args.Remove("widget");
		}

		public virtual Rectangle EventBounds { get { return RenderBounds; } }

		public virtual Rectangle GetEventBounds()
		{
			// PERF: Avoid LINQ.
			var bounds = EventBounds;
			foreach (var child in Children)
				if (child.IsVisible())
					bounds = Rectangle.Union(bounds, child.GetEventBounds());
			return bounds;
		}

		public bool HasMouseFocus { get { return Ui.MouseFocusWidget == this; } }
		public bool HasKeyboardFocus { get { return Ui.KeyboardFocusWidget == this; } }

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
			if (Ui.MouseFocusWidget == this && !YieldMouseFocus(default(MouseInput)))
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

		public virtual string GetCursor(int2 pos) { return "default"; }
		public string GetCursorOuter(int2 pos)
		{
			// Is the cursor on top of us?
			if (!(IsVisible() && GetEventBounds().Contains(pos)))
				return null;

			// Do any of our children specify a cursor?
			foreach (var child in Children.OfType<Widget>().Reverse())
			{
				var cc = child.GetCursorOuter(pos);
				if (cc != null)
					return cc;
			}

			return EventBounds.Contains(pos) ? GetCursor(pos) : null;
		}

		public virtual void MouseEntered() { }
		public virtual void MouseExited() { }

		/// <summary>Possibly handles mouse input (click, drag, scroll, etc).</summary>
		/// <returns><c>true</c>, if mouse input was handled, <c>false</c> if the input should bubble to the parent widget</returns>
		/// <param name="mi">Mouse input data</param>
		public virtual bool HandleMouseInput(MouseInput mi) { return false; }

		public bool HandleMouseInputOuter(MouseInput mi)
		{
			// Are we able to handle this event?
			if (!(HasMouseFocus || (IsVisible() && GetEventBounds().Contains(mi.Location))))
				return false;

			var oldMouseOver = Ui.MouseOverWidget;

			// Send the event to the deepest children first and bubble up if unhandled
			foreach (var child in Children.OfType<Widget>().Reverse())
				if (child.HandleMouseInputOuter(mi))
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
			foreach (var child in Children.OfType<Widget>().Reverse())
				if (child.HandleKeyPressOuter(e))
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
			foreach (var child in Children.OfType<Widget>().Reverse())
				if (child.HandleTextInputOuter(text))
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
			while (Children.Count > 0)
				RemoveChild(Children[Children.Count - 1]);
		}

		public virtual void Hidden()
		{
			// Using the forced versions because the widgets
			// have been removed
			ForceYieldKeyboardFocus();
			ForceYieldMouseFocus();

			foreach (var c in Children.OfType<Widget>().Reverse())
				c.Hidden();
		}

		public virtual void Removed()
		{
			// Using the forced versions because the widgets
			// have been removed
			ForceYieldKeyboardFocus();
			ForceYieldMouseFocus();

			foreach (var c in Children.OfType<Widget>().Reverse())
				c.Removed();

			if (LogicObjects != null)
				foreach (var l in LogicObjects)
					l.Dispose();
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
				throw new InvalidOperationException(
					"Widget {0} has no child {1} of type {2}".F(
						Id, id, typeof(T).Name));
			return t;
		}

		public Widget Get(string id) { return Get<Widget>(id); }
	}

	public class ContainerWidget : Widget
	{
		public readonly bool ClickThrough = true;

		public ContainerWidget() { IgnoreMouseOver = true; }
		public ContainerWidget(ContainerWidget other)
			: base(other) { IgnoreMouseOver = true; }

		public override string GetCursor(int2 pos) { return null; }
		public override Widget Clone() { return new ContainerWidget(this); }
		public Func<KeyInput, bool> OnKeyPress = _ => false;
		public override bool HandleKeyPress(KeyInput e) { return OnKeyPress(e); }

		public override bool HandleMouseInput(MouseInput mi)
		{
			return !ClickThrough && EventBounds.Contains(mi.Location);
		}
	}

	public class WidgetArgs : Dictionary<string, object>
	{
		public WidgetArgs() { }
		public WidgetArgs(Dictionary<string, object> args) : base(args) { }
		public void Add(string key, Action val) { base.Add(key, val); }
	}
}
