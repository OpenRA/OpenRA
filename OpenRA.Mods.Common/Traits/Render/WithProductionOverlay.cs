#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
		ProductionQueue queue;
		bool buildComplete;

		bool IsProducing
		{
			get { return queue != null && queue.CurrentItem() != null && !queue.CurrentPaused; }
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

		void SelectQueue(Actor self)
		{
			var perBuildingQueues = self.TraitsImplementing<ProductionQueue>();
			queue = perBuildingQueues.FirstOrDefault(q => q.Enabled && production.Produces.Contains(q.Info.Type));

			if (queue == null)
			{
				var perPlayerQueues = self.Owner.PlayerActor.TraitsImplementing<ProductionQueue>();
				queue = perPlayerQueues.FirstOrDefault(q => q.Enabled && production.Produces.Contains(q.Info.Type));
			}

			if (queue == null)
				throw new InvalidOperationException("Can't find production queues.");
		}

		public void Created(Actor self)
		{
			if (buildComplete)
				SelectQueue(self);
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
			SelectQueue(self);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			self.World.AddFrameEndTask(w => SelectQueue(self));
		}

		public void Sold(Actor self) { }
		public void Selling(Actor self)
		{
			buildComplete = false;
		}

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}
	}
}