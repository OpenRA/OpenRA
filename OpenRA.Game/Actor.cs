#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA
{
	[Flags]
	public enum ActorFlag
	{
		None		= 0,
		InWorld		= 1 << 0,
		Idle		= 1 << 1,
		Dead		= 1 << 2,
		Destroyed	= 1 << 3,
		Disguised	= 1 << 4,
	}

	public class Actor : IScriptBindable, IScriptNotifyBind, ILuaTableBinding, ILuaEqualityBinding, ILuaToStringBinding, IEquatable<Actor>
	{
		public readonly ActorInfo Info;

		public readonly World World;
		public readonly uint ActorID;
		public Lazy<Rectangle> Bounds;

		Lazy<IOccupySpace> occupySpace;
		Lazy<IFacing> facing;
		Lazy<Health> health;
		Lazy<IEffectiveOwner> effectiveOwner;

		public IOccupySpace OccupiesSpace { get { return occupySpace.Value; } }
		public IEffectiveOwner EffectiveOwner { get { return effectiveOwner.Value; } }

		public CPos Location { get { return occupySpace.Value.TopLeft; } }
		public WPos CenterPosition { get { return occupySpace.Value.CenterPosition; } }

		public WRot Orientation
		{
			get
			{
				// TODO: Support non-zero pitch/roll in IFacing (IOrientation?)
				var facingValue = facing.Value != null ? facing.Value.Facing : 0;
				return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facingValue));
			}
		}

		[Sync] public Player Owner;

		ActorFlag actorFlags;
		Activity currentActivity;
		public Group Group;
		public int Generation;

		public bool Flagged(ActorFlag flag)
		{
			return (actorFlags & flag) != 0;
		}

		internal void SetFlag(ActorFlag flag, bool on)
		{
			actorFlags = on ? actorFlags | flag : actorFlags ^ flag;
		}

		internal Actor(World world, string name, TypeDictionary initDict)
		{
			var init = new ActorInitializer(this, initDict);

			World = world;
			ActorID = world.NextAID();
			if (initDict.Contains<OwnerInit>())
				Owner = init.Get<OwnerInit, Player>();

			occupySpace = Exts.Lazy(() => TraitOrDefault<IOccupySpace>());

			if (name != null)
			{
				if (!world.Map.Rules.Actors.ContainsKey(name.ToLowerInvariant()))
					throw new NotImplementedException("No rules definition for unit {0}".F(name.ToLowerInvariant()));

				Info = world.Map.Rules.Actors[name.ToLowerInvariant()];
				foreach (var trait in Info.TraitsInConstructOrder())
					AddTrait(trait.Create(init));
			}

			facing = Exts.Lazy(() => TraitOrDefault<IFacing>());
			health = Exts.Lazy(() => TraitOrDefault<Health>());
			effectiveOwner = Exts.Lazy(() => TraitOrDefault<IEffectiveOwner>());

			Bounds = Exts.Lazy(() =>
			{
				var si = Info.Traits.GetOrDefault<SelectableInfo>();
				var size = (si != null && si.Bounds != null) ? new int2(si.Bounds[0], si.Bounds[1]) :
					TraitsImplementing<IAutoSelectionSize>().Select(x => x.SelectionSize(this)).FirstOrDefault();

				var offset = -size / 2;
				if (si != null && si.Bounds != null && si.Bounds.Length > 2)
					offset += new int2(si.Bounds[2], si.Bounds[3]);

				return new Rectangle(offset.X, offset.Y, size.X, size.Y);
			});
		}

		public void Tick()
		{
			var wasIdle = Flagged(ActorFlag.Idle);
			currentActivity = Traits.Util.RunActivity(this, currentActivity);

			SetFlag(ActorFlag.Idle, currentActivity == null);

			if (!wasIdle && Flagged(ActorFlag.Idle))
				foreach (var n in TraitsImplementing<INotifyBecomingIdle>())
					n.OnBecomingIdle(this);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var renderables = Renderables(wr);
			foreach (var modifier in TraitsImplementing<IRenderModifier>())
				renderables = modifier.ModifyRender(this, wr, renderables);
			return renderables;
		}

		IEnumerable<IRenderable> Renderables(WorldRenderer wr)
		{
			foreach (var render in TraitsImplementing<IRender>())
				foreach (var renderable in render.Render(this, wr))
					yield return renderable;
		}

		public void QueueActivity(bool queued, Activity nextActivity)
		{
			if (!queued)
				CancelActivity();
			QueueActivity(nextActivity);
		}

		public void QueueActivity(Activity nextActivity)
		{
			if (currentActivity == null)
				currentActivity = nextActivity;
			else
				currentActivity.Queue(nextActivity);
		}

		public void CancelActivity()
		{
			if (currentActivity != null)
				currentActivity.Cancel(this);
		}

		public Activity GetCurrentActivity()
		{
			return currentActivity;
		}

		public override int GetHashCode()
		{
			return (int)ActorID;
		}

		public override bool Equals(object obj)
		{
			var o = obj as Actor;
			return o != null && Equals(o);
		}

		public bool Equals(Actor other)
		{
			return ActorID == other.ActorID;
		}

		public override string ToString()
		{
			return "{0} {1}{2}".F(Info.Name, ActorID, Flagged(ActorFlag.InWorld) ? "" : " (not in world)");
		}

		public T Trait<T>()
		{
			return World.traitDict.Get<T>(this);
		}

		public T TraitOrDefault<T>()
		{
			return World.traitDict.GetOrDefault<T>(this);
		}

		public IEnumerable<T> TraitsImplementing<T>()
		{
			return World.traitDict.WithInterface<T>(this);
		}

		public bool HasTrait<T>()
		{
			return World.traitDict.Contains<T>(this);
		}

		public void AddTrait(object trait)
		{
			World.traitDict.AddTrait(this, trait);
		}

		public void Destroy()
		{
			World.AddFrameEndTask(w =>
			{
				if (Flagged(ActorFlag.Destroyed))
					return;

				if (Flagged(ActorFlag.InWorld))
					World.Remove(this);

				World.traitDict.RemoveActor(this);
				SetFlag(ActorFlag.Destroyed, true);
				SetFlag(ActorFlag.Dead, true);

				if (luaInterface != null)
					luaInterface.Value.OnActorDestroyed();
			});
		}

		// TODO: move elsewhere.
		public void ChangeOwner(Player newOwner)
		{
			World.AddFrameEndTask(w =>
			{
				if (Flagged(ActorFlag.Destroyed))
					return;

				var oldOwner = Owner;

				// momentarily remove from world so the ownership queries don't get confused
				w.Remove(this);
				Owner = newOwner;
				Generation++;
				w.Add(this);

				foreach (var t in this.TraitsImplementing<INotifyOwnerChanged>())
					t.OnOwnerChanged(this, oldOwner, newOwner);
			});
		}

		public void ChangeDisguise()
		{
			if (effectiveOwner.Value != null)
				SetFlag(ActorFlag.Disguised, effectiveOwner.Value.Disguised);
		}

		public void Kill(Actor attacker)
		{
			if (health.Value == null)
				return;

			health.Value.InflictDamage(this, attacker, health.Value.MaxHP, null, true);
		}

		#region Scripting interface

		Lazy<ScriptActorInterface> luaInterface;
		public void OnScriptBind(ScriptContext context)
		{
			if (luaInterface == null)
				luaInterface = Exts.Lazy(() => new ScriptActorInterface(context, this));
		}

		public LuaValue this[LuaRuntime runtime, LuaValue keyValue]
		{
			get { return luaInterface.Value[runtime, keyValue]; }
			set { luaInterface.Value[runtime, keyValue] = value; }
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			Actor a, b;
			if (!left.TryGetClrValue<Actor>(out a) || !right.TryGetClrValue<Actor>(out b))
				return false;

			return a == b;
		}

		public LuaValue ToString(LuaRuntime runtime)
		{
			return "Actor ({0})".F(this);
		}

		public bool HasScriptProperty(string name)
		{
			return luaInterface.Value.ContainsKey(name);
		}

		#endregion
	}
}
