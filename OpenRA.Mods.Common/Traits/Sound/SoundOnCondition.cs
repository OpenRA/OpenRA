#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	[Desc("Sound to play when trait is enabled.")]
	public class SoundOnConditionInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		public readonly string Sound = null;

		[Desc("Set to false if trait is enabled at actor creation but sound should not play at that point.")]
		public readonly bool TriggerOnFirstEnable = true;

		[Desc("Should the sound play at actor position? If set to 'false', it will be audible on the entire map.")]
		public readonly bool PlayAtPosition = true;

		[Desc("Should the sound only be audible to the actor's owner?")]
		public readonly bool PlayOnlyToOwner = false;

		public override object Create(ActorInitializer init) { return new SoundOnCondition(init.Self, this); }
	}

	public class SoundOnCondition : ConditionalTrait<SoundOnConditionInfo>
	{
		bool skip;

		public SoundOnCondition(Actor self, SoundOnConditionInfo info)
			: base(info)
		{
			skip = !info.TriggerOnFirstEnable;
		}

		protected override void TraitEnabled(Actor self)
		{
			if (skip)
			{
				skip = false;
				return;
			}

			if (Info.PlayAtPosition)
			{
				if (Info.PlayOnlyToOwner)
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, Info.Sound, self.CenterPosition);
				else
					Game.Sound.Play(SoundType.World, Info.Sound, self.CenterPosition);
			}
			else
			{
				if (Info.PlayOnlyToOwner)
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, Info.Sound);
				else	
					Game.Sound.Play(SoundType.World, Info.Sound);
			}
		}
	}
}
