using System.Collections.Generic;
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
		public readonly List<Widget> Children = new List<Widget>();
		
		public virtual void Draw(SpriteRenderer rgbaRenderer, Renderer renderer)
		{
			foreach (var child in Children)
				child.Draw(rgbaRenderer, renderer);
		}
	}
	class ContainerWidget : Widget {	}
}