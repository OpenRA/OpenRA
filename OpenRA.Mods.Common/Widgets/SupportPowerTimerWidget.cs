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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class SupportPowerTimerWidget : Widget
	{
		public readonly string Font = "Bold";
		public readonly string Format = "{0}: {1}";
		public readonly TimerOrder Order = TimerOrder.Descending;

		readonly int timestep;
		readonly IEnumerable<SupportPowerInstance> powers;
		Tuple<string, Color, Color>[] texts;

		[ObjectCreator.UseCtor]
		public SupportPowerTimerWidget(World world)
		{
			powers = world.ActorsWithTrait<SupportPowerManager>()
				.Where(p => !p.Actor.IsDead && !p.Actor.Owner.NonCombatant)
				.SelectMany(s => s.Trait.Powers.Values)
				.Where(p => p.Instances.Any() && p.Info.DisplayTimer && !p.Disabled);

			// Timers in replays should be synced to the effective game time, not the playback time.
			timestep = world.Timestep;
			if (world.IsReplay)
				timestep = world.WorldActor.Trait<MapOptions>().GameSpeed.Timestep;
		}

		public override void Tick()
		{
			texts = powers.Select(p =>
			{
				var time = WidgetUtils.FormatTime(p.RemainingTime, false, timestep);
				var text = Format.F(p.Info.Description, time);
				var self = p.Instances[0].Self;
				var playerColor = self.Owner.Color.RGB;

				if (Game.Settings.Game.UsePlayerStanceColors)
					playerColor = self.Owner.PlayerStanceColor(self);

				var color = !p.Ready || Game.LocalTick % 50 < 25 ? playerColor : Color.White;

				var inversedColor = self.Owner.Color;
				var inversedL = color == Color.White || inversedColor.L > 128 ? (byte)0 : (byte)255;
				inversedColor = new HSLColor(inversedColor.H, 0, inversedL);

				return Tuple.Create(text, color, inversedColor.RGB);
			}).ToArray();
		}

		public override void Draw()
		{
			if (!IsVisible() || texts == null)
				return;

			var y = 0;
			foreach (var t in texts)
			{
				var font = Game.Renderer.Fonts[Font];
				font.DrawTextWithContrast(t.Item1, new float2(Bounds.Location) + new float2(0, y), t.Item2, t.Item3, 1);
				y += (font.Measure(t.Item1).Y + 5) * (int)Order;
			}
		}

		public enum TimerOrder { Ascending = -1, Descending = 1 }
	}
}
