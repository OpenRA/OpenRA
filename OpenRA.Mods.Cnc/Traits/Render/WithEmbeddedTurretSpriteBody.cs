#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
		public override object Create(ActorInitializer init) { return new WithEmbeddedTurretSpriteBody(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var t = init.Actor.TraitInfos<TurretedInfo>().FirstOrDefault();
			var wsb = init.Actor.TraitInfos<WithSpriteBodyInfo>().FirstOrDefault();

			// Show the correct turret facing
			var anim = new Animation(init.World, image, () => t.InitialFacing);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), wsb.Sequence));

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p, rs.Scale);
		}
	}

	public class WithEmbeddedTurretSpriteBody : WithSpriteBody
	{
		readonly Turreted turreted;

		static Func<int> MakeTurretFacingFunc(Actor self)
		{
			// Turret artwork is baked into the sprite, so only the first turret makes sense.
			var turreted = self.TraitsImplementing<Turreted>().FirstOrDefault();
			return () => turreted.TurretFacing;
		}

		public WithEmbeddedTurretSpriteBody(ActorInitializer init, WithSpriteBodyInfo info)
			: base(init, info, MakeTurretFacingFunc(init.Self))
		{
			turreted = init.Self.TraitsImplementing<Turreted>().FirstOrDefault();
			turreted.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}

		protected override void DamageStateChanged(Actor self)
		{
			base.DamageStateChanged(self);
			turreted.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}
	}
}
