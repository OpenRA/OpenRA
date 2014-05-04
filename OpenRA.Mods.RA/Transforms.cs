#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class TransformsInfo : ITraitInfo
	{
		[ActorReference] public readonly string IntoActor = null;
		public readonly int2 Offset = int2.Zero;
		public readonly int Facing = 96;
		public readonly string[] TransformSounds = {};
		public readonly string[] NoTransformSounds = {};

		public virtual object Create(ActorInitializer init) { return new Transforms(init.self, this); }
	}

	class Transforms : IIssueOrder, IResolveOrder, IOrderVoice
	{
		Actor self;
		TransformsInfo Info;
		BuildingInfo bi;

		public Transforms(Actor self, TransformsInfo info)
		{
			this.self = self;
			Info = info;
			bi = self.World.Map.Rules.Actors[info.IntoActor].Traits.GetOrDefault<BuildingInfo>();
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "DeployTransform") ? "Move" : null;
		}

		bool CanDeploy()
		{
			var b = self.TraitOrDefault<Building>();
			if (b != null && b.Locked)
				return false;

			return (bi == null || self.World.CanPlaceBuilding(Info.IntoActor, bi, self.Location + (CVec)Info.Offset, self));
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter( "DeployTransform", 5, () => CanDeploy() ); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "DeployTransform" )
				return new Order( order.OrderID, self, queued );

			return null;
		}

		public void DeployTransform(bool queued)
		{
			var b = self.TraitOrDefault<Building>();

			if (!CanDeploy() || (b != null && !b.Lock()))
			{
				foreach (var s in Info.NoTransformSounds)
					Sound.PlayToPlayer(self.Owner, s);
				return;
			}

			if (!queued)
				self.CancelActivity();

			if (self.HasTrait<IFacing>())
				self.QueueActivity(new Turn(Info.Facing));

			var rb = self.TraitOrDefault<RenderBuilding>();
			if (rb != null && self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation)
				self.QueueActivity(new MakeAnimation(self, true, () => rb.PlayCustomAnim(self, "make")));

			self.QueueActivity(new Transform(self, Info.IntoActor) { Offset = (CVec)Info.Offset, Facing = Info.Facing, Sounds = Info.TransformSounds });
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if (order.OrderString == "DeployTransform")
				DeployTransform(order.Queued);
		}
	}
}
