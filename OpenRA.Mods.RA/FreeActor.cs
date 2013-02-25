#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class FreeActorInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string Actor = null;
		public readonly string InitialActivity = null;
		public readonly int2 SpawnOffset = int2.Zero;
		public readonly int Facing = 0;

		public object Create( ActorInitializer init ) { return new FreeActor(init, this); }
	}

	public class FreeActor
	{
		public FreeActor(ActorInitializer init, FreeActorInfo info)
		{
			if (init.Contains<FreeActorInit>() && !init.Get<FreeActorInit>().value) return;

			init.self.World.AddFrameEndTask(
				w =>
				{
					var a = w.CreateActor(info.Actor, new TypeDictionary
					{
						new LocationInit( init.self.Location + (CVec)info.SpawnOffset ),
						new OwnerInit( init.self.Owner ),
						new FacingInit( info.Facing ),
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
}
