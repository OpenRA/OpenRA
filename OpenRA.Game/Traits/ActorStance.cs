#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.FileFormats;
namespace OpenRA.Traits
{
	class ActorStanceInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new ActorStance(init); }
	}
	
	public class ActorStance
	{
		// Stances modify default actor behavior
		public enum Stance
		{
			None, 
			Guard, // Stay near an actor/area; attack anything that comes near
			Defend, // Come running if a bad guy comes in range of the defendee
			Hunt, // Go searching for things to kill; will stray from move orders etc to follow target
			Retreat // Actively avoid things which might kill me
		}

		// Doesn't do anything... yet
		public ActorStance(ActorInitializer init) {}
	}
	
	public class ActorStanceInit : IActorInit<ActorStance.Stance>
	{
		[FieldFromYamlKey]
		public readonly ActorStance.Stance value = ActorStance.Stance.None;
		
		public ActorStanceInit() { }
		
		public ActorStanceInit( ActorStance.Stance init )
		{
			value = init;
		}
		
		public ActorStance.Stance Value( World world )
		{
			return value;	
		}
	}
}
