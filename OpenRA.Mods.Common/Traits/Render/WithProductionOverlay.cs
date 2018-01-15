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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders an animation when the Production trait of the actor is activated.",
		"Works both with per player ClassicProductionQueue and per building ProductionQueue, but needs any of these.")]
	public class WithProductionOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>, Requires<ProductionInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "production-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithProductionOverlay(init.Self, this); }
	}

	public class WithProductionOverlay : INotifyDamageStateChanged, INotifyCreated, INotifyBuildComplete, INotifySold, INotifyOwnerChanged
	{
		readonly Animation overlay;
		readonly ProductionInfo production;
		ProductionQueue[] queues;
		bool buildComplete;

		bool IsProducing
		{
			get { return queues != null && queues.Any(q => q.Enabled && q.CurrentItem() != null && !q.CurrentPaused); }
		}

		public WithProductionOverlay(Actor self, WithProductionOverlayInfo info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units
			production = self.Info.TraitInfo<ProductionInfo>();

			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.PlayRepeating(info.Sequence);

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => !IsProducing || !buildComplete);

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void CacheQueues(Actor self)
		{
			// Per-actor production
			queues = self.TraitsImplementing<ProductionQueue>()
				.Where(q => production.Produces.Contains(q.Info.Type))
				.ToArray();

			if (!queues.Any())
			{
				// Player-wide production
				queues = self.Owner.PlayerActor.TraitsImplementing<ProductionQueue>()
					.Where(q => production.Produces.Contains(q.Info.Type))
					.ToArray();
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			if (buildComplete)
				CacheQueues(self);
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			buildComplete = true;
			CacheQueues(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			self.World.AddFrameEndTask(w => CacheQueues(self));
		}

		void INotifySold.Sold(Actor self) { }
		void INotifySold.Selling(Actor self)
		{
			buildComplete = false;
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}
	}
}