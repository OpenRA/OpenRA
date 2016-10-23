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

namespace OpenRA.Mods.Common.Traits
{
	public class UpgradeOnMovementInfo : UpgradableTraitInfo, Requires<UpgradeManagerInfo>, Requires<IMoveInfo>
	{
		[UpgradeGrantedReference, FieldLoader.Require]
		[Desc("The upgrades to grant.")]
		public readonly string[] Upgrades = { };

		[Desc("Apply upgrades on straight vertical movement as well.")]
		public readonly bool ConsiderVerticalMovement = false;

		public override object Create(ActorInitializer init) { return new UpgradeOnMovement(init.Self, this); }
	}

	public class UpgradeOnMovement : UpgradableTrait<UpgradeOnMovementInfo>, ITick
	{
		readonly UpgradeManager manager;
		readonly IMove movement;

		bool granted;

		public UpgradeOnMovement(Actor self, UpgradeOnMovementInfo info)
			: base(info)
		{
			manager = self.Trait<UpgradeManager>();
			movement = self.Trait<IMove>();
		}

		void Revoke(Actor self)
		{
			if (!granted)
				return;

			foreach (var up in Info.Upgrades)
				manager.RevokeUpgrade(self, up, this);

			granted = false;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
			{
				Revoke(self);
				return;
			}

			var isMovingVertically = Info.ConsiderVerticalMovement ? movement.IsMovingVertically : false;
			var isMoving = !self.IsDead && (movement.IsMoving || isMovingVertically);
			if (isMoving && !granted)
			{
				foreach (var up in Info.Upgrades)
					manager.GrantUpgrade(self, up, this);

				granted = true;
			}
			else if (!isMoving)
				Revoke(self);
		}
	}
}
