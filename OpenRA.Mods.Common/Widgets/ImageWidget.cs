#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ImageWidget : Widget
	{
		public readonly string TooltipTemplate;
		public readonly string TooltipContainer;

		public string ImageCollection = "";
		public string ImageName = "";
		public bool ClickThrough = true;
		public Func<string> GetImageName;
		public Func<string> GetImageCollection;

		public string TooltipText;

		Lazy<TooltipContainerWidget> tooltipContainer;
		public Func<string> GetTooltipText;

		public ImageWidget()
		{
			GetImageName = () => ImageName;
			GetImageCollection = () => ImageCollection;
			GetTooltipText = () => TooltipText;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		protected ImageWidget(ImageWidget other)
			: base(other)
		{
			ClickThrough = other.ClickThrough;
			ImageName = other.ImageName;
			GetImageName = other.GetImageName;
			ImageCollection = other.ImageCollection;
			GetImageCollection = other.GetImageCollection;

			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;
			GetTooltipText = other.GetTooltipText;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
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

		public override bool HandleMouseInput(MouseInput mi)
		{
			return !ClickThrough && RenderBounds.Contains(mi.Location);
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null || GetTooltipText == null)
				return;

			tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", GetTooltipText } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}
	}
}
