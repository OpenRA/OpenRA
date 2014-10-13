#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.RA.Activities;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ExposedForDestroyedActors]
	[ScriptPropertyGroup("General")]
	public class BaseActorProperties : ScriptActorProperties
	{
		// Note: This class must not make any trait queries so that this
		// remains safe to call on dead actors.
		public BaseActorProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Specifies whether the actor is in the world.")]
		public bool IsInWorld
		{
			get
			{
				return self.IsInWorld;
			}

			set
			{
				if (value)
					self.World.AddFrameEndTask(w => w.Add(self));
				else
					self.World.AddFrameEndTask(w => w.Remove(self));
			}
		}

		[Desc("Specifies whether the actor is alive or dead.")]
		public bool IsDead { get { return self.IsDead(); } }

		[Desc("Specifies whether the actor is idle (not performing any activities).")]
		public bool IsIdle { get { return self.IsIdle; } }

		[Desc("The player that owns the actor.")]
		public Player Owner
		{
			get
			{
				return self.Owner;
			}

			set
			{
				if (self.Owner != value)
					self.ChangeOwner(value);
			}
		}

		[Desc("The type of the actor (e.g. \"e1\").")]
		public string Type { get { return self.Info.Name; } }

		[Desc("Test whether an actor has a specific property.")]
		public bool HasProperty(string name)
		{
			return self.HasScriptProperty(name);
		}
	}

	[ScriptPropertyGroup("General")]
	public class GeneralProperties : ScriptActorProperties
	{
		readonly IFacing facing;
		readonly AutoTarget autotarget;

		public GeneralProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			facing = self.TraitOrDefault<IFacing>();
			autotarget = self.TraitOrDefault<AutoTarget>();
		}

		[Desc("The actor position in cell coordinates.")]
		public CPos Location { get { return self.Location; } }

		[Desc("The actor position in world coordinates.")]
		public WPos CenterPosition { get { return self.CenterPosition; } }

		[Desc("The direction that the actor is facing.")]
		public int Facing
		{
			get
			{
				if (facing == null)
					throw new LuaException("Actor '{0}' doesn't define a facing".F(self));

				return facing.Facing;
			}
		}

		[ScriptActorPropertyActivity]
		[Desc("Instantly moves the actor to the specified cell.")]
		public void Teleport(CPos cell)
		{
			self.QueueActivity(new SimpleTeleport(cell));
		}

		[ScriptActorPropertyActivity]
		[Desc("Run an arbitrary Lua function.")]
		public void CallFunc(LuaFunction func)
		{
			self.QueueActivity(new CallLuaFunc(func));
		}

		[ScriptActorPropertyActivity]
		[Desc("Wait for a specified number of game ticks (25 ticks = 1 second).")]
		public void Wait(int ticks)
		{
			self.QueueActivity(new Wait(ticks));
		}

		[ScriptActorPropertyActivity]
		[Desc("Remove the actor from the game, without triggering any death notification.")]
		public void Destroy()
		{
			self.QueueActivity(new RemoveSelf());
		}

		[Desc("Attempt to cancel any active activities.")]
		public void Stop()
		{
			self.CancelActivity();
		}

		[Desc("Current actor stance. Returns nil if this actor doesn't support stances.")]
		public string Stance
		{
			get
			{
				if (autotarget == null)
					return null;

				return autotarget.Stance.ToString();
			}

			set
			{
				if (autotarget == null)
					return;

				UnitStance stance;
				if (!Enum<UnitStance>.TryParse(value, true, out stance))
					throw new LuaException("Unknown stance type '{0}'".F(value));

				autotarget.Stance = stance;
			}
		}
	}
}
