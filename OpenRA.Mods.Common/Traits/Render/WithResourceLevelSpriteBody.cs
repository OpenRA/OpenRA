#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render trait for buildings that change the sprite according to the remaining resource storage capacity across all depots.")]
	public class WithResourceLevelSpriteBodyInfo : WithSpriteBodyInfo
	{
		[Desc("Internal resource stages. Does not have to match number of sequence frames.")]
		public readonly int Stages = 10;

		public override object Create(ActorInitializer init) { return new WithResourceLevelSpriteBody(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var anim = new Animation(init.World, image);
			anim.PlayFetchIndex(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence), () => 0);

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p, rs.Scale);
		}
	}

	public class WithResourceLevelSpriteBody : WithSpriteBody, INotifyOwnerChanged
	{
		readonly WithResourceLevelSpriteBodyInfo info;
		PlayerResources playerResources;

		public WithResourceLevelSpriteBody(ActorInitializer init, WithResourceLevelSpriteBodyInfo info)
			: base(init, info, () => 0)
		{
			this.info = info;
			playerResources = init.Self.Owner.PlayerActor.Trait<PlayerResources>();
			ConfigureAnimation(init.Self);
		}

		void ConfigureAnimation(Actor self)
		{
			DefaultAnimation.PlayFetchIndex(NormalizeSequence(self, Info.Sequence),
				() => playerResources.ResourceCapacity != 0
					? ((info.Stages * DefaultAnimation.CurrentSequence.Length - 1) * playerResources.Resources) / (info.Stages * playerResources.ResourceCapacity)
					: 0);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Do nothing - we just want to disable the default WithSpriteBody implementation
		}

		public override void CancelCustomAnimation(Actor self)
		{
			ConfigureAnimation(self);
		}
	}
}
