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
		Animation icon;
		World world;
		WorldRenderer worldRenderer;
		Dictionary<string, Animation> clocks;

		public int IconWidth = 32;
		public int IconHeight = 24;
		public int IconSpacing = 8;

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
			var powers = player.World.ActorsWithTrait<SupportPower>().Where(s => s.Actor.Owner == player)
				.Select((a, i) => new { a.Trait, i }); ;
			foreach (var power in powers)
			{
				if (!clocks.ContainsKey(power.Trait.Key))
				{
					clocks.Add(power.Trait.Key, new Animation(world, "clock"));
				}
			}

			var iconSize = new float2(IconWidth, IconHeight);
			foreach (var power in powers)
			{
				var item = power.Trait;
				if (item == null || item.Info == null || item.Info.Icon == null)
					continue;

				icon.Play(item.Info.Icon);
				var location = new float2(RenderBounds.Location) + new float2(power.i * (IconWidth + IconSpacing), 0);
				WidgetUtils.DrawSHPCentered(icon.Image, location + 0.5f * iconSize, worldRenderer, 0.5f);

				var clock = clocks[power.Trait.Key];
				clock.PlayFetchIndex("idle",
					() => item.TotalTime == 0 ? 0 : ((item.TotalTime - item.RemainingTime)
						* (clock.CurrentSequence.Length - 1) / item.TotalTime));
				clock.Tick();
				WidgetUtils.DrawSHPCentered(clock.Image, location + 0.5f * iconSize, worldRenderer, 0.5f);

				var tiny = Game.Renderer.Fonts["Tiny"];
				var text = GetOverlayForItem(item);
				tiny.DrawTextWithContrast(text,
					location + new float2(16, 16) - new float2(tiny.Measure(text).X / 2, 0),
					Color.White, Color.Black, 1);
			}
		}

		static string GetOverlayForItem(SupportPower item)
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
