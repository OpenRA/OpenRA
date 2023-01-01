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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	[Desc("This actor has turret art with facings baked into the sprite.")]
	public class WithEmbeddedTurretSpriteBodyInfo : WithSpriteBodyInfo, Requires<TurretedInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Number of facings for gameplay calculations. -1 indicates auto-detection from the sequence.")]
		public readonly int QuantizedFacings = -1;

		public override object Create(ActorInitializer init) { return new WithEmbeddedTurretSpriteBody(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var t = init.Actor.TraitInfos<TurretedInfo>().FirstOrDefault();
			var wsb = init.Actor.TraitInfos<WithSpriteBodyInfo>().FirstOrDefault();

			// Show the correct turret facing
			var anim = new Animation(init.World, image, t.WorldFacingFromInit(init));
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), wsb.Sequence));

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p);
		}
	}

	public class WithEmbeddedTurretSpriteBody : WithSpriteBody
	{
		readonly WithEmbeddedTurretSpriteBodyInfo info;
		readonly Turreted turreted;

		static Func<WAngle> MakeTurretFacingFunc(Actor self)
		{
			// Turret artwork is baked into the sprite, so only the first turret makes sense.
			var turreted = self.TraitsImplementing<Turreted>().FirstOrDefault();
			return () => turreted.WorldOrientation.Yaw;
		}

		public WithEmbeddedTurretSpriteBody(ActorInitializer init, WithEmbeddedTurretSpriteBodyInfo info)
			: base(init, info, MakeTurretFacingFunc(init.Self))
		{
			this.info = info;
			turreted = init.Self.TraitsImplementing<Turreted>().FirstOrDefault();
		}

		protected override void TraitEnabled(Actor self)
		{
			base.TraitEnabled(self);
			turreted.QuantizedFacings = info.QuantizedFacings >= 0 ? info.QuantizedFacings : DefaultAnimation.CurrentSequence.Facings;
		}

		protected override void DamageStateChanged(Actor self)
		{
			base.DamageStateChanged(self);
			turreted.QuantizedFacings = info.QuantizedFacings >= 0 ? info.QuantizedFacings : DefaultAnimation.CurrentSequence.Facings;
		}
	}
}
