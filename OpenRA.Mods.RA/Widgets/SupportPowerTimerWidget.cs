#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class SupportPowerTimerWidget : Widget
	{
		public readonly string Font = "Bold";
		public readonly string Format = "{0}: {1}";
		public readonly TimerOrder Order = TimerOrder.Descending;

		readonly IEnumerable<SupportPowerInstance> powers;
		Pair<string, Color>[] texts;

		[ObjectCreator.UseCtor]
		public SupportPowerTimerWidget(World world)
		{
			powers = world.ActorsWithTrait<SupportPowerManager>()
				.Where(p => !p.Actor.IsDead && !p.Actor.Owner.NonCombatant)
				.SelectMany(s => s.Trait.Powers.Values)
				.Where(p => p.Instances.Any() && p.Info.DisplayTimer && !p.Disabled);
		}

		public override void Tick()
		{
			texts = powers.Select(p =>
			{
				var time = WidgetUtils.FormatTime(p.RemainingTime, false);
				var text = Format.F(p.Info.Description, time);
				var color = !p.Ready || Game.LocalTick % 50 < 25 ? p.Instances[0].self.Owner.Color.RGB : Color.White;
				return Pair.New(text, color);
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
				font.DrawTextWithContrast(t.First, new float2(Bounds.Location) + new float2(0, y), t.Second, Color.Black, 1);
				y += (font.Measure(t.First).Y + 5) * (int)Order;
			}
		}

		public enum TimerOrder { Ascending = -1, Descending = 1 }
	}
}
