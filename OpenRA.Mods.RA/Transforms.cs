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
	[Desc("Actor becomes a specified actor type when this trait is triggered.")]
	class TransformsInfo : ITraitInfo
	{
		[ActorReference] public readonly string IntoActor = null;
		public readonly CVec Offset = CVec.Zero;
		public readonly int Facing = 96;
		public readonly string[] TransformSounds = { };
		public readonly string[] NoTransformSounds = { };

		public virtual object Create(ActorInitializer init) { return new Transforms(init, this); }
	}

	class Transforms : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		readonly TransformsInfo info;
		readonly BuildingInfo bi;
		readonly string race;

		public Transforms(ActorInitializer init, TransformsInfo info)
		{
			self = init.self;
			this.info = info;
			bi = self.World.Map.Rules.Actors[info.IntoActor].Traits.GetOrDefault<BuildingInfo>();
			race = init.Contains<RaceInit>() ? init.Get<RaceInit, string>() : self.Owner.Country.Race;
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

			return bi == null || self.World.CanPlaceBuilding(info.IntoActor, bi, self.Location + info.Offset, self);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeployOrderTargeter("DeployTransform", 5, () => CanDeploy()); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "DeployTransform")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		public void DeployTransform(bool queued)
		{
			var b = self.TraitOrDefault<Building>();

			if (!CanDeploy() || (b != null && !b.Lock()))
			{
				foreach (var s in info.NoTransformSounds)
					Sound.PlayToPlayer(self.Owner, s);

				return;
			}

			if (!queued)
				self.CancelActivity();

			if (self.HasTrait<IFacing>())
				self.QueueActivity(new Turn(self, info.Facing));

			foreach (var nt in self.TraitsImplementing<INotifyTransform>())
				nt.BeforeTransform(self);

			var transform = new Transform(self, info.IntoActor) { Offset = info.Offset, Facing = info.Facing, Sounds = info.TransformSounds, Race = race };
			var makeAnimation = self.TraitOrDefault<WithMakeAnimation>();
			if (makeAnimation != null)
				makeAnimation.Reverse(self, transform);
			else
				self.QueueActivity(transform);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DeployTransform")
				DeployTransform(order.Queued);
		}
	}
}
