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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class WithCargoSoundsInfo : ConditionalTraitInfo, Requires<CargoInfo>
	{
		[NotificationReference("Speech")]
		[Desc("Speech notification played when a passenger enters this actor.")]
		public readonly string EnterNotification = null;

		[NotificationReference("Speech")]
		[Desc("Speech notification played when a passenger leaves this actor.")]
		public readonly string ExitNotification = null;

		[Desc("List of sounds which a random one is played when the a passenger enters this actor.")]
		public readonly string[] EnterSounds = { };

		[Desc("List of sounds which a random one is played when the a passenger exits this actor.")]
		public readonly string[] ExitSounds = { };

		public override object Create(ActorInitializer init) { return new WithCargoSounds(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (EnterNotification == null && ExitNotification == null && !EnterSounds.Any() && !ExitSounds.Any())
				throw new YamlException("Actor '{0}' has WithCargoSounds trait, but doesn't define any sounds or notifications.".F(ai.Name));
		}
	}

	public class WithCargoSounds : ConditionalTrait<WithCargoSoundsInfo>, INotifyPassengerEntered, INotifyPassengerExited
	{
		public WithCargoSounds(Actor self, WithCargoSoundsInfo info)
            : base(info) { }

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			if (IsTraitDisabled)
				return;

			if (Info.EnterSounds.Any())
				Game.Sound.Play(SoundType.World, Info.EnterSounds.Random(self.World.LocalRandom), self.CenterPosition);
			Game.Sound.PlayNotification(self.World.Map.Rules, passenger.Owner, "Speech", Info.EnterNotification, passenger.Owner.Faction.InternalName);
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			if (IsTraitDisabled)
				return;

			if (Info.ExitSounds.Any())
				Game.Sound.Play(SoundType.World, Info.ExitSounds.Random(self.World.LocalRandom), self.CenterPosition);
			Game.Sound.PlayNotification(self.World.Map.Rules, passenger.Owner, "Speech", Info.ExitNotification, passenger.Owner.Faction.InternalName);
		}
	}
}
