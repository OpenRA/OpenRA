#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public abstract class Widget
	{
		// Info defined in YAML
		public string Id = null;
		public string X = "0";
		public string Y = "0";
		public string Width = "0";
		public string Height = "0";
		public string Delegate = null;
		public string EventHandler = null;
		public bool ClickThrough = true;
		public bool Visible = true;
		public readonly List<Widget> Children = new List<Widget>();

		// Calculated internally
		public Rectangle Bounds;
		public Widget Parent = null;

		static List<string> Delegates = new List<string>();
		public static Stack<Widget> WindowList = new Stack<Widget>();
		
		// Common Funcs that most widgets will want
		public Action<object> SpecialOneArg = (arg) => {};
		public Func<MouseInput, bool> OnMouseDown = mi => false;
		public Func<MouseInput, bool> OnMouseUp = mi => false;
		public Func<MouseInput, bool> OnMouseMove = mi => false;
		public Func<KeyInput, bool> OnKeyPress = e => false;

		public Func<bool> IsVisible;

		public Widget() { IsVisible = () => Visible; }

		public static Widget RootWidget
		{
			get { return rootWidget; }
			set { rootWidget = value; }
		}
		private static Widget rootWidget = new ContainerWidget();
		
		public Widget(Widget widget)
		{	
			Id = widget.Id;
			X = widget.X;
		 	Y = widget.Y;
		 	Width = widget.Width;
			Height = widget.Height;
		 	Delegate = widget.Delegate;
		 	ClickThrough = widget.ClickThrough;
		 	Visible = widget.Visible;
			
			Bounds = widget.Bounds;
			Parent = widget.Parent;
			
			OnMouseDown = widget.OnMouseDown;
			OnMouseUp = widget.OnMouseUp;
			OnMouseMove = widget.OnMouseMove;
			OnKeyPress = widget.OnKeyPress;

			IsVisible = widget.IsVisible;
			
			foreach(var child in widget.Children)
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

		public virtual int2 ChildOrigin	{ get { return RenderOrigin; } }
		public virtual Rectangle RenderBounds { get { return new Rectangle(RenderOrigin.X, RenderOrigin.Y, Bounds.Width, Bounds.Height); } }

		public virtual void Initialize()
		{
			// Parse the YAML equations to find the widget bounds
			var parentBounds = (Parent == null)
				? new Rectangle(0, 0, Game.viewport.Width, Game.viewport.Height)
				: Parent.Bounds;

			var substitutions = new Dictionary<string, int>();
			substitutions.Add("WINDOW_RIGHT", Game.viewport.Width);
			substitutions.Add("WINDOW_BOTTOM", Game.viewport.Height);
			substitutions.Add("PARENT_RIGHT", parentBounds.Width);
			substitutions.Add("PARENT_LEFT", parentBounds.Left);
			substitutions.Add("PARENT_TOP", parentBounds.Top);
			substitutions.Add("PARENT_BOTTOM", parentBounds.Height);
			int width = Evaluator.Evaluate(Width, substitutions);
			int height = Evaluator.Evaluate(Height, substitutions);

			substitutions.Add("WIDTH", width);
			substitutions.Add("HEIGHT", height);

			Bounds = new Rectangle(Evaluator.Evaluate(X, substitutions),
								   Evaluator.Evaluate(Y, substitutions),
								   width,
								   height);
		}

		public void PostInit( Dictionary<string, object> args )
		{
			if( Delegate != null )
			{
				args[ "widget" ] = this;
				Game.modData.ObjectCreator.CreateObject<IWidgetDelegate>( Delegate, args );
				args.Remove( "widget" );
			}
		}
		
		public void InitDelegates()
		{
			foreach(var d in Delegates)
				Game.CreateObject<IWidgetDelegate>(d);
		}
		
		public virtual Rectangle EventBounds { get { return RenderBounds; } }
		public virtual Rectangle GetEventBounds()
		{
			return Children
				.Where(c => c.IsVisible())
				.Select(c => c.GetEventBounds())
				.Aggregate(EventBounds, Rectangle.Union);
		}
		
		public static Widget SelectedWidget;
		public bool Focused { get { return SelectedWidget == this; } }
		public virtual bool TakeFocus(MouseInput mi)
		{
			if (Focused)
				return true;
			
			if (SelectedWidget != null && !SelectedWidget.LoseFocus(mi))
				return false;
			SelectedWidget = this;
			return true;
		}
		
		// Remove focus from this widget; return false if you don't want to give it up
		public virtual bool LoseFocus(MouseInput mi)
		{
			// Some widgets may need to override focus depending on mouse click
			return LoseFocus();
		}
		
		public virtual bool LoseFocus()
		{
			if (SelectedWidget == this)
				SelectedWidget = null;
			
			return true;
		}
		
		public virtual string GetCursor(int2 pos) { return "default"; }
		public string GetCursorOuter(int2 pos)
		{
			// Is the cursor on top of us?
			if (!(IsVisible() && GetEventBounds().Contains(pos.ToPoint())))
				return null;
			
			// Do any of our children specify a cursor?
			foreach (var child in Children.OfType<Widget>().Reverse())
			{
				var cc = child.GetCursorOuter(pos);
				if (cc != null)
					return cc;
			}

			return EventBounds.Contains(pos.ToPoint()) ? GetCursor(pos) : null;
		}
		
		public virtual bool HandleInputInner(MouseInput mi) { return !ClickThrough; }
		
		public static bool HandleInput(MouseInput mi)
		{
			bool handled = false;
			if (SelectedWidget != null && SelectedWidget.HandleMouseInputOuter(mi))
				handled = true;
			
			if (!handled && RootWidget.HandleMouseInputOuter(mi))
				handled = true;

			if (mi.Event == MouseInputEvent.Move)
			{
				Viewport.LastMousePos = mi.Location;
				Viewport.TicksSinceLastMove = 0;
			}
			return handled;
		}
		
		public bool HandleMouseInputOuter(MouseInput mi)
		{
			// Are we able to handle this event?
			if (!(Focused || (IsVisible() && GetEventBounds().Contains(mi.Location.X,mi.Location.Y))))
				return false;
			
			// Send the event to the deepest children first and bubble up if unhandled
			foreach (var child in Children.OfType<Widget>().Reverse())
				if (child.HandleMouseInputOuter(mi))
					return true;

			// Do any widgety behavior (button click etc)
			// Return false if it can't handle any user actions
			if (!HandleInputInner(mi))
				return false;
			
			// Apply any special logic added by delegates; they return true if they caught the input
			if (mi.Event == MouseInputEvent.Down && OnMouseDown(mi)) return true;
			if (mi.Event == MouseInputEvent.Up && OnMouseUp(mi)) return true;
			if (mi.Event == MouseInputEvent.Move && OnMouseMove(mi)) return true;
			
			return true;
		}
				
		
		public virtual bool HandleKeyPressInner(KeyInput e) { return false; }
		public virtual bool HandleKeyPressOuter(KeyInput e)
		{			
			if (!IsVisible())
				return false;
			
			// Can any of our children handle this?
			foreach (var child in Children)
				if (child.HandleKeyPressOuter(e))
					return true;

			// Do any widgety behavior (enter text etc)
			var handled = HandleKeyPressInner(e);
			
			// Apply any special logic added by delegates; they return true if they caught the input
			if (OnKeyPress(e)) return true;
			
			return handled;
		}
		
		public static bool HandleKeyPress(KeyInput e)
		{
			if (SelectedWidget != null)
				return SelectedWidget.HandleKeyPressOuter(e);
			
			if (RootWidget.HandleKeyPressOuter(e))
				return true;
			return false;
		}
		
		public abstract void DrawInner();
		
		public virtual void Draw()
		{
			if (IsVisible())
			{
				DrawInner();
				foreach (var child in Children)
					child.Draw();
			}
		}
		
		public virtual void Tick(World world)
		{
			if (IsVisible())
				foreach (var child in Children)
					child.Tick(world);
		}
		
		public void AddChild(Widget child)
		{
			child.Parent = this;
			Children.Add( child );
		}
		
		public Widget GetWidget(string id)
		{
			if (this.Id == id)
				return this;
			
			foreach (var child in Children)
			{
				var w = child.GetWidget(id);
				if (w != null)
					return w;
			}
			return null;
		}

		public T GetWidget<T>(string id) where T : Widget
		{
			var widget = GetWidget(id);
			return (widget != null)? (T) widget : null;
		}
		
		public static void CloseWindow()
		{
			RootWidget.Children.Remove( WindowList.Pop() );
			if( WindowList.Count > 0 )
				rootWidget.Children.Add( WindowList.Peek() );
		}

		public static Widget OpenWindow( string id )
		{
			return OpenWindow( id, new Dictionary<string, object>() );
		}

		public static Widget OpenWindow(string id, Dictionary<string, object> args )
		{
			var window = Game.modData.WidgetLoader.LoadWidget( args, rootWidget, id );
			if( WindowList.Count > 0 )
				rootWidget.Children.Remove( WindowList.Peek() );
			WindowList.Push( window );
			return window;
		}
		
		public static void DoTick(World world)
		{
			RootWidget.Tick(world);
			
			if (!world.GameHasStarted) return;
			if (world.LocalPlayer == null) return;
			++Viewport.TicksSinceLastMove;
		}
		
		public static void DoDraw()
		{
			RootWidget.Draw();
		}
	}

	class ContainerWidget : Widget {
		public ContainerWidget() : base() { }

		public ContainerWidget(Widget other) : base(other) { }

		public override void DrawInner() { }
		
		public override string GetCursor(int2 pos) { return null; }
		public override Widget Clone() { return new ContainerWidget(this); }
	}
	public interface IWidgetDelegate { }
}