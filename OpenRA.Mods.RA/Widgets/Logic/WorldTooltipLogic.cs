#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class WorldTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public WorldTooltipLogic(Widget widget, World world, TooltipContainerWidget tooltipContainer, ViewportControllerWidget viewport)
		{
			widget.IsVisible = () => viewport.TooltipType != WorldTooltipType.None;
			var label = widget.Get<LabelWidget>("LABEL");
			var flag = widget.Get<ImageWidget>("FLAG");
			var owner = widget.Get<LabelWidget>("OWNER");

			var font = Game.Renderer.Fonts[label.Font];
			var ownerFont = Game.Renderer.Fonts[owner.Font];
			var cachedWidth = 0;
			var labelText = "";
			var showOwner = false;
			var flagRace = "";
			var ownerName = "";
			var ownerColor = Color.White;

			var singleHeight = widget.Get("SINGLE_HEIGHT").Bounds.Height;
			var doubleHeight = widget.Get("DOUBLE_HEIGHT").Bounds.Height;

			tooltipContainer.BeforeRender = () =>
			{
				if (viewport == null || viewport.TooltipType == WorldTooltipType.None)
					return;

				showOwner = false;

				Player o = null;
				switch (viewport.TooltipType)
				{
					case WorldTooltipType.Unexplored:
						labelText = "Unexplored Terrain";
						break;
					case WorldTooltipType.Actor:
					{
						o = viewport.ActorTooltip.Owner;
						showOwner = !o.NonCombatant && viewport.ActorTooltip.TooltipInfo.IsOwnerRowVisible;

						var stance = o == null || world.RenderPlayer == null? Stance.None : o.Stances[world.RenderPlayer];
						labelText = viewport.ActorTooltip.TooltipInfo.TooltipForPlayerStance(stance);
						break;
					}
					case WorldTooltipType.FrozenActor:
					{
						o = viewport.FrozenActorTooltip.TooltipOwner;
						showOwner = !o.NonCombatant && viewport.FrozenActorTooltip.TooltipInfo.IsOwnerRowVisible;

						var stance = o == null || world.RenderPlayer == null? Stance.None : o.Stances[world.RenderPlayer];
						labelText = viewport.FrozenActorTooltip.TooltipInfo.TooltipForPlayerStance(stance);
						break;
					}
				}

				var textWidth = font.Measure(labelText).X;
				if (textWidth != cachedWidth)
				{
					label.Bounds.Width = textWidth;
					widget.Bounds.Width = 2*label.Bounds.X + textWidth;
				}

				if (showOwner)
				{
					flagRace = o.Country.Race;
					ownerName = o.PlayerName;
					ownerColor = o.Color.RGB;
					widget.Bounds.Height = doubleHeight;
					widget.Bounds.Width = Math.Max(widget.Bounds.Width,
						owner.Bounds.X + ownerFont.Measure(ownerName).X + label.Bounds.X);
				}
				else
					widget.Bounds.Height = singleHeight;
			};

			label.GetText = () => labelText;
			flag.IsVisible = () => showOwner;
			flag.GetImageCollection = () => "flags";
			flag.GetImageName = () => flagRace;
			owner.IsVisible = () => showOwner;
			owner.GetText = () => ownerName;
			owner.GetColor = () => ownerColor;
		}
	}
}

