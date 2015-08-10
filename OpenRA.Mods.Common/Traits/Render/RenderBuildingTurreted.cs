#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class RenderBuildingTurretedInfo : RenderBuildingInfo, Requires<TurretedInfo>, InitializeAfter<TurretedInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingTurreted(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var t = init.Actor.Traits.WithInterface<TurretedInfo>()
				.FirstOrDefault();

			// Show the correct turret facing
			var anim = new Animation(init.World, image, () => t.InitialFacing);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}
	}

	class RenderBuildingTurreted : RenderBuilding
	{
		readonly Turreted turreted;

		static Func<int> MakeTurretFacingFunc(Actor self)
		{
			// Turret artwork is baked into the sprite, so only the first turret makes sense.
			var turreted = self.FirstTraitOrDefault<Turreted>();
			return () => turreted.TurretFacing;
		}

		public RenderBuildingTurreted(ActorInitializer init, RenderBuildingInfo info)
			: base(init, info, MakeTurretFacingFunc(init.Self))
		{
			turreted = init.Self.FirstTraitOrDefault<Turreted>();
			turreted.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}

		public override void DamageStateChanged(Actor self, AttackInfo e)
		{
			base.DamageStateChanged(self, e);
			turreted.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}
	}
}
