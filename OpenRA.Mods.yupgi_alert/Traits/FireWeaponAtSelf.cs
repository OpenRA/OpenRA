#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod.
 * Started from Mod.AS's ExplodesWeapon trait but not much left.
 * 
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/* Works without base engine modification */

namespace OpenRA.Mods.yupgi_alert.Traits
{
	[Desc("Fires one of its armament at the actor's position when enabled.")]
	public class FireWeaponAtSelfInfo : ConditionalTraitInfo, Requires<ArmamentInfo>
	{
		[WeaponReference]
		[Desc("The name of the weapon, one of its armament. Must be specified with \"Name:\" field.")]
		public readonly string Name = "primary";

		[Desc("Fire at this position.")]
		public readonly WVec LocalOffset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new FireWeaponAtSelf(init.Self, this); }
	}

	class FireWeaponAtSelf : ConditionalTrait<FireWeaponAtSelfInfo>, ITick
	{
		readonly FireWeaponAtSelfInfo info;
		readonly BodyOrientation body;
		readonly Armament[] armaments;
		readonly AttackBase attackBase;

		public FireWeaponAtSelf(Actor self, FireWeaponAtSelfInfo info)
			: base(info)
		{
			this.info = info;

			armaments = self.TraitsImplementing<Armament>().Where(a => a.Info.Name == info.Name).ToArray();
			System.Diagnostics.Debug.Assert(armaments.Length == 1, "Multiple armaments with given name detected: " + info.Name);
			body = self.TraitOrDefault<BodyOrientation>();

			// Not sure about attackbase selection. I assert there is only one active at once,
			// but then if we decide this at creation time... ugh.
			attackBase = self.TraitsImplementing<AttackBase>().Where(a => a.IsTraitEnabled()).First();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (armaments[0].IsReloading)
				return;

			var localoffset = body != null
				? body.LocalToWorld(info.LocalOffset.Rotate(body.QuantizeOrientation(self, self.Orientation)))
				: info.LocalOffset;

			attackBase.DoAttack(self, Target.FromPos(self.CenterPosition + localoffset), armaments);
		}
	}
}