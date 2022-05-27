#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor's experience increases when it has killed a GivesExperience actor.")]
	public class GainsExperienceInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Condition to grant at each level.",
			"Key is the XP requirements for each level as a percentage of our own value.",
			"Value is the condition to grant.")]
		public readonly Dictionary<int, string> Conditions = null;

		[GrantedConditionReference]
		public IEnumerable<string> LinterConditions => Conditions.Values;

		[Desc("Image for the level up sprite.")]
		public readonly string LevelUpImage = null;

		[SequenceReference(nameof(LevelUpImage), allowNullImage: true)]
		[Desc("Sequence for the level up sprite. Needs to be present on LevelUpImage.")]
		public readonly string LevelUpSequence = "levelup";

		[PaletteReference]
		[Desc("Palette for the level up sprite.")]
		public readonly string LevelUpPalette = "effect";

		[Desc("Multiplier to apply to the Conditions keys. Defaults to the actor's value.")]
		public readonly int ExperienceModifier = -1;

		[Desc("Should the level-up animation be suppressed when actor is created?")]
		public readonly bool SuppressLevelupAnimation = true;

		[NotificationReference("Sounds")]
		public readonly string LevelUpNotification = null;

		public readonly string LevelUpTextNotification = null;

		public override object Create(ActorInitializer init) { return new GainsExperience(init, this); }
	}

	public class GainsExperience : INotifyCreated, ISync, IResolveOrder, ITransformActorInitModifier
	{
		readonly Actor self;
		readonly GainsExperienceInfo info;
		readonly int initialExperience;

		readonly List<(int RequiredExperience, string Condition)> nextLevel = new List<(int, string)>();

		// Stored as a percentage of our value
		[Sync]
		public int Experience { get; private set; }

		[Sync]
		public int Level { get; private set; }
		public readonly int MaxLevel;

		public GainsExperience(ActorInitializer init, GainsExperienceInfo info)
		{
			self = init.Self;
			this.info = info;

			Experience = 0;
			MaxLevel = info.Conditions.Count;
			initialExperience = init.GetValue<ExperienceInit, int>(info, 0);
		}

		void INotifyCreated.Created(Actor self)
		{
			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
			var requiredExperience = info.ExperienceModifier < 0 ? (valued != null ? valued.Cost : 1) : info.ExperienceModifier;
			foreach (var kv in info.Conditions)
				nextLevel.Add((kv.Key * requiredExperience, kv.Value));

			if (initialExperience > 0)
				GiveExperience(initialExperience, info.SuppressLevelupAnimation);
		}

		public bool CanGainLevel => Level < MaxLevel;

		public void GiveLevels(int numLevels, bool silent = false)
		{
			if (MaxLevel == 0)
				return;

			var newLevel = Math.Min(Level + numLevels, MaxLevel);
			GiveExperience(nextLevel[newLevel - 1].RequiredExperience - Experience, silent);
		}

		public void GiveExperience(int amount, bool silent = false)
		{
			if (amount < 0)
				throw new ArgumentException("Revoking experience is not implemented.", nameof(amount));

			if (MaxLevel == 0)
				return;

			Experience = (Experience + amount).Clamp(0, nextLevel[MaxLevel - 1].RequiredExperience);

			while (Level < MaxLevel && Experience >= nextLevel[Level].RequiredExperience)
			{
				self.GrantCondition(nextLevel[Level].Condition);

				Level++;

				if (!silent)
				{
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", info.LevelUpNotification, self.Owner.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(info.LevelUpTextNotification, self.Owner);

					if (info.LevelUpImage != null && info.LevelUpSequence != null)
						self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self, w, info.LevelUpImage, info.LevelUpSequence, info.LevelUpPalette)));
				}
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DevLevelUp")
			{
				var developerMode = self.Owner.PlayerActor.Trait<DeveloperMode>();
				if (!developerMode.Enabled)
					return;

				if ((int)order.ExtraData > 0)
					GiveLevels((int)order.ExtraData);
				else
					GiveLevels(1);
			}
		}

		void ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new ExperienceInit(info, Experience));
		}
	}

	class ExperienceInit : ValueActorInit<int>
	{
		public ExperienceInit(TraitInfo info, int value)
			: base(info, value) { }
	}
}
