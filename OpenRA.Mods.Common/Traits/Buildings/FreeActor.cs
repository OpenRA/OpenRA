#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Player recives a unit for free once the building is placed. This also works for structures.",
		"If you want more than one unit to appear copy this section and assign IDs like FreeActor@2, ...")]
	public class FreeActorInfo : ITraitInfo
	{
		[ActorReference]
		[Desc("Name of the actor.")]
		public readonly string Actor = null;

		[Desc("What the unit should start doing. Warning: If this is not a harvester", "it will break if you use FindResources.")]
		public readonly string InitialActivity = null;

		[Desc("Offset relative to the top-left cell of the building.")]
		public readonly CVec SpawnOffset = CVec.Zero;

		[Desc("Which direction the unit should face.")]
		public readonly int Facing = 0;

		public virtual object Create(ActorInitializer init) { return new FreeActor(init, this); }
	}

	public class FreeActor
	{
		public FreeActor(ActorInitializer init, FreeActorInfo info)
		{
			if (init.Contains<FreeActorInit>() && !init.Get<FreeActorInit>().ActorValue)
				return;

			init.Self.World.AddFrameEndTask(w =>
			{
				var a = w.CreateActor(info.Actor, new TypeDictionary
				{
					new ParentActorInit(init.Self),
					new LocationInit(init.Self.Location + info.SpawnOffset),
					new OwnerInit(init.Self.Owner),
					new FacingInit(info.Facing),
				});

				if (info.InitialActivity != null)
					a.QueueActivity(Game.CreateObject<Activity>(info.InitialActivity));
			});
		}
	}

	public class FreeActorInit : IActorInit<bool>
	{
		[FieldFromYamlKey]
		public readonly bool ActorValue = true;
		public FreeActorInit() { }
		public FreeActorInit(bool init) { ActorValue = init; }
		public bool Value(World world) { return ActorValue; }
	}

	public class ParentActorInit : IActorInit<Actor>
	{
		public readonly Actor ActorValue;
		public ParentActorInit(Actor parent) { ActorValue = parent; }
		public Actor Value(World world) { return ActorValue; }
	}
}
