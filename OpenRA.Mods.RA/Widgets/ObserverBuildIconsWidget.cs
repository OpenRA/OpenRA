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
using System.Drawing;

namespace OpenRA.Mods.RA.Widgets
{
	public class ObserverBuildIconsWidget : Widget
	{
		public Func<Player> GetPlayer;
		Dictionary<string, Sprite> iconSprites;
		World world;
		WorldRenderer worldRenderer;

		[ObjectCreator.UseCtor]
		public ObserverBuildIconsWidget(World world, WorldRenderer worldRenderer)
			: base()
		{
			iconSprites = Rules.Info.Values.Where(u => u.Traits.Contains<BuildableInfo>() && u.Name[0] != '^')
				.ToDictionary(
				u => u.Name,
				u => Game.modData.SpriteLoader.LoadAllSprites(u.Traits.Get<TooltipInfo>().Icon ?? (u.Name + "icon"))[0]);
			this.world = world;
			this.worldRenderer = worldRenderer;
		}

		protected ObserverBuildIconsWidget(ObserverBuildIconsWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
			iconSprites = other.iconSprites;
			world = other.world;
			worldRenderer = other.worldRenderer;
		}

		public override void Draw()
		{
			var player = GetPlayer();
			if (player == null)
			{
				return;
			}
			var queues = world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player)
				.Select((a, i) => new { a.Trait, i });
			foreach (var queue in queues)
			{
				var item = queue.Trait.CurrentItem();
				if (item == null)
				{
					return;
				}
				var sprite = iconSprites[item.Item];
				var size = sprite.size / new float2(2, 2);
				var location = new float2(RenderBounds.Location) + new float2(queue.i * (int)size.Length, 0);
				WidgetUtils.DrawSHP(sprite, location, worldRenderer, size);
			}
		}

		public override Widget Clone()
		{
			return new ObserverBuildIconsWidget(this);
		}
	}
}
