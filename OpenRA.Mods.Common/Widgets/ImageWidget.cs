#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Primitives;
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
		public Func<Sprite> GetSprite;

		[FluentReference]
		public string TooltipText;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		public Func<string> GetTooltipText;

		readonly CachedTransform<(string Collection, string Image, int ThemeGeneration), Sprite> getImageCache = new(
			args => ChromeProvider.GetImage(args.Collection, args.Image));

		public ImageWidget()
		{
			GetImageName = () => ImageName;
			GetImageCollection = () => ImageCollection;
			var tooltipCache = new CachedTransform<string, string>(s => !string.IsNullOrEmpty(s) ? FluentProvider.GetMessage(s) : "");
			GetTooltipText = () => tooltipCache.Update(TooltipText);
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			GetSprite = () => getImageCache.Update((GetImageCollection(), GetImageName(), ChromeProvider.ThemeGeneration));
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

			GetSprite = () => getImageCache.Update((GetImageCollection(), GetImageName(), ChromeProvider.ThemeGeneration));
		}

		public override ImageWidget Clone() { return new ImageWidget(this); }

		public override void Draw()
		{
			var sprite = GetSprite();
			if (sprite == null)
				return;

			var rb = RenderBounds;
			if (Width != null && Height != null && rb.Width > 0 && rb.Height > 0 &&
				(rb.Width != sprite.Bounds.Width || rb.Height != sprite.Bounds.Height))
				WidgetUtils.DrawSprite(sprite, RenderOrigin, rb.Size);
			else
				WidgetUtils.DrawSprite(sprite, RenderOrigin);
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
