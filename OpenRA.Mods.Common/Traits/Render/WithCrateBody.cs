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
	class WithCrateBodyInfo : ITraitInfo, Requires<RenderSpritesInfo>, IRenderActorPreviewSpritesInfo
	{
		[Desc("Easteregg sequences to use in December.")]
		public readonly string[] XmasImages = { };

		[SequenceReference] public readonly string IdleSequence = "idle";
		[SequenceReference] public readonly string WaterSequence = null;
		[SequenceReference] public readonly string LandSequence = null;

		public object Create(ActorInitializer init) { return new WithCrateBody(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var anim = new Animation(init.World, rs.Image, () => 0);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), IdleSequence));
			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}
	}

	class WithCrateBody : INotifyParachuteLanded, INotifyAddedToWorld
	{
		readonly Actor self;
		readonly Animation anim;
		readonly WithCrateBodyInfo info;

		public WithCrateBody(Actor self, WithCrateBodyInfo info)
		{
			this.self = self;
			this.info = info;

			var rs = self.Trait<RenderSprites>();
			var image = rs.GetImage(self);
			var images = info.XmasImages.Any() && DateTime.Today.Month == 12 ? info.XmasImages : new[] { image };

			anim = new Animation(self.World, images.Random(Game.CosmeticRandom));
			anim.Play(info.IdleSequence);
			rs.Add(anim);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			// Don't change animations while still in air
			if (!self.IsAtGroundLevel())
				return;

			PlaySequence();
		}

		void INotifyParachuteLanded.OnLanded()
		{
			PlaySequence();
		}

		void PlaySequence()
		{
			var sequence = self.World.Map.GetTerrainInfo(self.Location).IsWater ? info.WaterSequence : info.LandSequence;
			if (!string.IsNullOrEmpty(sequence))
				anim.PlayRepeating(sequence);
		}
	}
}
