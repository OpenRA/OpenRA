#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ObserverSupportPowerIconsWidget : Widget
	{
		readonly Animation icon;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly Dictionary<string, Animation> clocks;
		readonly int timestep;

		public int IconWidth = 32;
		public int IconHeight = 24;
		public int IconSpacing = 8;

		public string ClockAnimation = "clock";
		public string ClockSequence = "idle";
		public string ClockPalette = "chrome";
		public Func<Player> GetPlayer;

		[ObjectCreator.UseCtor]
		public ObserverSupportPowerIconsWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			clocks = new Dictionary<string, Animation>();
			icon = new Animation(world, "icon");

			// Timers in replays should be synced to the effective game time, not the playback time.
			timestep = world.Timestep;
			if (world.IsReplay)
				timestep = world.WorldActor.Trait<MapOptions>().GameSpeed.Timestep;
		}

		protected ObserverSupportPowerIconsWidget(ObserverSupportPowerIconsWidget other)
			: base(other)
		{
			GetPlayer = other.GetPlayer;
			icon = other.icon;
			world = other.world;
			worldRenderer = other.worldRenderer;
			clocks = other.clocks;
			timestep = other.timestep;

			IconWidth = other.IconWidth;
			IconHeight = other.IconHeight;
			IconSpacing = other.IconSpacing;

			ClockAnimation = other.ClockAnimation;
			ClockSequence = other.ClockSequence;
			ClockPalette = other.ClockPalette;
		}

		public override void Draw()
		{
			var player = GetPlayer();
			if (player == null)
				return;

			var powers = player.PlayerActor.Trait<SupportPowerManager>().Powers
				.Where(x => !x.Value.Disabled).Select((a, i) => new { a, i });
			foreach (var power in powers)
			{
				if (!clocks.ContainsKey(power.a.Key))
					clocks.Add(power.a.Key, new Animation(world, ClockAnimation));
			}

			var iconSize = new float2(IconWidth, IconHeight);
			foreach (var power in powers)
			{
				var item = power.a.Value;
				if (item == null || item.Info == null || item.Info.Icon == null)
					continue;

				icon.Play(item.Info.Icon);
				var location = new float2(RenderBounds.Location) + new float2(power.i * (IconWidth + IconSpacing), 0);
				WidgetUtils.DrawSHPCentered(icon.Image, location + 0.5f * iconSize, worldRenderer.Palette(item.Info.IconPalette), 0.5f);

				var clock = clocks[power.a.Key];
				clock.PlayFetchIndex(ClockSequence,
					() => item.TotalTime == 0 ? 0 : ((item.TotalTime - item.RemainingTime)
						* (clock.CurrentSequence.Length - 1) / item.TotalTime));
				clock.Tick();
				WidgetUtils.DrawSHPCentered(clock.Image, location + 0.5f * iconSize, worldRenderer.Palette(ClockPalette), 0.5f);

				var tiny = Game.Renderer.Fonts["Tiny"];
				var text = GetOverlayForItem(item, timestep);
				tiny.DrawTextWithContrast(text,
					location + new float2(16, 16) - new float2(tiny.Measure(text).X / 2, 0),
					Color.White, Color.Black, 1);
			}
		}

		static string GetOverlayForItem(SupportPowerInstance item, int timestep)
		{
			if (item.Disabled) return "ON HOLD";
			if (item.Ready) return "READY";
			return WidgetUtils.FormatTime(item.RemainingTime, timestep);
		}

		public override Widget Clone()
		{
			return new ObserverSupportPowerIconsWidget(this);
		}
	}
}
