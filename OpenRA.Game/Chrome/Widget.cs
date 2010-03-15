using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets.Delegates;

namespace OpenRA.Widgets
{
	public class Widget
	{
		// Typically defined in YAML
		public readonly string Id = null;
		public readonly int X = 0;
		public readonly int Y = 0;
		public readonly int Width = 0;
		public readonly int Height = 0;
		public readonly string Delegate = null;

		public bool Visible = true;
		public readonly List<Widget> Children = new List<Widget>();

		// Calculated internally
		public Rectangle Bounds {get {return new Rectangle(X,Y,Width, Height);}}
		public Rectangle ClickRect;
		
		public virtual void Initialize()
		{
			// Create the clickrect
			ClickRect = Bounds;	
			foreach (var child in Children)
				ClickRect = Rectangle.Union(ClickRect, child.Bounds);

		}
		
		public virtual bool HandleInput(MouseInput mi)
		{
			if (!Visible)
				return false;
			
			// Do any of our children handle this?
			bool caught = false;
			if (ClickRect.Contains(mi.Location.X,mi.Location.Y))
			{
				foreach (var child in Children)
				{
					caught = child.HandleInput(mi);
					if (caught)
						break;
				}
			}
			
			// Child has handled the event
			if (caught)
				return true;
						
			// Mousedown
			if (Delegate != null && mi.Event == MouseInputEvent.Down && ClickRect.Contains(mi.Location.X,mi.Location.Y))
			{
				foreach (var mod in Game.ModAssemblies)
				{
					var act = (IWidgetDelegate)mod.First.CreateInstance(mod.Second + "."+Delegate);
					if (act == null) continue;
					
					return act.OnClick(this, mi);
				}
				throw new InvalidOperationException("Cannot locate widget delegate: {0}".F(Delegate));
			}
			
			return false;
		}
		
		public virtual void Draw()
		{
			if (Visible)
				foreach (var child in Children)
					child.Draw();
		}
		
		public void AddChild(Widget child)
		{
			Children.Add( child );
		}
		
		public Widget GetWidget(string id)
		{
			if (this.Id == id)
				return this;
			
			foreach (var child in Children)
				if (child.GetWidget(id) != null)
					return child;
			
			return null;
		}
		
	}
	class ContainerWidget : Widget {	}
}