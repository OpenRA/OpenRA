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
	public class ObserverSupportPowerIconsWidget : Widget
	{
		public Func<Player> GetPlayer;
		Dictionary<string, Sprite> iconSprites;
		World world;
		WorldRenderer worldRenderer;
		Dictionary<string, Animation> clocks;

		[ObjectCreator.UseCtor]
		public ObserverSupportPowerIconsWidget(World world, WorldRenderer worldRenderer)
			: base()
		{
			iconSprites = Rules.Info.Values.SelectMany(u => u.Traits.WithInterface<SupportPowerInfo>())
				.Select(u => u.Image).Distinct()
				.ToDictionary(
					u => u,
					u => Game.modData.SpriteLoader.LoadAllSprites(u)[0]);
			this.world = world;
			this.worldRenderer = worldRenderer;
			clocks = new Dictionary<string, Animation>();
		}

		protected ObserverSupportPowerIconsWidget(ObserverSupportPowerIconsWidget other)
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
			var powers = player.PlayerActor.Trait<SupportPowerManager>().Powers
				.Select((a, i) => new { a, i });
			foreach (var power in powers)
			{
				if (!clocks.ContainsKey(power.a.Key))
				{
					clocks.Add(power.a.Key, new Animation("clock"));
				}
			}
			foreach (var power in powers)
			{
				var item = power.a.Value;
				if (item == null || item.Info == null || item.Info.Image == null)
					continue;
				var sprite = iconSprites[item.Info.Image];
				var size = sprite.size / new float2(2, 2);
				var location = new float2(RenderBounds.Location) + new float2(power.i * (int)size.Length, 0);
				WidgetUtils.DrawSHP(sprite, location, worldRenderer, size);

				var clock = clocks[power.a.Key];
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

		static string GetOverlayForItem(SupportPowerInstance item)
		{
			if (item.Disabled) return "ON HOLD";
			if (item.Ready) return "READY";
			return WidgetUtils.FormatTime(item.RemainingTime);
		}

		public override Widget Clone()
		{
			return new ObserverSupportPowerIconsWidget(this);
		}
	}
}
