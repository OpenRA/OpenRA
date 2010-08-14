#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderUnitRotorInfo : RenderUnitInfo
	{
		public readonly int[] PrimaryOffset = { 0, 0 };
		public readonly int[] SecondaryOffset = null;

		public override object Create(ActorInitializer init) { return new RenderUnitRotor(init.self); }
	}

	class RenderUnitRotor : RenderUnit
	{
		public Animation rotorAnim, secondRotorAnim;

		public RenderUnitRotor( Actor self )
			: base(self)
		{
			var facing = self.Trait<IFacing>();
			var info = self.Info.Traits.Get<RenderUnitRotorInfo>();

			rotorAnim = new Animation(GetImage(self));
			rotorAnim.PlayRepeating("rotor");
			anims.Add("rotor_1", new AnimationWithOffset(
				rotorAnim,
				() => Combat.GetTurretPosition( self, facing, new Turret(info.PrimaryOffset)),
				null ) { ZOffset = 1 } );

			if (info.SecondaryOffset == null) return;

			secondRotorAnim = new Animation(GetImage(self));
			secondRotorAnim.PlayRepeating("rotor2");
			anims.Add("rotor_2", new AnimationWithOffset(
				secondRotorAnim,
				() => Combat.GetTurretPosition(self, facing, new Turret(info.SecondaryOffset)),
				null) { ZOffset = 1 });
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			
			var isFlying = self.Trait<IMove>().Altitude > 0;
			if (isFlying ^ (rotorAnim.CurrentSequence.Name != "rotor")) 
				return;

			rotorAnim.ReplaceAnim(isFlying ? "rotor" : "slow-rotor");
			if (secondRotorAnim != null)
				secondRotorAnim.ReplaceAnim(isFlying ? "rotor2" : "slow-rotor2");
		}
	}
}
