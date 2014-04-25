#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Player recives a unit for free once the building is placed. This also works for structures.",
		"If you want more than one unit to appear copy this section and assign IDs like FreeActor@2, ...")]
	public class FreeActorInfo : ITraitInfo
	{
		[ActorReference]
		[Desc("Name of actor (use HARV if this trait is for refineries)")]
		public readonly string Actor = null;
		[Desc("What the unit should start doing. Warning: If this is not a harvester", "it will break if you use FindResources.")]
		public readonly string InitialActivity = null;
		[Desc("Offset relative to structure-center in 2D (e.g. 1, 2)")]
		public readonly CVec SpawnOffset = CVec.Zero;
		[Desc("Which direction the unit should face.")]
		public readonly int Facing = 0;

		public object Create( ActorInitializer init ) { return new FreeActor(init, this); }
	}

	public class FreeActor
	{
		public FreeActor(ActorInitializer init, FreeActorInfo info)
		{
			if (init.Contains<FreeActorInit>() && !init.Get<FreeActorInit>().value)
				return;

			init.self.World.AddFrameEndTask(w =>
			{
				var a = w.CreateActor(info.Actor, new TypeDictionary
				{
					new ParentActorInit(init.self),
					new LocationInit(init.self.Location + info.SpawnOffset),
					new OwnerInit(init.self.Owner),
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
		public readonly bool value = true;
		public FreeActorInit() { }
		public FreeActorInit(bool init) { value = init; }
		public bool Value(World world) { return value; }
	}

	public class ParentActorInit : IActorInit<Actor>
	{
		public readonly Actor value;
		public ParentActorInit(Actor parent) { value = parent; }
		public Actor Value(World world) { return value; }
	}
}
