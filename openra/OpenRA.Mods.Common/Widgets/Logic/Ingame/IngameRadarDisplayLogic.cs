#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Radar;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameRadarDisplayLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public IngameRadarDisplayLogic(Widget widget, World world)
		{
			var radarEnabled = false;
			var cachedRadarEnabled = false;
			var blockColor = Color.Transparent;
			var radar = widget.Get<RadarWidget>("RADAR_MINIMAP");
			radar.IsEnabled = () => radarEnabled;
			var devMode = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();

			var ticker = widget.Get<LogicTickerWidget>("RADAR_TICKER");
			ticker.OnTick = () =>
			{
				radarEnabled = devMode.DisableShroud || world.ActorsHavingTrait<ProvidesRadar>(r => !r.IsTraitDisabled)
					.Any(a => a.Owner == world.LocalPlayer);

				if (radarEnabled != cachedRadarEnabled)
					Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", radarEnabled ? radar.SoundUp : radar.SoundDown, null);
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
