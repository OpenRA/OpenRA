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
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.D2k.Widgets;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Widgets;
using OpenRA.Mods.RA.Widgets.Logic;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2k.Widgets.Logic
{
	public class SlidingRadarBinLogic
	{
		enum RadarBinState { Closed, BinAnimating, RadarAnimating, Open }

		[ObjectCreator.UseCtor]
		public SlidingRadarBinLogic(Widget widget, World world)
		{
			var radarActive = false;
			var binState = RadarBinState.Closed;
			var radarBin = widget.Get<SlidingContainerWidget>("INGAME_RADAR_BIN");
			radarBin.IsOpen = () => radarActive || binState > RadarBinState.BinAnimating;
			radarBin.AfterOpen = () => binState = RadarBinState.RadarAnimating;
			radarBin.AfterClose = () => binState = RadarBinState.Closed;

			var radarMap = radarBin.Get<RadarWidget>("RADAR_MINIMAP");
			radarMap.IsEnabled = () => radarActive && binState >= RadarBinState.RadarAnimating;
			radarMap.AfterOpen = () => binState = RadarBinState.Open;
			radarMap.AfterClose = () => binState = RadarBinState.BinAnimating;

			radarBin.Get<ImageWidget>("RADAR_BIN_BG").GetImageCollection = () => "chrome-" + world.LocalPlayer.Country.Race;

			var cachedRadarActive = false;
			var radarTicker = widget.Get<LogicTickerWidget>("RADAR_TICKER");
			radarTicker.OnTick = () =>
			{
				// Update radar bin
				radarActive = world.ActorsWithTrait<ProvidesRadar>()
				.Any(a => a.Actor.Owner == world.LocalPlayer && a.Trait.IsActive);

				if (radarActive != cachedRadarActive)
					Sound.PlayNotification(world.Map.Rules, null, "Sounds", radarActive ? "RadarUp" : "RadarDown", null);
				cachedRadarActive = radarActive;
			};
		}
	}
}
