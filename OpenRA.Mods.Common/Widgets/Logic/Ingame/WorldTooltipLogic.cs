#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class WorldTooltipLogic : ChromeLogic
	{
		const int CellTriggerNone = int.MinValue;

		[TranslationReference]
		static readonly string UnrevealedTerrain = "unrevealed-terrain";

		readonly Widget widget;
		readonly ModData modData;
		readonly World world;
		readonly ViewportControllerWidget viewport;
		readonly WorldRenderer worldRenderer;

		readonly LabelWidget label;
		readonly ImageWidget flag;
		readonly LabelWidget owner;
		readonly LabelWidget extras;
		readonly SpriteFont font;
		readonly SpriteFont ownerFont;
		readonly int singleHeight;
		readonly int doubleHeight;
		readonly int extraHeightOnDouble;
		readonly int extraHeightOnSingle;

		string labelText = "";
		bool showOwner = false;
		string flagFaction = "";
		Color ownerColor = Color.White;
		string ownerName = "";
		string extraText = "";

		int cellTrigger = CellTriggerNone;
		CPos lastMapPos = CPos.Zero;

		[ObjectCreator.UseCtor]
		public WorldTooltipLogic(Widget widget, ModData modData, World world, WorldRenderer worldRenderer, TooltipContainerWidget tooltipContainer, ViewportControllerWidget viewport)
		{
			this.widget = widget;
			this.modData = modData;
			this.world = world;
			this.worldRenderer = worldRenderer;
			this.viewport = viewport;
			widget.IsVisible = () => viewport.TooltipType != WorldTooltipType.None;

			label = widget.Get<LabelWidget>("LABEL");
			flag = widget.Get<ImageWidget>("FLAG");
			owner = widget.Get<LabelWidget>("OWNER");
			extras = widget.Get<LabelWidget>("EXTRA");

			font = Game.Renderer.Fonts[label.Font];
			ownerFont = Game.Renderer.Fonts[owner.Font];

			singleHeight = widget.Get("SINGLE_HEIGHT").Bounds.Height;
			doubleHeight = widget.Get("DOUBLE_HEIGHT").Bounds.Height;
			extraHeightOnDouble = extras.Bounds.Y;
			extraHeightOnSingle = extraHeightOnDouble - (doubleHeight - singleHeight);

			tooltipContainer.InitializeTooltipContent = () =>
			{
				AddCellTrigger();
				InitializeTooltipContent();
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

		void InitializeTooltipContent()
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
					labelText = modData.Translation.GetString(UnrevealedTerrain);
					break;
				case WorldTooltipType.Resource:
					labelText = viewport.ResourceTooltip;
					break;
				case WorldTooltipType.Actor:
					{
						o = viewport.ActorTooltip.Owner;
						showOwner = o != null && !o.NonCombatant && viewport.ActorTooltip.TooltipInfo.IsOwnerRowVisible;

						var stance = o == null || world.RenderPlayer == null ? PlayerRelationship.None : o.RelationshipWith(world.RenderPlayer);
						labelText = viewport.ActorTooltip.TooltipInfo.TooltipForPlayerStance(stance);
						break;
					}

				case WorldTooltipType.FrozenActor:
					{
						o = viewport.FrozenActorTooltip.TooltipOwner;
						showOwner = o != null && !o.NonCombatant && viewport.FrozenActorTooltip.TooltipInfo.IsOwnerRowVisible;

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
				ownerColor = o.Color;
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
		}

		void RemoveCellTrigger()
		{
			if (cellTrigger != CellTriggerNone)
			{
				world.ActorMap.RemoveCellTrigger(cellTrigger);
				cellTrigger = CellTriggerNone;
				lastMapPos = CPos.Zero;
			}
		}

		void AddCellTrigger()
		{
			var pos = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (lastMapPos != pos)
			{
				RemoveCellTrigger();
				cellTrigger = world.ActorMap.AddCellTrigger(new CPos[] { pos },
					_ => InitializeTooltipContent(),
					_ => InitializeTooltipContent());
				lastMapPos = pos;
			}
		}

		protected override void Dispose(bool disposing)
		{
			RemoveCellTrigger();
		}
	}
}
