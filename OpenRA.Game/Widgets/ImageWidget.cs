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
			GetImageName = () => ImageName;
			GetImageCollection = () => ImageCollection;
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

		public override void DrawInner()
		{
			var name = GetImageName();
			var collection = GetImageCollection();
			WidgetUtils.DrawRGBA(
				ChromeProvider.GetImage(collection, name), 
				RenderOrigin);
		}
	}
}
