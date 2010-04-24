#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Widgets.Delegates;

namespace OpenRA.Widgets
{
	public class Widget
	{
		// Info defined in YAML
		public string Id = null;
		public string X = "0";
		public string Y = "0";
		public string Width = "0";
		public string Height = "0";
		public string Delegate = null;
		public bool ClickThrough = false;
		public bool Visible = true;
		public readonly List<Widget> Children = new List<Widget>();

		// Calculated internally
		public Rectangle Bounds;
		public Widget Parent = null;

		static List<string> Delegates = new List<string>();
		public static Stack<string> WindowList = new Stack<string>();
		
		// Common Funcs that most widgets will want
		public Func<MouseInput,bool> OnMouseDown = mi => {return false;};
		public Func<MouseInput,bool> OnMouseUp = mi => {return false;};
		public Func<MouseInput,bool> OnMouseMove = mi => {return false;};
		public Func<bool> IsVisible;

		public Widget() { IsVisible = () => Visible; }
		
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
			IsVisible = widget.IsVisible;
			
			foreach(var child in widget.Children)
				AddChild(child.Clone());
		}
		
		public virtual Widget Clone()
		{
			return new Widget(this);	
		}
		
		public int2 DrawPosition()
		{
			return new int2(Bounds.X, Bounds.Y) + ((Parent == null) ? int2.Zero : Parent.DrawPosition());
		}
		
		public virtual void Initialize()
		{
			// Parse the YAML equations to find the widget bounds
			Rectangle parentBounds = (Parent == null) 
				? new Rectangle(0,0,Game.viewport.Width,Game.viewport.Height) 
				: Parent.Bounds;
			
			Dictionary<string, int> substitutions = new Dictionary<string, int>();
				substitutions.Add("WINDOW_RIGHT", Game.viewport.Width);
				substitutions.Add("WINDOW_BOTTOM", Game.viewport.Height);
				substitutions.Add("PARENT_RIGHT", parentBounds.Width);
				substitutions.Add("PARENT_BOTTOM", parentBounds.Height);
			int width = Evaluator.Evaluate(Width, substitutions);
			int height = Evaluator.Evaluate(Height, substitutions);
					
			substitutions.Add("WIDTH", width);
			substitutions.Add("HEIGHT", height);
			
			Bounds = new Rectangle(Evaluator.Evaluate(X, substitutions),
			                       Evaluator.Evaluate(Y, substitutions),
			                       width,
			                       height);
			
			// Non-static func definitions
			
			if (Delegate != null && !Delegates.Contains(Delegate))
					Delegates.Add(Delegate);
			
			foreach (var child in Children)
				child.Initialize();
		}
		
		public void InitDelegates()
		{
			foreach(var d in Delegates)
				Game.CreateObject<IWidgetDelegate>(d);
		}
		
		public bool HitTest(int2 xy)
		{
			if (!IsVisible()) return false;
			var pos = DrawPosition();
			var rect = new Rectangle(pos.X,  pos.Y, Bounds.Width, Bounds.Height);
			if (rect.Contains(xy.ToPoint()) && !ClickThrough) return true;
			
			return Children.Any(c => c.HitTest(xy));
		}
		
		public Rectangle GetEventBounds()
		{
			var pos = DrawPosition();
			var rect = new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height);
			return Children
				.Where(c => c.Visible)
				.Select(c => c.GetEventBounds())
				.Aggregate(rect, Rectangle.Union);
		}
				
		public virtual bool HandleInput(MouseInput mi)
		{
			// Are we able to handle this event?
			if (!IsVisible() || !GetEventBounds().Contains(mi.Location.X,mi.Location.Y))
				return false;
			
			// Can any of our children handle this?
			foreach (var child in Children)
				if (child.HandleInput(mi))
					return true;

			if (mi.Event == MouseInputEvent.Down) return OnMouseDown(mi);
			if (mi.Event == MouseInputEvent.Up) return OnMouseUp(mi);
			if (mi.Event == MouseInputEvent.Move) return OnMouseMove(mi);

			throw new InvalidOperationException("Impossible");
		}
		
		public virtual void Draw(World world)
		{
			if (IsVisible())
				foreach (var child in Children)
					child.Draw(world);
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
			return (T)GetWidget(id);
		}
		
		public void CloseWindow()
		{
			Chrome.rootWidget.GetWidget(WindowList.Pop()).Visible = false;
			Chrome.rootWidget.GetWidget(WindowList.Peek()).Visible = true;
		}

		public Widget OpenWindow(string id)
		{
			Chrome.rootWidget.GetWidget(WindowList.Peek()).Visible = false;
			WindowList.Push(id);
			var window = Chrome.rootWidget.GetWidget(id);
			window.Visible = true;
			return window;
		}
	}

	class ContainerWidget : Widget { }
	public interface IWidgetDelegate { }
}