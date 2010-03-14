using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	class Widget
	{
		public readonly string Id = null;
		public readonly int X = 0;
		public readonly int Y = 0;
		public readonly int Width = 0;
		public readonly int Height = 0;
		public bool Visible = true;
		public readonly List<Widget> Children = new List<Widget>();
		public Rectangle Bounds
		{
			get {return new Rectangle(X,Y,Width, Height);}
		}
		
		public Rectangle ClickRect;
		
		public virtual void Initialize()
		{
			// Create the clickrect
			ClickRect = Bounds;	
			foreach (var child in Children)
				ClickRect = Rectangle.Union(ClickRect, child.Bounds);

		}
		
		public virtual void Draw(SpriteRenderer rgbaRenderer, Renderer renderer)
		{
			if (Visible)
				foreach (var child in Children)
					child.Draw(rgbaRenderer, renderer);
		}
		
		public virtual bool HandleInput(MouseInput mi)
		{
			if (!Visible)
				return false;
			
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
			
			return caught;
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