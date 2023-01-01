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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders an animation when the Production trait of the actor is activated.",
		"Works both with per player ClassicProductionQueue and per building ProductionQueue, but needs any of these.")]
	public class WithProductionOverlayInfo : PausableConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>, Requires<ProductionInfo>
	{
		[Desc("Queues that should be producing for this overlay to render.")]
		public readonly HashSet<string> Queues = new HashSet<string>();

		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "production-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithProductionOverlay(init.Self, this); }
	}

	public class WithProductionOverlay : PausableConditionalTrait<WithProductionOverlayInfo>, INotifyDamageStateChanged, INotifyCreated, INotifyOwnerChanged
	{
		readonly Actor self;
		readonly Animation overlay;
		readonly ProductionInfo[] productionInfos;
		ProductionQueue[] queues;

		bool IsProducing
		{
			get { return queues != null && queues.Any(q => q.Enabled && q.AllQueued().Any(i => !i.Paused && i.Started) && q.MostLikelyProducer().Actor == self); }
		}

		public WithProductionOverlay(Actor self, WithProductionOverlayInfo info)
			: base(info)
		{
			this.self = self;

			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			productionInfos = self.Info.TraitInfos<ProductionInfo>().ToArray();

			overlay = new Animation(self.World, rs.GetImage(self), () => IsTraitPaused);
			overlay.PlayRepeating(info.Sequence);

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self.Orientation))),
				() => !IsProducing || IsTraitDisabled);

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void CacheQueues(Actor self)
		{
			// Per-actor production
			queues = self.TraitsImplementing<ProductionQueue>()
				.Where(q => productionInfos.Any(p => p.Produces.Contains(q.Info.Type)))
				.Where(q => Info.Queues.Count == 0 || Info.Queues.Contains(q.Info.Type))
				.ToArray();

			if (queues.Length == 0)
			{
				// Player-wide production
				queues = self.Owner.PlayerActor.TraitsImplementing<ProductionQueue>()
					.Where(q => productionInfos.Any(p => p.Produces.Contains(q.Info.Type)))
					.Where(q => Info.Queues.Count == 0 || Info.Queues.Contains(q.Info.Type))
					.ToArray();
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			CacheQueues(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			self.World.AddFrameEndTask(w => CacheQueues(self));
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}
	}
}
