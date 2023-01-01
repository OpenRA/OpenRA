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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	class WithGunboatBodyInfo : WithSpriteBodyInfo, Requires<BodyOrientationInfo>, Requires<IFacingInfo>, Requires<TurretedInfo>
	{
		[Desc("Turreted 'Turret' key to display")]
		public readonly string Turret = "primary";

		[SequenceReference]
		public readonly string LeftSequence = "left";

		[SequenceReference]
		public readonly string RightSequence = "right";

		[SequenceReference]
		public readonly string WakeLeftSequence = "wake-left";

		[SequenceReference]
		public readonly string WakeRightSequence = "wake-right";

		public override object Create(ActorInitializer init) { return new WithGunboatBody(init, this); }
	}

	class WithGunboatBody : WithSpriteBody, ITick
	{
		readonly WithGunboatBodyInfo info;
		readonly Animation wake;
		readonly RenderSprites rs;
		readonly IFacing facing;
		readonly Turreted turret;

		static Func<WAngle> MakeTurretFacingFunc(Actor self)
		{
			// Turret artwork is baked into the sprite, so only the first turret makes sense.
			var turreted = self.TraitsImplementing<Turreted>().FirstOrDefault();
			return () => turreted.WorldOrientation.Yaw;
		}

		public WithGunboatBody(ActorInitializer init, WithGunboatBodyInfo info)
			: base(init, info, MakeTurretFacingFunc(init.Self))
		{
			this.info = info;
			rs = init.Self.Trait<RenderSprites>();
			facing = init.Self.Trait<IFacing>();
			var name = rs.GetImage(init.Self);
			turret = init.Self.TraitsImplementing<Turreted>()
				.First(t => t.Name == info.Turret);

			wake = new Animation(init.World, name);
			wake.PlayRepeating(info.WakeLeftSequence);
			rs.Add(new AnimationWithOffset(wake, null, null, -87));
		}

		protected override void TraitEnabled(Actor self)
		{
			base.TraitEnabled(self);
			turret.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}

		void ITick.Tick(Actor self)
		{
			if (facing.Facing.Angle <= 512)
			{
				var left = NormalizeSequence(self, info.LeftSequence);
				if (DefaultAnimation.CurrentSequence.Name != left)
					DefaultAnimation.ReplaceAnim(left);

				if (wake.CurrentSequence.Name != info.WakeLeftSequence)
					wake.ReplaceAnim(info.WakeLeftSequence);
			}
			else
			{
				var right = NormalizeSequence(self, info.RightSequence);
				if (DefaultAnimation.CurrentSequence.Name != right)
					DefaultAnimation.ReplaceAnim(right);

				if (wake.CurrentSequence.Name != info.WakeRightSequence)
					wake.ReplaceAnim(info.WakeRightSequence);
			}
		}
	}
}
