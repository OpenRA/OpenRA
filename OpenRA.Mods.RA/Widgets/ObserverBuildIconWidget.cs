#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class ObserverBuildIconWidget : Widget
	{
		public Func<Player> GetPlayer;
		public Func<string> GetQueue;
		Dictionary<string, Sprite> iconSprites;
		World world;
		WorldRenderer worldRenderer;

		[ObjectCreator.UseCtor]
		public ObserverBuildIconWidget(World world, WorldRenderer worldRenderer)
			: base()
		{
			iconSprites = Rules.Info.Values.Where(u => u.Traits.Contains<BuildableInfo>() && u.Name[0] != '^')
				.ToDictionary(
				u => u.Name,
				u => Game.modData.SpriteLoader.LoadAllSprites(u.Traits.Get<TooltipInfo>().Icon ?? (u.Name + "icon"))[0]);
			this.world = world;
			this.worldRenderer = worldRenderer;
		}

		protected ObserverBuildIconWidget(ObserverBuildIconWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
			GetQueue = other.GetQueue;
			iconSprites = other.iconSprites;
			world = other.world;
			worldRenderer = other.worldRenderer;
		}

		public override void Draw()
		{
			var player = GetPlayer();
			var queue = GetQueue();
			if (player == null || queue == null)
			{
				return;
			}
			var production = world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player && a.Trait.Info.Type == queue)
				.Select(a => a.Trait)
				.FirstOrDefault();
			if (production == null)
			{
				return;
			}
			var item = production.CurrentItem();
			if (item == null)
			{
				return;
			}
			var location = new float2(RenderBounds.Location);
			var sprite = iconSprites[item.Item];
			WidgetUtils.DrawSHP(sprite, location, worldRenderer, sprite.size / new float2(2, 2));
		}

		public override Widget Clone()
		{
			return new ObserverBuildIconWidget(this);
		}
	}
}
