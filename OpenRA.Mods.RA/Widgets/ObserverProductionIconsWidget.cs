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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class ObserverProductionIconsWidget : Widget
	{
		public Func<Player> GetPlayer;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		Dictionary<ProductionQueue, Animation> clocks;

		protected int iconWidth = 64;
		protected int iconHeight = 48;
		public int IconSpacing = 8;
		public float IconScale = 0.5f;
		public bool ShowTimeLeft = true;

		[ObjectCreator.UseCtor]
		public ObserverProductionIconsWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			clocks = new Dictionary<ProductionQueue, Animation>();
		}

		protected ObserverProductionIconsWidget(ObserverProductionIconsWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
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
					clocks.Add(queue.Trait, new Animation(world, "clock"));
				}
			}

			var iconIndex = 0;
			var iconSize = new float2(iconWidth * IconScale, iconHeight * IconScale);
			foreach (var queue in queues)
			{
				var current = queue.Trait.CurrentItem();
				if (current == null)
					continue;

				var actor = queue.Trait.AllItems().FirstOrDefault(a => a.Name == current.Item);
				if (actor == null)
					continue;

				var icon = new Animation(world, RenderSimple.GetImage(actor));
				icon.Play(actor.Traits.Get<TooltipInfo>().Icon);
				var location = new float2(RenderBounds.Location) + new float2(iconIndex * (iconWidth * IconScale + IconSpacing), 0);
				WidgetUtils.DrawSHPCentered(icon.Image, location + 0.5f * iconSize, worldRenderer, IconScale);

				var clock = clocks[queue.Trait];
				clock.PlayFetchIndex("idle",
					() => current.TotalTime == 0 ? 0 : ((current.TotalTime - current.RemainingTime)
					* (clock.CurrentSequence.Length - 1) / current.TotalTime));
				clock.Tick();
				WidgetUtils.DrawSHPCentered(clock.Image, location + 0.5f * iconSize, worldRenderer, IconScale);

				if (ShowTimeLeft)
				{
					var tiny = Game.Renderer.Fonts["Tiny"];
					var text = GetOverlayForItem(current);
					tiny.DrawTextWithContrast(text,
						location + new float2(iconSize.X / 2, iconSize.Y / 2 - 8) - new float2(tiny.Measure(text).X / 2, 0),
						Color.White, Color.Black, 1);
				}

				iconIndex++;
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
