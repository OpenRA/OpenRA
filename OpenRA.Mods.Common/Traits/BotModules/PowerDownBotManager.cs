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

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI powerdown.",
		"You need to use PowerMultiplier on toggle control only on related buildings, for calculation of this bot module")]
	public class PowerDownBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Delay (in ticks) between two action on toggling powerdown.")]
		public readonly int Interval = 150;

		[Desc("Actors that allow this module to toggle")]
		public readonly FrozenSet<string> PowerDownTypes = FrozenSet<string>.Empty;

		[Desc("Order used by " + nameof(ToggleConditionOnOrderInfo) + " for powerdown on toggled actor.")]
		public readonly string PowerDownOrder = "PowerDown";

		public override object Create(ActorInitializer init) { return new PowerDownBotModule(init.Self, this); }
	}

	public class PowerDownBotModule : ConditionalTrait<PowerDownBotModuleInfo>, IBotTick, IGameSaveTraitData, INotifyActorDisposing
	{
		readonly World world;
		readonly Player player;
		readonly ActorIndex.OwnerAndNamesAndTrait<ToggleConditionOnOrderInfo> togglable;
		readonly Func<Actor, bool> isTogglableValid;

		// We keep a list to track toggled buildings for performance.
		readonly List<ToggledPowerWrapper> toggled = [];

		PowerManager playerPower;
		int toggleTick;

		sealed class ToggledPowerWrapper
		{
			public int ExpectedPowerChanging;
			public Actor Actor;

			public ToggledPowerWrapper(Actor a, int p)
			{
				Actor = a;
				ExpectedPowerChanging = p;
			}
		}

		public PowerDownBotModule(Actor self, PowerDownBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			togglable = new ActorIndex.OwnerAndNamesAndTrait<ToggleConditionOnOrderInfo>(world, info.PowerDownTypes, player);

			isTogglableValid = a => a != null && a.Owner == self.Owner && !a.IsDead && a.IsInWorld;
		}

		protected override void Created(Actor self)
		{
			playerPower = self.Owner.PlayerActor.TraitOrDefault<PowerManager>();
		}

		protected override void TraitEnabled(Actor self)
		{
			toggleTick = world.LocalRandom.Next(Info.Interval);
		}

		// We calculate the approximate power changing if toggled on. Since we have no idea on the conditions set by users
		// Calculation here need user to use PowerMultiplier on toggle control only on related buildings, for calculation of this bot module.
		static int GetTogglePowerChanging(Actor a)
		{
			var powerChangingIfToggled = 0;
			var power = a.TraitsImplementing<Power>().Where(t => !t.IsTraitDisabled).Sum(t => t.Info.Amount);
			if (power != 0)
			{
				var powerMulTraits = a.TraitsImplementing<PowerMultiplier>().ToArray();
				powerChangingIfToggled = power * (powerMulTraits.Sum(p => p.Info.Modifier) - 100) / 100;
				if (Array.Exists(powerMulTraits, t => !t.IsTraitDisabled))
					powerChangingIfToggled = -powerChangingIfToggled;
			}

			return powerChangingIfToggled;
		}

		IEnumerable<ToggledPowerWrapper> GetOnlineBuildings()
		{
			var toggleableBuildings = new List<ToggledPowerWrapper>();

			foreach (var a in togglable.Actors.Where(a => !a.IsDead && a.Info.HasTraitInfo<PowerInfo>()))
			{
				// Note: it is OK if GetTogglePowerChanging is not accurate, when player is still in lowpower.
				// The bot will try to toggle off more buildings next bot tick.
				var powerChanging = GetTogglePowerChanging(a);
				if (powerChanging > 0)
					toggleableBuildings.Add(new ToggledPowerWrapper(a, powerChanging));
			}

			return toggleableBuildings.OrderBy(bpw => bpw.ExpectedPowerChanging);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (toggleTick > 0 || playerPower == null)
			{
				toggleTick--;
				return;
			}

			var power = playerPower.ExcessPower;
			var togglingBuildings = new List<Actor>();

			// When there is extra power, check if AI can toggle on
			// TODO: captured disabled actors will never be toggled on
			if (power > 0 && toggled.Count > 0)
			{
				toggled.RemoveAll(bpw => !isTogglableValid(bpw.Actor));
				toggled.Sort((bpw1, bpw2) => bpw2.ExpectedPowerChanging.CompareTo(bpw1.ExpectedPowerChanging));
				for (var i = 0; i < toggled.Count; i++)
				{
					var bpw = toggled[i];
					if (power + bpw.ExpectedPowerChanging < 0)
						continue;

					togglingBuildings.Add(bpw.Actor);
					power += bpw.ExpectedPowerChanging;
					toggled.RemoveAt(i);
				}
			}

			// When there is no power, check if AI can toggle off
			// and add those toggled to list for toggling on
			else if (power < 0)
			{
				foreach (var bpw in GetOnlineBuildings())
				{
					if (power > 0)
						break;

					togglingBuildings.Add(bpw.Actor);
					toggled.Add(new ToggledPowerWrapper(bpw.Actor, -bpw.ExpectedPowerChanging));
					power += bpw.ExpectedPowerChanging;
				}
			}

			if (togglingBuildings.Count > 0)
				bot.QueueOrder(new Order(Info.PowerDownOrder, null, false, groupedActors: togglingBuildings.ToArray()));

			toggleTick = Info.Interval;
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			var data = new List<MiniYamlNode>();
			foreach (var tb in toggled.Where(td => isTogglableValid(td.Actor)))
				data.Add(new MiniYamlNode(FieldSaver.FormatValue(tb.Actor.ActorID), FieldSaver.FormatValue(tb.ExpectedPowerChanging)));

			return
			[
				new("ToggledBuildings", new MiniYaml("", data))
			];
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var nodes = data.ToDictionary();

			if (nodes.TryGetValue("ToggledBuildings", out var toggledBuildingsNode))
			{
				foreach (var n in toggledBuildingsNode.Nodes)
				{
					var a = self.World.GetActorById(FieldLoader.GetValue<uint>(n.Key, n.Key));

					if (isTogglableValid(a))
						toggled.Add(new ToggledPowerWrapper(a, FieldLoader.GetValue<int>(n.Key, n.Value.Value)));
				}
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			togglable.Dispose();
		}
	}
}
