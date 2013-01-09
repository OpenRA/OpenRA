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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class ObserverProductionIconsWidget : Widget
	{
		public Func<Player> GetPlayer;
		Dictionary<string, Sprite> iconSprites;
		World world;
		WorldRenderer worldRenderer;
		Dictionary<ProductionQueue, Animation> clocks;

		[ObjectCreator.UseCtor]
		public ObserverProductionIconsWidget(World world, WorldRenderer worldRenderer)
			: base()
		{
			iconSprites = Rules.Info.Values.Where(u => u.Traits.Contains<BuildableInfo>() && u.Name[0] != '^')
				.ToDictionary(
				u => u.Name,
				u => Game.modData.SpriteLoader.LoadAllSprites(u.Traits.Get<TooltipInfo>().Icon ?? (u.Name + "icon"))[0]);
			this.world = world;
			this.worldRenderer = worldRenderer;
			clocks = new Dictionary<ProductionQueue, Animation>();
		}

		protected ObserverProductionIconsWidget(ObserverProductionIconsWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
			iconSprites = other.iconSprites;
			world = other.world;
			worldRenderer = other.worldRenderer;
			clocks = other.clocks;
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
				if (!clocks.ContainsKey(queue.Trait))
				{
					clocks.Add(queue.Trait, new Animation("clock"));
				}
			}
			foreach (var queue in queues)
			{
				var item = queue.Trait.CurrentItem();
				if (item == null)
				{
					continue;
				}
				var sprite = iconSprites[item.Item];
				var size = sprite.size / new float2(2, 2);
				var location = new float2(RenderBounds.Location) + new float2(queue.i * (int)size.Length, 0);
				WidgetUtils.DrawSHP(sprite, location, worldRenderer, size);

				var clock = clocks[queue.Trait];
				clock.PlayFetchIndex("idle",
					() => item.TotalTime == 0 ? 0 : ((item.TotalTime - item.RemainingTime)
						* (clock.CurrentSequence.Length - 1) / item.TotalTime));
				clock.Tick();
				WidgetUtils.DrawSHP(clock.Image, location, worldRenderer, size);

				var tiny = Game.Renderer.Fonts["Tiny"];
				var text = GetOverlayForItem(item);
				tiny.DrawTextWithContrast(text,
					location + new float2(16, 16) - new float2(tiny.Measure(text).X / 2, 0),
					Color.White, Color.Black, 1);
			}
		}

		static string GetOverlayForItem(ProductionItem item)
		{
			if (item.Paused) return "ON HOLD";
			if (item.Done) return "READY";
			return WidgetUtils.FormatTime(item.RemainingTimeActual);
		}

		public override Widget Clone()
		{
			return new ObserverProductionIconsWidget(this);
		}
	}
}
