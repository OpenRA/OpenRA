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
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class WorldTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public WorldTooltipLogic(Widget widget, World world, TooltipContainerWidget tooltipContainer, ViewportControllerWidget viewport)
		{
			widget.IsVisible = () => viewport.TooltipType != WorldTooltipType.None;
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

			var singleHeight = (int)widget.Get("SINGLE_HEIGHT").Node.LayoutHeight;
			var doubleHeight = (int)widget.Get("DOUBLE_HEIGHT").Node.LayoutHeight;
			var extraHeightOnDouble = (int)extras.Node.LayoutY;
			var extraHeightOnSingle = extraHeightOnDouble - (doubleHeight - singleHeight);

			tooltipContainer.BeforeRender = () =>
			{
				if (viewport == null || viewport.TooltipType == WorldTooltipType.None)
					return;

				var index = 0;
				extraText = "";
				showOwner = false;

				Player o = null;
				switch (viewport.TooltipType)
				{
					case WorldTooltipType.Unexplored:
						labelText = "Unrevealed Terrain";
						break;
					case WorldTooltipType.Resource:
						labelText = viewport.ResourceTooltip.Info.Name;
						break;
					case WorldTooltipType.Actor:
						{
							o = viewport.ActorTooltip.Owner;
							showOwner = o != null && !o.NonCombatant && viewport.ActorTooltip.TooltipInfo.IsOwnerRowVisible;

							var stance = o == null || world.RenderPlayer == null ? Stance.None : o.Stances[world.RenderPlayer];
							labelText = viewport.ActorTooltip.TooltipInfo.TooltipForPlayerStance(stance);
							break;
						}

					case WorldTooltipType.FrozenActor:
						{
							o = viewport.FrozenActorTooltip.TooltipOwner;
							showOwner = o != null && !o.NonCombatant && viewport.FrozenActorTooltip.TooltipInfo.IsOwnerRowVisible;

							var stance = o == null || world.RenderPlayer == null ? Stance.None : o.Stances[world.RenderPlayer];
							labelText = viewport.FrozenActorTooltip.TooltipInfo.TooltipForPlayerStance(stance);
							break;
						}
				}

				if (viewport.ActorTooltipExtra != null)
				{
					foreach (var info in viewport.ActorTooltipExtra)
					{
						if (info.IsTooltipVisible(world.RenderPlayer))
						{
							if (index != 0)
								extraText += "\n";
							extraText += info.TooltipText;
							index++;
						}
					}
				}

				var textWidth = Math.Max(font.Measure(labelText).X, font.Measure(extraText).X);

				if (textWidth != cachedWidth)
				{
					label.Node.Width = textWidth;
					label.Node.CalculateLayout();
					widget.Node.Width = 2 * (int)label.Node.LayoutX + textWidth;
					widget.Node.CalculateLayout();
				}

				if (showOwner)
				{
					flagFaction = o.Faction.InternalName;
					ownerName = o.PlayerName;
					ownerColor = o.Color;
					widget.Node.Height = doubleHeight;
					widget.Node.Width = Math.Max((int)widget.Node.LayoutWidth,
						(int)owner.Node.LayoutX + ownerFont.Measure(ownerName).X + (int)label.Node.LayoutX);
					widget.Node.CalculateLayout();
					index++;
				}
				else
				{
					widget.Node.Height = singleHeight;
					widget.Node.CalculateLayout();
				}

				if (extraText != "")
				{
					widget.Node.Height = (int)widget.Node.LayoutHeight + font.Measure(extraText).Y + (int)extras.Node.LayoutHeight;
					widget.Node.CalculateLayout();
					if (showOwner)
						extras.Node.Top = extraHeightOnDouble;
					else
						extras.Node.Top = extraHeightOnSingle;
					extras.Node.CalculateLayout();
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
