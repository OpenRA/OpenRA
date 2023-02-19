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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays the fill status of PlayerResources with an extra sprite overlay on the actor.")]
	class WithResourceLevelOverlayInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "resources";

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name.")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithResourceLevelOverlay(init.Self, this); }
	}

	class WithResourceLevelOverlay : ConditionalTrait<WithResourceLevelOverlayInfo>, INotifyOwnerChanged, INotifyDamageStateChanged
	{
		readonly AnimationWithOffset anim;
		readonly RenderSprites rs;
		readonly WithSpriteBody wsb;

		PlayerResources playerResources;

		public WithResourceLevelOverlay(Actor self, WithResourceLevelOverlayInfo info)
			: base(info)
		{
			rs = self.Trait<RenderSprites>();
			wsb = self.Trait<WithSpriteBody>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();

			var a = new Animation(self.World, rs.GetImage(self));
			a.PlayFetchIndex(info.Sequence, () =>
				playerResources.ResourceCapacity != 0 ?
				(10 * a.CurrentSequence.Length - 1) * playerResources.Resources / (10 * playerResources.ResourceCapacity) :
				0);

			anim = new AnimationWithOffset(a, null, () => IsTraitDisabled, 1024);
			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (anim.Animation.CurrentSequence != null)
				anim.Animation.ReplaceAnim(wsb.NormalizeSequence(self, Info.Sequence));
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}
	}
}
