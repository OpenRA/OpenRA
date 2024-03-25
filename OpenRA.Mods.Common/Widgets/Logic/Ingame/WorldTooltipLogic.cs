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
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class WorldTooltipLogic : ChromeLogic
	{
		[TranslationReference]
		const string UnrevealedTerrain = "label-unrevealed-terrain";

		[ObjectCreator.UseCtor]
		public WorldTooltipLogic(Widget widget, ModData modData, World world, TooltipContainerWidget tooltipContainer, ViewportControllerWidget viewport)
		{
			widget.IsVisible = () => viewport.TooltipType != WorldTooltipType.None;
			var label = widget.Get<LabelWidget>("LABEL");
			var flag = widget.Get<ImageWidget>("FLAG");
			var owner = widget.Get<LabelWidget>("OWNER");
			var extras = widget.Get<LabelWidget>("EXTRA");

			var font = Game.Renderer.Fonts[label.Font];
			var ownerFont = Game.Renderer.Fonts[owner.Font];
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

			var unrevealedTerrain = TranslationProvider.GetString(UnrevealedTerrain);

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
						labelText = unrevealedTerrain;
						break;
					case WorldTooltipType.Resource:
						labelText = viewport.ResourceTooltip;
						break;
					case WorldTooltipType.Actor:
					{
						o = viewport.ActorTooltip.Owner;
						showOwner = o != null && !o.NonCombatant && viewport.ActorTooltip.TooltipInfo.IsOwnerRowVisible;

						if (showOwner)
							ownerColor = o.Color;

						var stance = o == null || world.RenderPlayer == null ? PlayerRelationship.None : o.RelationshipWith(world.RenderPlayer);
						labelText = viewport.ActorTooltip.TooltipInfo.TooltipForPlayerStance(stance);
						break;
					}

					case WorldTooltipType.FrozenActor:
					{
						o = viewport.FrozenActorTooltip.TooltipOwner;
						showOwner = o != null && !o.NonCombatant && viewport.FrozenActorTooltip.TooltipInfo.IsOwnerRowVisible;

						if (showOwner)
							ownerColor = o.Color;

						var stance = o == null || world.RenderPlayer == null ? PlayerRelationship.None : o.RelationshipWith(world.RenderPlayer);
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
				label.Bounds.Width = textWidth;
				widget.Bounds.Width = 2 * label.Bounds.X + textWidth;

				if (showOwner)
				{
					flagFaction = o.Faction.InternalName;
					ownerName = o.PlayerName;
					widget.Bounds.Height = doubleHeight;
					widget.Bounds.Width = Math.Max(widget.Bounds.Width,
						owner.Bounds.X + ownerFont.Measure(ownerName).X + label.Bounds.X);
					index++;
				}
				else
					widget.Bounds.Height = singleHeight;

				if (extraText != "")
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
