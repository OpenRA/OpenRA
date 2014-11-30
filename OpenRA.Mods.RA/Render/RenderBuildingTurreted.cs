#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingTurretedInfo : RenderBuildingInfo, Requires<TurretedInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingTurreted(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var t = init.Actor.Traits.WithInterface<TurretedInfo>()
				.FirstOrDefault();
	
			// Show the correct turret facing
			var anim = new Animation(init.World, image, () => t.InitialFacing);
			anim.PlayRepeating("idle");

			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}
	}

	class RenderBuildingTurreted : RenderBuilding
	{
		Turreted t;

		static Func<int> MakeTurretFacingFunc(Actor self)
		{
			// Turret artwork is baked into the sprite, so only the first turret makes sense.
			var turreted = self.TraitsImplementing<Turreted>().FirstOrDefault();
			return () => turreted.TurretFacing;
		}

		public RenderBuildingTurreted(ActorInitializer init, RenderBuildingInfo info)
			: base(init, info, MakeTurretFacingFunc(init.self))
		{
			t = init.self.TraitsImplementing<Turreted>().FirstOrDefault();
			t.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}

		public override void DamageStateChanged(Actor self, AttackInfo e)
		{
			base.DamageStateChanged(self, e);
			t.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
		}
	}
}
