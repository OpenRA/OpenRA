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

using Eluant;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
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
				return Self.IsInWorld;
			}

			set
			{
				if (value)
					Self.World.AddFrameEndTask(w => w.Add(Self));
				else
					Self.World.AddFrameEndTask(w => w.Remove(Self));
			}
		}

		[Desc("Specifies whether the actor is alive or dead.")]
		public bool IsDead { get { return Self.IsDead; } }

		[Desc("Specifies whether the actor is idle (not performing any activities).")]
		public bool IsIdle { get { return Self.IsIdle; } }

		[Desc("The player that owns the actor.")]
		public Player Owner
		{
			get
			{
				return Self.Owner;
			}

			set
			{
				if (Self.Owner != value)
					Self.ChangeOwner(value);
			}
		}

		[Desc("The type of the actor (e.g. \"e1\").")]
		public string Type { get { return Self.Info.Name; } }

		[Desc("Unique ID of the actor (e.g. \"e1 234\"")]
		public string UID { get { return Self.ToString(); } }

		[Desc("Unique ID of the actor (e.g. \"234\"")]
		public int ActorID { get { return (int) Self.ActorID; } }

		[Desc("Test whether an actor has a specific property.")]
		public bool HasProperty(string name)
		{
			return Self.HasScriptProperty(Context, name);
		}

		[ScriptContext(ScriptContextType.Mission)]
		[Desc("Render a target flash on the actor. If set, 'asPlayer'",
			"defines which player palette to use. Duration is in ticks.")]
		public void Flash(int duration = 4, Player asPlayer = null)
		{
			Self.World.Add(new FlashTarget(Self, asPlayer, duration));
		}
	}

	[ScriptPropertyGroup("General")]
	public class GeneralProperties : ScriptActorProperties
	{
		readonly IFacing facing;
		readonly AutoTarget autotarget;
		readonly ScriptTags scriptTags;

		public GeneralProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			facing = self.TraitOrDefault<IFacing>();
			autotarget = self.TraitOrDefault<AutoTarget>();
			scriptTags = self.TraitOrDefault<ScriptTags>();
		}

		[Desc("The actor position in cell coordinates.")]
		public CPos Location { get { return Self.Location; } }

		[Desc("The actor position in world coordinates.")]
		public WPos CenterPosition { get { return Self.CenterPosition; } }

		[Desc("The direction that the actor is facing.")]
		public int Facing
		{
			get
			{
				if (facing == null)
					throw new LuaException("Actor '{0}' doesn't define a facing".F(Self));

				return facing.Facing;
			}
		}

		// AI and Misison safe but it would be cheating!
		[ScriptActorPropertyActivity]
		[Desc("Instantly moves the actor to the specified cell.")]
		public void Teleport(CPos cell)
		{
			Self.QueueActivity(new SimpleTeleport(cell));
		}

		[ScriptActorPropertyActivity]
		[Desc("Run an arbitrary Lua function.")]
		public void CallFunc(LuaFunction func)
		{
			Self.QueueActivity(new CallLuaFunc(func, Context));
		}

		[ScriptActorPropertyActivity]
		[Desc("Wait for a specified number of game ticks (25 ticks = 1 second).")]
		public void Wait(int ticks)
		{
			Self.QueueActivity(new Wait(ticks));
		}

		[ScriptActorPropertyActivity]
		[Desc("Remove the actor from the game, without triggering any death notification.")]
		public void Destroy()
		{
			Self.QueueActivity(new RemoveSelf());
		}

		[Desc("Attempt to cancel any active activities.")]
		public void Stop()
		{
			Self.CancelActivity();
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

		[Desc("Specifies whether or not the actor supports 'tags'.")]
		public bool IsTaggable { get { return scriptTags != null; } }

		[Desc("Add a tag to the actor. Returns true on success, false otherwise (for example the actor may already have the given tag).")]
		public bool AddTag(string tag)
		{
			return IsTaggable && scriptTags.AddTag(tag);
		}

		[Desc("Remove a tag from the actor. Returns true on success, false otherwise (tag was not present).")]
		public bool RemoveTag(string tag)
		{
			return IsTaggable && scriptTags.RemoveTag(tag);
		}

		[Desc("Specifies whether or not the actor has a particular tag.")]
		public bool HasTag(string tag)
		{
			return IsTaggable && scriptTags.HasTag(tag);
		}
	}
}
