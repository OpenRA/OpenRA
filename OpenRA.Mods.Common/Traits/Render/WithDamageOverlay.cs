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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders an overlay when the actor is taking heavy damage.")]
	public class WithDamageOverlayInfo : TraitInfo, Requires<RenderSpritesInfo>, IRulesetLoaded
	{
		public readonly string Image = "smoke_m";

		[SequenceReference(nameof(Image))]
		public readonly string IdleSequence = "idle";

		[SequenceReference(nameof(Image))]
		public readonly string LoopSequence = "loop";

		[SequenceReference(nameof(Image))]
		public readonly string EndSequence = "end";

		[Desc("Position relative to the body orientation.")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name.")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Damage types that this should be used for (defined on the warheads).",
			"Leave empty to disable all filtering.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[Desc("Trigger when Undamaged, Light, Medium, Heavy, Critical or Dead.")]
		public readonly DamageState MinimumDamageState = DamageState.Heavy;
		public readonly DamageState MaximumDamageState = DamageState.Dead;

		public override object Create(ActorInitializer init) { return new WithDamageOverlay(init.Self, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (Offset != WVec.Zero && !info.HasTraitInfo<BodyOrientationInfo>())
				throw new YamlException("Specifying WithDamageOverlay.Offset requires the BodyOrientation trait on the actor.");
		}
	}

	public class WithDamageOverlay : INotifyDamage, INotifyCreated
	{
		readonly WithDamageOverlayInfo info;
		readonly Animation anim;

		bool isSmoking;

		public WithDamageOverlay(Actor self, WithDamageOverlayInfo info)
		{
			this.info = info;
			anim = new Animation(self.World, info.Image);
		}

		void INotifyCreated.Created(Actor self)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.TraitOrDefault<BodyOrientation>();

			WVec AnimationOffset() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self.Orientation)));
			rs.Add(new AnimationWithOffset(anim, info.Offset == WVec.Zero || body == null ? null : AnimationOffset, () => !isSmoking),
				info.Palette, info.IsPlayerPalette);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!info.DamageTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(info.DamageTypes))
				return;

			if (isSmoking) return;
			if (e.Damage.Value < 0) return; /* getting healed */
			if (e.DamageState < info.MinimumDamageState) return;
			if (e.DamageState > info.MaximumDamageState) return;

			isSmoking = true;
			anim.PlayThen(info.IdleSequence,
				() => anim.PlayThen(info.LoopSequence,
					() => anim.PlayThen(info.EndSequence,
						() => isSmoking = false)));
		}
	}
}
