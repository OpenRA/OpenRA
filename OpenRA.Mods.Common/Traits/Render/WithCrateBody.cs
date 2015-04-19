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
	[Desc("Renders crates with both water and land variants.")]
	class WithCrateBodyInfo : ITraitInfo, Requires<RenderSpritesInfo>, IQuantizeBodyOrientationInfo, IRenderActorPreviewSpritesInfo
	{
		public readonly string[] Images = { "crate" };

		[Desc("Easteregg sequences to use in december.")]
		public readonly string[] XmasImages = { };

		public object Create(ActorInitializer init) { return new WithCrateBody(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var anim = new Animation(init.World, Images.First(), () => 0);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), "idle"));
			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}

		public int QuantizedBodyFacings(ActorInfo ai, SequenceProvider sequenceProvider, string race) { return 1; }
	}

	class WithCrateBody : INotifyParachuteLanded
	{
		readonly Actor self;
		readonly Animation anim;

		public WithCrateBody(Actor self, WithCrateBodyInfo info)
		{
			this.self = self;
			var rs = self.Trait<RenderSprites>();
			var images = info.XmasImages.Any() && DateTime.Today.Month == 12 ? info.XmasImages : info.Images;
			anim = new Animation(self.World, images.Random(Game.CosmeticRandom));
			anim.Play("idle");
			rs.Add(anim);
		}

		public void OnLanded()
		{
			var seq = self.World.Map.GetTerrainInfo(self.Location).IsWater ? "water" : "land";
			anim.PlayRepeating(seq);
		}
	}
}
