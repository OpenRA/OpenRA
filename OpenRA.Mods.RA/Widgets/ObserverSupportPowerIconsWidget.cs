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
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class ObserverSupportPowerIconsWidget : Widget
	{
		public Func<Player> GetPlayer;
		Animation icon;
		World world;
		WorldRenderer worldRenderer;
		Dictionary<string, Animation> clocks;

		protected int iconWidth = 64;
		protected int iconHeight = 48;
		public int IconSpacing = 8;
		public float IconScale = 0.5f;
		public bool ShowTimeLeft = true;

		[ObjectCreator.UseCtor]
		public ObserverSupportPowerIconsWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			clocks = new Dictionary<string, Animation>();
			icon = new Animation(world, "icon");
		}

		protected ObserverSupportPowerIconsWidget(ObserverSupportPowerIconsWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
			icon = other.icon;
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
					clocks.Add(power.a.Key, new Animation(world, "clock"));
				}
			}

			var iconSize = new float2(iconWidth * IconScale, iconHeight * IconScale);
			foreach (var power in powers)
			{
				var item = power.a.Value;
				if (item == null || item.Info == null || item.Info.Icon == null)
					continue;

				icon.Play(item.Info.Icon);
				var location = new float2(RenderBounds.Location) + new float2(power.i * (iconWidth * IconScale + IconSpacing), 0);
				WidgetUtils.DrawSHPCentered(icon.Image, location + 0.5f * iconSize, worldRenderer, IconScale);

				var clock = clocks[power.a.Key];
				clock.PlayFetchIndex("idle",
					() => item.TotalTime == 0 ? 0 : ((item.TotalTime - item.RemainingTime)
						* (clock.CurrentSequence.Length - 1) / item.TotalTime));
				clock.Tick();
				WidgetUtils.DrawSHPCentered(clock.Image, location + 0.5f * iconSize, worldRenderer, IconScale);

				if (ShowTimeLeft)
				{
					var tiny = Game.Renderer.Fonts["Tiny"];
					var text = GetOverlayForItem(item);
					tiny.DrawTextWithContrast(text,
						location + new float2(iconSize.X / 2, iconSize.Y / 2 - 8) - new float2(tiny.Measure(text).X / 2, 0),
						Color.White, Color.Black, 1);
				}
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
