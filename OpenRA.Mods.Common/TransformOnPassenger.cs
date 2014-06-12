#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class TransformOnPassengerInfo : ITraitInfo
	{
		[ActorReference] public readonly string[] PassengerTypes = {};
		[ActorReference] public readonly string OnEnter = null;
		[ActorReference] public readonly string OnExit = null;
		public readonly bool SkipMakeAnims = false;
		public readonly bool BecomeNeutral = false;

		public object Create(ActorInitializer init) { return new TransformOnPassenger(this); }
	}

	public class TransformOnPassenger : INotifyPassengerEntered, INotifyPassengerExited
	{
		TransformOnPassengerInfo info;

		public TransformOnPassenger(TransformOnPassengerInfo info) { this.info = info; }

		void MaybeTransform(Actor self, Actor passenger, string transformTo)
		{
			if (info.PassengerTypes.Contains(passenger.Info.Name) && transformTo != null)
			{
				self.World.AddFrameEndTask( w =>
				{
					var facing = self.TraitOrDefault<IFacing>();
					var transform = new Transform(self, transformTo) { SkipMakeAnims = info.SkipMakeAnims };
					if (facing != null) transform.Facing = facing.Facing;

					self.CancelActivity();
					self.QueueActivity(transform);
					if (info.BecomeNeutral) self.ChangeOwner(self.World.WorldActor.Owner);
				});
			}
		}

		public void PassengerEntered(Actor self, Actor passenger) { MaybeTransform(self, passenger, info.OnEnter); }
		public void PassengerExited(Actor self, Actor passenger) { MaybeTransform(self, passenger, info.OnExit); }
	}
}
