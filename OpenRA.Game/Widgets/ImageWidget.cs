
using System;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class ImageWidget : Widget
	{
		public string ImageCollection = "";
		public string ImageName = "";
		public Func<string> GetImageName;
		public Func<string> GetImageCollection;

		public ImageWidget ()
			: base()
		{
			GetImageName = () => { return ImageName; };
			GetImageCollection = () => { return ImageCollection; };
		}
		
		public ImageWidget(Widget other)
			: base(other)
		{
			ImageName = (other as ImageWidget).ImageName;
			GetImageName = (other as ImageWidget).GetImageName;
			ImageCollection = (other as ImageWidget).ImageCollection;
			GetImageCollection = (other as ImageWidget).GetImageCollection;
		}
		
		public override Widget Clone()
		{	
			return new ImageWidget(this);
		}
		
		public override void DrawInner(World world)
		{		
			var name = GetImageName();
			var collection = GetImageCollection();
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(Game.chrome.renderer, collection, name), RenderOrigin);
		}
	}
}
