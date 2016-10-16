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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithAimAnimationInfo : UpgradableTraitInfo, Requires<WithSpriteBodyInfo>, Requires<ArmamentInfo>, Requires<AttackBaseInfo>
	{
		[Desc("Armament name")]
		public readonly string Armament = "primary";

		[Desc("Displayed while targeting.")]
		[SequenceReference] public readonly string AimSequence = null;

		[Desc("Shown while reloading.")]
		[SequenceReference(null, true)] public readonly string ReloadPrefix = null;

		public override object Create(ActorInitializer init) { return new WithAimAnimation(init, this); }
	}

	public class WithAimAnimation : UpgradableTrait<WithAimAnimationInfo>, ITick, INotifyCreated
	{
		readonly AttackBase attack;
		readonly Armament armament;
		readonly WithSpriteBody[] wsbs;

		public WithAimAnimation(ActorInitializer init, WithAimAnimationInfo info)
			: base(info)
		{
			attack = init.Self.Trait<AttackBase>();
			armament = init.Self.TraitsImplementing<Armament>()
				.Single(a => a.Info.Name == info.Armament);
			wsbs = init.Self.TraitsImplementing<WithSpriteBody>().ToArray();
		}

		void INotifyCreated.Created(Actor self)
		{
			// The way this trait currently works will cancel/override attack animations early,
			// so prevent modders from enabling both at the same time by throwing an exception.
			// If they enable one of them later via upgrade, they do so at their own risk.
			// TODO: Refactor WithSpriteBody and its modifiers in a way that makes WithAimAnimation and WithAttackAnimation compatible with each other.
			var waa = self.TraitsImplementing<WithAttackAnimation>();
			if (!IsTraitDisabled && waa.Any(Exts.IsTraitEnabled))
				throw new YamlException("Actor {0}: WithAimAnimation and WithAttackAnimation must not be enabled at the same time.".F(self.Info.Name));
		}

		void PlayAimAnimation()
		{
			if (string.IsNullOrEmpty(Info.AimSequence) && string.IsNullOrEmpty(Info.ReloadPrefix))
				return;

			foreach (var wsb in wsbs)
			{
				if (wsb.IsTraitDisabled)
					continue;

				var sequence = wsb.Info.Sequence;
				if (!string.IsNullOrEmpty(Info.AimSequence) && attack.IsAttacking)
					sequence = Info.AimSequence;

				var prefix = (armament.IsReloading && !string.IsNullOrEmpty(Info.ReloadPrefix)) ? Info.ReloadPrefix : "";

				if (!string.IsNullOrEmpty(prefix) && sequence != (prefix + sequence))
					sequence = prefix + sequence;

				wsb.DefaultAnimation.ReplaceAnim(sequence);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			PlayAimAnimation();
		}
	}
}
