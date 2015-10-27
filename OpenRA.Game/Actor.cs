#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA
{
	public sealed class Actor : IScriptBindable, IScriptNotifyBind, ILuaTableBinding, ILuaEqualityBinding, ILuaToStringBinding, IEquatable<Actor>, IDisposable
	{
		public readonly ActorInfo Info;

		public readonly World World;

		public readonly uint ActorID;

		public Player Owner { get; set; }

		public bool IsInWorld { get; internal set; }
		public bool Disposed { get; private set; }

		Activity currentActivity;

		public Group Group;
		public int Generation;

		public Rectangle Bounds { get; private set; }
		public Rectangle VisualBounds { get; private set; }
		public IEffectiveOwner EffectiveOwner { get; private set; }
		public IOccupySpace OccupiesSpace { get; private set; }

		public bool IsIdle { get { return currentActivity == null; } }
		public bool IsDead { get { return Disposed || (health != null && health.IsDead); } }

		public CPos Location { get { return OccupiesSpace.TopLeft; } }
		public WPos CenterPosition { get { return OccupiesSpace.CenterPosition; } }

		public WRot Orientation
		{
			get
			{
				// TODO: Support non-zero pitch/roll in IFacing (IOrientation?)
				var facingValue = facing != null ? facing.Facing : 0;
				return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facingValue));
			}
		}

		readonly IFacing facing;
		readonly IHealth health;
		readonly IRenderModifier[] renderModifiers;
		readonly IRender[] renders;
		readonly IDisable[] disables;
		readonly IVisibilityModifier[] visibilityModifiers;
		readonly IDefaultVisibility defaultVisibility;

		internal Actor(World world, string name, TypeDictionary initDict)
		{
			var init = new ActorInitializer(this, initDict);

			World = world;
			ActorID = world.NextAID();
			if (initDict.Contains<OwnerInit>())
				Owner = init.Get<OwnerInit, Player>();

			if (name != null)
			{
				name = name.ToLowerInvariant();

				if (!world.Map.Rules.Actors.ContainsKey(name))
					throw new NotImplementedException("No rules definition for unit " + name);

				Info = world.Map.Rules.Actors[name];
				foreach (var trait in Info.TraitsInConstructOrder())
				{
					AddTrait(trait.Create(init));

					// Some traits rely on properties provided by IOccupySpace in their initialization,
					// so we must ready it now, we cannot wait until all traits have finished construction.
					if (trait is IOccupySpaceInfo)
						OccupiesSpace = Trait<IOccupySpace>();
				}
			}

			Bounds = DetermineBounds();
			VisualBounds = DetermineVisualBounds();
			EffectiveOwner = TraitOrDefault<IEffectiveOwner>();
			facing = TraitOrDefault<IFacing>();
			health = TraitOrDefault<IHealth>();
			renderModifiers = TraitsImplementing<IRenderModifier>().ToArray();
			renders = TraitsImplementing<IRender>().ToArray();
			disables = TraitsImplementing<IDisable>().ToArray();
			visibilityModifiers = TraitsImplementing<IVisibilityModifier>().ToArray();
			defaultVisibility = Trait<IDefaultVisibility>();
		}

		Rectangle DetermineBounds()
		{
			var si = Info.TraitInfoOrDefault<SelectableInfo>();
			var size = (si != null && si.Bounds != null) ? new int2(si.Bounds[0], si.Bounds[1]) :
				TraitsImplementing<IAutoSelectionSize>().Select(x => x.SelectionSize(this)).FirstOrDefault();

			var offset = -size / 2;
			if (si != null && si.Bounds != null && si.Bounds.Length > 2)
				offset += new int2(si.Bounds[2], si.Bounds[3]);

			return new Rectangle(offset.X, offset.Y, size.X, size.Y);
		}

		Rectangle DetermineVisualBounds()
		{
			var sd = Info.TraitInfoOrDefault<ISelectionDecorationsInfo>();
			if (sd == null || sd.SelectionBoxBounds == null)
				return Bounds;

			var size = new int2(sd.SelectionBoxBounds[0], sd.SelectionBoxBounds[1]);

			var offset = -size / 2;
			if (sd.SelectionBoxBounds.Length > 2)
				offset += new int2(sd.SelectionBoxBounds[2], sd.SelectionBoxBounds[3]);

			return new Rectangle(offset.X, offset.Y, size.X, size.Y);
		}

		public void Tick()
		{
			var wasIdle = IsIdle;
			currentActivity = Traits.Util.RunActivity(this, currentActivity);

			if (!wasIdle && IsIdle)
				foreach (var n in TraitsImplementing<INotifyBecomingIdle>())
					n.OnBecomingIdle(this);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var renderables = Renderables(wr);
			foreach (var modifier in renderModifiers)
				renderables = modifier.ModifyRender(this, wr, renderables);
			return renderables;
		}

		IEnumerable<IRenderable> Renderables(WorldRenderer wr)
		{
			foreach (var render in renders)
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
			var name = Info.Name + " " + ActorID;
			if (!IsInWorld)
				name += " (not in world)";
			return name;
		}

		public T Trait<T>()
		{
			return World.TraitDict.Get<T>(this);
		}

		public T TraitOrDefault<T>()
		{
			return World.TraitDict.GetOrDefault<T>(this);
		}

		public IEnumerable<T> TraitsImplementing<T>()
		{
			return World.TraitDict.WithInterface<T>(this);
		}

		public void AddTrait(object trait)
		{
			World.TraitDict.AddTrait(this, trait);
		}

		public void Dispose()
		{
			World.AddFrameEndTask(w =>
			{
				if (Disposed)
					return;

				if (IsInWorld)
					World.Remove(this);

				foreach (var t in TraitsImplementing<INotifyActorDisposing>())
					t.Disposing(this);

				World.TraitDict.RemoveActor(this);
				Disposed = true;

				if (luaInterface != null)
					luaInterface.Value.OnActorDestroyed();
			});
		}

		// TODO: move elsewhere.
		public void ChangeOwner(Player newOwner)
		{
			World.AddFrameEndTask(w =>
			{
				if (Disposed)
					return;

				var oldOwner = Owner;
				var wasInWorld = IsInWorld;

				// momentarily remove from world so the ownership queries don't get confused
				if (wasInWorld)
					w.Remove(this);

				Owner = newOwner;
				Generation++;

				if (wasInWorld)
					w.Add(this);

				foreach (var t in this.TraitsImplementing<INotifyOwnerChanged>())
					t.OnOwnerChanged(this, oldOwner, newOwner);
			});
		}

		public DamageState GetDamageState()
		{
			if (Disposed)
				return DamageState.Dead;

			return (health == null) ? DamageState.Undamaged : health.DamageState;
		}

		public void InflictDamage(Actor attacker, int damage, IWarhead warhead)
		{
			if (Disposed || health == null)
				return;

			health.InflictDamage(this, attacker, damage, warhead, false);
		}

		public void Kill(Actor attacker)
		{
			if (Disposed || health == null)
				return;

			health.Kill(this, attacker);
		}

		public bool IsDisabled()
		{
			foreach (var disable in disables)
				if (disable.Disabled)
					return true;
			return false;
		}

		public bool CanBeViewedByPlayer(Player player)
		{
			foreach (var visibilityModifier in visibilityModifiers)
				if (!visibilityModifier.IsVisible(this, player))
					return false;

			return defaultVisibility.IsVisible(this, player);
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
