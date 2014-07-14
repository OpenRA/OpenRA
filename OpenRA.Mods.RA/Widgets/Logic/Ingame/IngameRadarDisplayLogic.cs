#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngameRadarDisplayLogic
	{
		[ObjectCreator.UseCtor]
		public IngameRadarDisplayLogic(Widget widget, World world)
		{
			var radarEnabled = false;
			var cachedRadarEnabled = false;
			var blockColor = Color.Transparent;
			var radar = widget.Get<RadarWidget>("RADAR_MINIMAP");
			radar.IsEnabled = () => radarEnabled;

			var ticker = widget.Get<LogicTickerWidget>("RADAR_TICKER");
			ticker.OnTick = () =>
			{
				radarEnabled = world.ActorsWithTrait<ProvidesRadar>()
					.Any(a => a.Actor.Owner == world.LocalPlayer && a.Trait.IsActive);

				if (radarEnabled != cachedRadarEnabled)
					Sound.PlayNotification(world.Map.Rules, null, "Sounds", radarEnabled ? "RadarUp" : "RadarDown", null);
				cachedRadarEnabled = radarEnabled;
			};

			var block = widget.GetOrNull<ColorBlockWidget>("RADAR_FADETOBLACK");
			if (block != null)
			{
				radar.Animating = x => blockColor = Color.FromArgb((int)(255 * x), Color.Black);
				block.IsVisible = () => blockColor.A != 0;
				block.GetColor = () => blockColor;
			}
		}
	}
}
