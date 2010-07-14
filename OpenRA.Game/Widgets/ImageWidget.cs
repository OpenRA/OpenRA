
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

		public ImageWidget()
			: base()
		{
			GetImageName = () => { return ImageName; };
			GetImageCollection = () => { return ImageCollection; };
		}

		protected ImageWidget(ImageWidget other)
			: base(other)
		{
			ImageName = other.ImageName;
			GetImageName = other.GetImageName;
			ImageCollection = other.ImageCollection;
			GetImageCollection = other.GetImageCollection;
		}

		public override Widget Clone() { return new ImageWidget(this); }

		public override void DrawInner(World world)
		{
			var name = GetImageName();
			var collection = GetImageCollection();
			WidgetUtils.DrawRGBA(
				ChromeProvider.GetImage(Game.chrome.renderer, collection, name), 
				RenderOrigin);
		}
	}
}
