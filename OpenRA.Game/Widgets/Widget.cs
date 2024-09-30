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
using System.Collections.ObjectModel;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	public abstract class Widget
	{
		string defaultCursor = null;

		public readonly List<Widget> Children = new();

		// Info defined in YAML
		public string Id = null;
		public IntegerExpression X;
		public IntegerExpression Y;
		public IntegerExpression Width;
		public IntegerExpression Height;
		public string[] Logic = Array.Empty<string>();
		public ChromeLogic[] LogicObjects { get; private set; }
		public bool Visible = true;
		public bool IgnoreMouseOver;
		public bool IgnoreChildMouseOver;

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
				new Dictionary<string, int>();

			substitutions.Add("WINDOW_RIGHT", Game.Renderer.Resolution.Width);
			substitutions.Add("WINDOW_BOTTOM", Game.Renderer.Resolution.Height);
			substitutions.Add("PARENT_RIGHT", parentBounds.Width);
			substitutions.Add("PARENT_BOTTOM", parentBounds.Height);

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
				.ToArray();

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
}
