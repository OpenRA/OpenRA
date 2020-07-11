#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This trait allows setting a time limit on matches. Attach this to the World actor.")]
	public class TimeLimitManagerInfo : TraitInfo, ILobbyOptions, IRulesetLoaded
	{
		[Desc("Label that will be shown for the time limit option in the lobby.")]
		public readonly string TimeLimitLabel = "Time Limit";

		[Desc("Tooltip description that will be shown for the time limit option in the lobby.")]
		public readonly string TimeLimitDescription = "Player or team with the highest score after this time wins";

		[Desc("Time Limit options that will be shown in the lobby dropdown. Values are in minutes.")]
		public readonly int[] TimeLimitOptions = { 0, 10, 20, 30, 40, 60, 90 };

		[Desc("List of remaining minutes of game time when a text and optional speech notification should be made to players.")]
		public readonly Dictionary<int, string> TimeLimitWarnings = new Dictionary<int, string>
		{
			{ 1, null },
			{ 2, null },
			{ 3, null },
			{ 4, null },
			{ 5, null },
			{ 10, null },
		};

		[Desc("Default selection for the time limit option in the lobby. Needs to use one of the TimeLimitOptions.")]
		public readonly int TimeLimitDefault = 0;

		[Desc("Prevent the time limit option from being changed in the lobby.")]
		public readonly bool TimeLimitLocked = false;

		[Desc("Whether to display the options dropdown in the lobby.")]
		public readonly bool TimeLimitDropdownVisible = true;

		[Desc("Display order for the time limit dropdown in the lobby.")]
		public readonly int TimeLimitDisplayOrder = 0;

		[Desc("Notification text for time limit warnings. The string '{0}' will be replaced by the remaining time in minutes, '{1}' is used for the plural form.")]
		public readonly string Notification = "{0} minute{1} remaining.";

		[Desc("ID of the LabelWidget used to display a text ingame that will be updated every second.")]
		public readonly string CountdownLabel = null;

		[Desc("Text to be shown using the CountdownLabel. The string '{0}' will be replaced by the time in hh:mm:ss format.")]
		public readonly string CountdownText = null;

		[Desc("Will prevent showing/playing the built-in time limit warnings when set to true.")]
		public readonly bool SkipTimeRemainingNotifications = false;

		[Desc("Will prevent showing/playing the built-in timer expired notification when set to true.")]
		public readonly bool SkipTimerExpiredNotification = false;

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (!TimeLimitOptions.Contains(TimeLimitDefault))
				throw new YamlException("TimeLimitDefault must be a value from TimeLimitOptions");
		}

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			var timelimits = TimeLimitOptions.ToDictionary(c => c.ToString(), c =>
			{
				if (c == 0)
					return "No limit";
				else
					return c.ToString() + " minute{0}".F(c > 1 ? "s" : null);
			});

			yield return new LobbyOption("timelimit", TimeLimitLabel, TimeLimitDescription, TimeLimitDropdownVisible, TimeLimitDisplayOrder,
				new ReadOnlyDictionary<string, string>(timelimits), TimeLimitDefault.ToString(), TimeLimitLocked);
		}

		public override object Create(ActorInitializer init) { return new TimeLimitManager(init.Self, this); }
	}

	public class TimeLimitManager : INotifyTimeLimit, ITick, IWorldLoaded
	{
		readonly TimeLimitManagerInfo info;
		MapOptions mapOptions;
		LabelWidget countdownLabel;
		CachedTransform<int, string> countdown;
		int ticksRemaining;

		public int TimeLimit;
		public string Notification;

		public TimeLimitManager(Actor self, TimeLimitManagerInfo info)
		{
			this.info = info;
			Notification = info.Notification;

			var tl = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("timelimit", info.TimeLimitDefault.ToString());
			if (!int.TryParse(tl, out TimeLimit))
				TimeLimit = info.TimeLimitDefault;

			// Convert from minutes to ticks
			TimeLimit *= 60 * (1000 / self.World.Timestep);
		}

		void IWorldLoaded.WorldLoaded(World w, OpenRA.Graphics.WorldRenderer wr)
		{
			mapOptions = w.WorldActor.Trait<MapOptions>();
			if (string.IsNullOrWhiteSpace(info.CountdownLabel) || string.IsNullOrWhiteSpace(info.CountdownText))
				return;

			countdownLabel = Ui.Root.GetOrNull<LabelWidget>(info.CountdownLabel);
			if (countdownLabel != null)
			{
				countdown = new CachedTransform<int, string>(t =>
					info.CountdownText.F(WidgetUtils.FormatTime(t, true, w.IsReplay ? mapOptions.GameSpeed.Timestep : w.Timestep)));
				countdownLabel.GetText = () => countdown.Update(ticksRemaining);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (TimeLimit <= 0)
				return;

			var ticksPerSecond = 1000 / (self.World.IsReplay ? mapOptions.GameSpeed.Timestep : self.World.Timestep);
			ticksRemaining = TimeLimit - self.World.WorldTick;

			if (ticksRemaining == 0)
			{
				foreach (var ntl in self.TraitsImplementing<INotifyTimeLimit>())
					ntl.NotifyTimerExpired(self);

				foreach (var p in self.World.Players)
					foreach (var ntl in p.PlayerActor.TraitsImplementing<INotifyTimeLimit>())
						ntl.NotifyTimerExpired(p.PlayerActor);

				return;
			}

			if (ticksRemaining < 0 || info.SkipTimeRemainingNotifications)
				return;

			foreach (var m in info.TimeLimitWarnings.Keys)
			{
				if (ticksRemaining == m * 60 * ticksPerSecond)
				{
					Game.AddSystemLine(Notification.F(m, m > 1 ? "s" : null));

					var faction = self.World.LocalPlayer == null ? null : self.World.LocalPlayer.Faction.InternalName;
					Game.Sound.PlayNotification(self.World.Map.Rules, self.World.LocalPlayer, "Speech", info.TimeLimitWarnings[m], faction);
				}
			}
		}

		void INotifyTimeLimit.NotifyTimerExpired(Actor self)
		{
			if (countdownLabel != null)
				countdownLabel.GetText = () => null;

			if (!info.SkipTimerExpiredNotification)
				Game.AddSystemLine("Time limit has expired.");
		}
	}
}
