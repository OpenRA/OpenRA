#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public interface IWorldTooltipInfo
	{
		string Label { get; }
		string Extra { get; }
		IPlayerSummary Owner { get; }
		bool ShowOwner { get; }
	}

	public class WorldTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public WorldTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, IWorldTooltipInfo info)
		{
			widget.IsVisible = () => info.Label != null;
			var label = widget.Get<LabelWidget>("LABEL");
			var flag = widget.Get<ImageWidget>("FLAG");
			var owner = widget.Get<LabelWidget>("OWNER");
			var extras = widget.Get<LabelWidget>("EXTRA");

			var font = Game.Renderer.Fonts[label.Font];
			var ownerFont = Game.Renderer.Fonts[owner.Font];
			var cachedWidth = 0;
			var labelText = "";
			var showOwner = false;
			var flagFaction = "";
			var ownerName = "";
			var ownerColor = Color.White;
			var extraText = "";

			var singleHeight = widget.Get("SINGLE_HEIGHT").Bounds.Height;
			var doubleHeight = widget.Get("DOUBLE_HEIGHT").Bounds.Height;
			var extraHeightOnDouble = extras.Bounds.Y;
			var extraHeightOnSingle = extraHeightOnDouble - (doubleHeight - singleHeight);

			tooltipContainer.BeforeRender = () =>
			{
				if (info == null || info.Label == null)
					return;

				var index = string.IsNullOrEmpty(info.Extra) ? 0 : info.Extra.Count(c => c == '\n') + 1;
				extraText = info.Extra != null ? info.Extra : "";
				showOwner = info.ShowOwner;
				labelText = info.Label;

				var textWidth = Math.Max(font.Measure(labelText).X, font.Measure(extraText).X);

				if (textWidth != cachedWidth)
				{
					label.Bounds.Width = textWidth;
					widget.Bounds.Width = 2 * label.Bounds.X + textWidth;
				}

				if (showOwner)
				{
					flagFaction = info.Owner.GetInternalFactionName();
					ownerName = info.Owner.GetPlayerName();
					ownerColor = info.Owner.GetColor().RGB;
					widget.Bounds.Height = doubleHeight;
					widget.Bounds.Width = Math.Max(widget.Bounds.Width,
						owner.Bounds.X + ownerFont.Measure(ownerName).X + label.Bounds.X);
					index++;
				}
				else
					widget.Bounds.Height = singleHeight;

				if (!string.IsNullOrEmpty(extraText))
				{
					widget.Bounds.Height += font.Measure(extraText).Y + extras.Bounds.Height;
					if (showOwner)
						extras.Bounds.Y = extraHeightOnDouble;
					else
						extras.Bounds.Y = extraHeightOnSingle;
				}
			};

			label.GetText = () => labelText;
			flag.IsVisible = () => showOwner;
			flag.GetImageCollection = () => "flags";
			flag.GetImageName = () => flagFaction;
			owner.IsVisible = () => showOwner;
			owner.GetText = () => ownerName;
			owner.GetColor = () => ownerColor;
			extras.GetText = () => extraText;
		}
	}
}
