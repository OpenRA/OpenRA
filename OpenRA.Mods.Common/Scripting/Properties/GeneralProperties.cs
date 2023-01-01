#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using Eluant;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
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
			get => Self.IsInWorld;

			set
			{
				if (value)
					Self.World.AddFrameEndTask(w => w.Add(Self));
				else
					Self.World.AddFrameEndTask(w => w.Remove(Self));
			}
		}

		[Desc("Specifies whether the actor is alive or dead.")]
		public bool IsDead => Self.IsDead;

		[Desc("Specifies whether the actor is idle (not performing any activities).")]
		public bool IsIdle => Self.IsIdle;

		[Desc("The player that owns the actor.")]
		public Player Owner
		{
			get => Self.Owner;

			set
			{
				if (value == null)
					throw new LuaException($"Attempted to change the owner of actor '{Self}' to nil value.");

				if (Self.Owner != value)
					Self.ChangeOwner(value);
			}
		}

		[Desc("The type of the actor (e.g. \"e1\").")]
		public string Type => Self.Info.Name;

		[Desc("Test whether an actor has a specific property.")]
		public bool HasProperty(string name)
		{
			return Self.HasScriptProperty(name);
		}

		[Desc("Render a target flash on the actor.")]
		public void Flash(Color color, int count = 2, int interval = 2, int delay = 0)
		{
			// TODO: We can't use floats with Lua, so use the default 0.5f here
			Self.World.Add(new FlashTarget(Self, color, 0.5f, count, interval, delay));
		}

		[Desc("The effective owner of the actor.")]
		public Player EffectiveOwner
		{
			get
			{
				if (Self.EffectiveOwner == null || Self.EffectiveOwner.Owner == null)
					return Self.Owner;

				return Self.EffectiveOwner.Owner;
			}
		}
	}

	[ScriptPropertyGroup("General")]
	public class GeneralProperties : ScriptActorProperties
	{
		readonly IFacing facing;
		readonly AutoTarget autotarget;
		readonly ScriptTags scriptTags;
		readonly Tooltip[] tooltips;

		public GeneralProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			facing = self.TraitOrDefault<IFacing>();
			autotarget = self.TraitOrDefault<AutoTarget>();
			scriptTags = self.TraitOrDefault<ScriptTags>();
			tooltips = self.TraitsImplementing<Tooltip>().ToArray();
		}

		[Desc("The actor position in cell coordinates.")]
		public CPos Location => Self.Location;

		[Desc("The actor position in world coordinates.")]
		public WPos CenterPosition => Self.CenterPosition;

		[Desc("The direction that the actor is facing.")]
		public WAngle Facing
		{
			get
			{
				if (facing == null)
					throw new LuaException($"Actor '{Self}' doesn't define a facing");

				return facing.Facing;
			}
		}

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
			get => autotarget?.Stance.ToString();

			set
			{
				if (autotarget == null)
					return;

				if (!Enum<UnitStance>.TryParse(value, true, out var stance))
					throw new LuaException($"Unknown stance type '{value}'");

				autotarget.PredictedStance = stance;
				autotarget.SetStance(Self, stance);
			}
		}

		[Desc("The actor's tooltip name. Returns nil if the actor has no tooltip.")]
		public string TooltipName
		{
			get
			{
				var tooltip = tooltips.FirstEnabledConditionalTraitOrDefault();

				return tooltip?.Info.Name;
			}
		}

		[Desc("Specifies whether or not the actor supports 'tags'.")]
		public bool IsTaggable => scriptTags != null;

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
