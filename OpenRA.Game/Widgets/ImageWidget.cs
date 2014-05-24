#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
		{
			GetImageName = () => ImageName;
			GetImageCollection = () => ImageCollection;
		}

		protected ImageWidget(ImageWidget other)
		{
			CopyOf(this, other);
			ImageName = other.ImageName;
			GetImageName = other.GetImageName;
			ImageCollection = other.ImageCollection;
			GetImageCollection = other.GetImageCollection;
		}

		public override Widget Clone() { return new ImageWidget(this); }

		public override void Draw()
		{
			var name = GetImageName();
			var collection = GetImageCollection();

			var sprite = ChromeProvider.GetImage(collection, name);
			if (sprite == null)
				throw new ArgumentException("Sprite {0}/{1} was not found.".F(collection, name));

			WidgetUtils.DrawRGBA(sprite, RenderOrigin);
		}
	}
}
