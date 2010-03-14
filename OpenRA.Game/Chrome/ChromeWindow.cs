using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using System.Drawing;

namespace OpenRA
{
	class Widget
	{
		public readonly string Id = null;
		public readonly int X = 0;
		public readonly int Y = 0;
		public readonly int Width = 0;
		public readonly int Height = 0;
		public readonly List<Widget> Children = new List<Widget>();
		
		public void Draw(SpriteRenderer rgbaRenderer, Renderer renderer)
		{
			string collection = "dialog";
			
			Rectangle r = new Rectangle(X,Y,Width,Height);
			renderer.Device.EnableScissor(r.Left, r.Top, r.Width, r.Height);
			
			string[] images = { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background" };
			var ss = Graphics.Util.MakeArray(9, n => ChromeProvider.GetImage(renderer, collection,images[n]));
			
			for( var x = r.Left + (int)ss[2].size.X; x < r.Right - (int)ss[3].size.X; x += (int)ss[8].size.X )
				for( var y = r.Top + (int)ss[0].size.Y; y < r.Bottom - (int)ss[1].size.Y; y += (int)ss[8].size.Y )
					rgbaRenderer.DrawSprite(ss[8], new float2(x, y), "chrome");

			//draw borders
			for (var y = r.Top + (int)ss[0].size.Y; y < r.Bottom - (int)ss[1].size.Y; y += (int)ss[2].size.Y)
			{
				rgbaRenderer.DrawSprite(ss[2], new float2(r.Left, y), "chrome");
				rgbaRenderer.DrawSprite(ss[3], new float2(r.Right - ss[3].size.X, y), "chrome");
			}

			for (var x = r.Left + (int)ss[2].size.X; x < r.Right - (int)ss[3].size.X; x += (int)ss[0].size.X)
			{
				rgbaRenderer.DrawSprite(ss[0], new float2(x, r.Top), "chrome");
				rgbaRenderer.DrawSprite(ss[1], new float2(x, r.Bottom - ss[1].size.Y), "chrome");
			}

			rgbaRenderer.DrawSprite(ss[4], new float2(r.Left, r.Top), "chrome");
			rgbaRenderer.DrawSprite(ss[5], new float2(r.Right - ss[5].size.X, r.Top), "chrome");
			rgbaRenderer.DrawSprite(ss[6], new float2(r.Left, r.Bottom - ss[6].size.Y), "chrome");
			rgbaRenderer.DrawSprite(ss[7], new float2(r.Right - ss[7].size.X, r.Bottom - ss[7].size.Y), "chrome");
			rgbaRenderer.Flush();

			renderer.Device.DisableScissor();
		
			foreach (var child in Children)
				child.Draw(rgbaRenderer, renderer);
		}
	}
	
	class ChromeWindow
	{
		public static Widget LoadWidget( MiniYaml node )
		{
			Widget widget = NewWidget( node.Value );
			foreach( var child in node.Nodes )
			{
				if( child.Key == "Children" )
				{
					foreach( var c in child.Value.Nodes )
					{
						// Hack around a bug in MiniYaml
						c.Value.Value = c.Key;
						widget.Children.Add( LoadWidget( c.Value ) );
					}
				}
				else
					FieldLoader.LoadField( widget, child.Key, child.Value );
			}
	
			return widget;
		}
	
		static Widget NewWidget( string widgetType )
		{
			if( widgetType.Contains( "@" ) )
				widgetType = widgetType.Substring( 0, widgetType.IndexOf( "@" ) );
			// TODO: make a particular widget by reflection, or by `switch( widgetType )`
			
			Log.Write("Creating Widget of type {0}",widgetType);
			return new Widget();
		}
	}
}