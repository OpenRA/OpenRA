#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
	[Flags]
	public enum SystemActors
	{
		Player = 0,
		EditorPlayer = 1,
		World = 2,
		EditorWorld = 4
	}

	public sealed class Actor : IScriptBindable, IScriptNotifyBind, ILuaTableBinding, ILuaEqualityBinding, ILuaToStringBinding, IEquatable<Actor>, IDisposable
	{
		internal readonly struct SyncHash
		{
			public readonly ISync Trait;
			readonly Func<object, int> hashFunction;
			public SyncHash(ISync trait) { Trait = trait; hashFunction = Sync.GetHashFunction(trait); }
			public int Hash() { return hashFunction(Trait); }
		}

		public readonly ActorInfo Info;

		public readonly World World;

		public readonly uint ActorID;

		public Player Owner { get; internal set; }

		public bool IsInWorld { get; internal set; }
		public bool WillDispose { get; private set; }
		public bool Disposed { get; private set; }

		Activity currentActivity;
		public Activity CurrentActivity
		{
			get => Activity.SkipDoneActivities(currentActivity);
			private set => currentActivity = value;
		}

		public int Generation;
		public Actor ReplacedByActor;

		public IEffectiveOwner EffectiveOwner { get; }
		public IOccupySpace OccupiesSpace { get; }
		public ITargetable[] Targetables { get; }
		public IEnumerable<ITargetablePositions> EnabledTargetablePositions { get; private set; }

		public bool IsIdle => CurrentActivity == null;
		public bool IsDead => Disposed || (health != null && health.IsDead);

		public CPos Location => OccupiesSpace.TopLeft;
		public WPos CenterPosition => OccupiesSpace.CenterPosition;

		public WRot Orientation => facing?.Orientation ?? WRot.None;

		/// <summary>Value used to represent an invalid token.</summary>
		public static readonly int InvalidConditionToken = -1;

		class ConditionState
		{
			/// <summary>Delegates that have registered to be notified when this condition changes.</summary>
			public readonly List<VariableObserverNotifier> Notifiers = new List<VariableObserverNotifier>();

			/// <summary>Unique integers identifying granted instances of the condition.</summary>
			public readonly HashSet<int> Tokens = new HashSet<int>();
		}

		readonly Dictionary<string, ConditionState> conditionStates = new Dictionary<string, ConditionState>();

		/// <summary>Each granted condition receives a unique token that is used when revoking.</summary>
		readonly Dictionary<int, string> conditionTokens = new Dictionary<int, string>();

		int nextConditionToken = 1;

		/// <summary>Cache of condition -> enabled state for quick evaluation of token counter conditions.</summary>
		readonly Dictionary<string, int> conditionCache = new Dictionary<string, int>();

		/// <summary>Read-only version of conditionCache that is passed to IConditionConsumers.</summary>
		readonly IReadOnlyDictionary<string, int> readOnlyConditionCache;

		internal SyncHash[] SyncHashes { get; }

		readonly IFacing facing;
		readonly IHealth health;
		readonly IResolveOrder[] resolveOrders;
		readonly IRenderModifier[] renderModifiers;
		readonly IRender[] renders;
		readonly IMouseBounds[] mouseBounds;
		readonly IVisibilityModifier[] visibilityModifiers;
		readonly IDefaultVisibility defaultVisibility;
		readonly INotifyBecomingIdle[] becomingIdles;
		readonly INotifyIdle[] tickIdles;
		readonly IEnumerable<WPos> enabledTargetableWorldPositions;
		bool created;

		internal Actor(World world, string name, TypeDictionary initDict)
		{
			var duplicateInit = initDict.WithInterface<ISingleInstanceInit>().GroupBy(i => i.GetType())
				.FirstOrDefault(i => i.Count() > 1);

			if (duplicateInit != null)
				throw new InvalidDataException($"Duplicate initializer '{duplicateInit.Key.Name}'");

			var init = new ActorInitializer(this, initDict);

			readOnlyConditionCache = new ReadOnlyDictionary<string, int>(conditionCache);

			World = world;
			ActorID = world.NextAID();
			var ownerInit = init.GetOrDefault<OwnerInit>();
			if (ownerInit != null)
				Owner = ownerInit.Value(world);

			if (name != null)
			{
				name = name.ToLowerInvariant();

				if (!world.Map.Rules.Actors.ContainsKey(name))
					throw new NotImplementedException("No rules definition for unit " + name);

				Info = world.Map.Rules.Actors[name];

				var resolveOrdersList = new List<IResolveOrder>();
				var renderModifiersList = new List<IRenderModifier>();
				var rendersList = new List<IRender>();
				var mouseBoundsList = new List<IMouseBounds>();
				var visibilityModifiersList = new List<IVisibilityModifier>();
				var becomingIdlesList = new List<INotifyBecomingIdle>();
				var tickIdlesList = new List<INotifyIdle>();
				var targetablesList = new List<ITargetable>();
				var targetablePositionsList = new List<ITargetablePositions>();
				var syncHashesList = new List<SyncHash>();

				foreach (var traitInfo in Info.TraitsInConstructOrder())
				{
					var trait = traitInfo.Create(init);
					AddTrait(trait);

					// PERF: Cache all these traits as soon as the actor is created. This is a fairly cheap one-off cost per
					// actor that allows us to provide some fast implementations of commonly used methods that are relied on by
					// performance-sensitive parts of the core game engine, such as pathfinding, visibility and rendering.
					// Note: The blocks are required to limit the scope of the t's, so we make an exception to our normal style
					// rules for spacing in order to keep these assignments compact and readable.
					{ if (trait is IOccupySpace t) OccupiesSpace = t; }
					{ if (trait is IEffectiveOwner t) EffectiveOwner = t; }
					{ if (trait is IFacing t) facing = t; }
					{ if (trait is IHealth t) health = t; }
					{ if (trait is IResolveOrder t) resolveOrdersList.Add(t); }
					{ if (trait is IRenderModifier t) renderModifiersList.Add(t); }
					{ if (trait is IRender t) rendersList.Add(t); }
					{ if (trait is IMouseBounds t) mouseBoundsList.Add(t); }
					{ if (trait is IVisibilityModifier t) visibilityModifiersList.Add(t); }
					{ if (trait is IDefaultVisibility t) defaultVisibility = t; }
					{ if (trait is INotifyBecomingIdle t) becomingIdlesList.Add(t); }
					{ if (trait is INotifyIdle t) tickIdlesList.Add(t); }
					{ if (trait is ITargetable t) targetablesList.Add(t); }
					{ if (trait is ITargetablePositions t) targetablePositionsList.Add(t); }
					{ if (trait is ISync t) syncHashesList.Add(new SyncHash(t)); }
				}

				resolveOrders = resolveOrdersList.ToArray();
				renderModifiers = renderModifiersList.ToArray();
				renders = rendersList.ToArray();
				mouseBounds = mouseBoundsList.ToArray();
				visibilityModifiers = visibilityModifiersList.ToArray();
				becomingIdles = becomingIdlesList.ToArray();
				tickIdles = tickIdlesList.ToArray();
				Targetables = targetablesList.ToArray();
				var targetablePositions = targetablePositionsList.ToArray();
				EnabledTargetablePositions = targetablePositions.Where(Exts.IsTraitEnabled);
				enabledTargetableWorldPositions = EnabledTargetablePositions.SelectMany(tp => tp.TargetablePositions(this));
				SyncHashes = syncHashesList.ToArray();
			}
		}

		internal void Initialize(bool addToWorld = true)
		{
			created = true;

			// Make sure traits are usable for condition notifiers
			foreach (var t in TraitsImplementing<INotifyCreated>())
				t.Created(this);

			var allObserverNotifiers = new HashSet<VariableObserverNotifier>();
			foreach (var provider in TraitsImplementing<IObservesVariables>())
			{
				foreach (var variableUser in provider.GetVariableObservers())
				{
					allObserverNotifiers.Add(variableUser.Notifier);
					foreach (var variable in variableUser.Variables)
					{
						var cs = conditionStates.GetOrAdd(variable);
						cs.Notifiers.Add(variableUser.Notifier);

						// Initialize conditions that have not yet been granted to 0
						// NOTE: Some conditions may have already been granted by INotifyCreated calling GrantCondition,
						// and we choose to assign the token count to safely cover both cases instead of adding an if branch.
						conditionCache[variable] = cs.Tokens.Count;
					}
				}
			}

			// Update all traits with their initial condition state
			foreach (var notify in allObserverNotifiers)
				notify(this, readOnlyConditionCache);

			// TODO: Other traits may need initialization after being notified of initial condition state.

			// TODO: A post condition initialization notification phase may allow queueing activities instead.
			// The initial activity should run before any activities queued by INotifyCreated.Created
			// However, we need to know which traits are enabled (via conditions), so wait for after the calls and insert the activity as the first
			ICreationActivity creationActivity = null;
			foreach (var ica in TraitsImplementing<ICreationActivity>())
			{
				if (!ica.IsTraitEnabled())
					continue;

				if (creationActivity != null)
					throw new InvalidOperationException($"More than one enabled ICreationActivity trait: {creationActivity.GetType().Name} and {ica.GetType().Name}");

				var activity = ica.GetCreationActivity();
				if (activity == null)
					continue;

				creationActivity = ica;

				activity.Queue(CurrentActivity);
				CurrentActivity = activity;
			}

			if (addToWorld)
				World.Add(this);
		}

		public void Tick()
		{
			var wasIdle = IsIdle;
			CurrentActivity = ActivityUtils.RunActivity(this, CurrentActivity);

			if (!wasIdle && IsIdle)
			{
				foreach (var n in becomingIdles)
					n.OnBecomingIdle(this);

				// If IsIdle is true, it means the last CurrentActivity.Tick returned null.
				// If a next activity has been queued via OnBecomingIdle, we need to start running it now,
				// to avoid an 'empty' null tick where the actor will (visibly, if moving) do nothing.
				CurrentActivity = ActivityUtils.RunActivity(this, CurrentActivity);
			}
			else if (wasIdle)
				foreach (var tickIdle in tickIdles)
					tickIdle.TickIdle(this);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			// PERF: Avoid LINQ.
			var renderables = Renderables(wr);
			foreach (var modifier in renderModifiers)
				renderables = modifier.ModifyRender(this, wr, renderables);
			return renderables;
		}

		IEnumerable<IRenderable> Renderables(WorldRenderer wr)
		{
			// PERF: Avoid LINQ.
			// Implementations of Render are permitted to return both an eagerly materialized collection or a lazily
			// generated sequence.
			// For large amounts of renderables, a lazily generated sequence (e.g. as returned by LINQ, or by using
			// `yield`) will avoid the need to allocate a large collection.
			// For small amounts of renderables, allocating a small collection can often be faster and require less
			// memory than creating the objects needed to represent a sequence.
			foreach (var render in renders)
				foreach (var renderable in render.Render(this, wr))
					yield return renderable;
		}

		public IEnumerable<Rectangle> ScreenBounds(WorldRenderer wr)
		{
			var bounds = Bounds(wr);
			foreach (var modifier in renderModifiers)
				bounds = modifier.ModifyScreenBounds(this, wr, bounds);
			return bounds;
		}

		IEnumerable<Rectangle> Bounds(WorldRenderer wr)
		{
			// PERF: Avoid LINQ. See comments for Renderables
			foreach (var render in renders)
				foreach (var r in render.ScreenBounds(this, wr))
					if (!r.IsEmpty)
						yield return r;
		}

		public Polygon MouseBounds(WorldRenderer wr)
		{
			foreach (var mb in mouseBounds)
			{
				var bounds = mb.MouseoverBounds(this, wr);
				if (!bounds.IsEmpty)
					return bounds;
			}

			return Polygon.Empty;
		}

		public void QueueActivity(bool queued, Activity nextActivity)
		{
			if (!queued)
				CancelActivity();

			QueueActivity(nextActivity);
		}

		public void QueueActivity(Activity nextActivity)
		{
			if (!created)
				throw new InvalidOperationException("An activity was queued before the actor was created. Queue it inside the INotifyCreated.Created callback instead.");

			if (CurrentActivity == null)
				CurrentActivity = nextActivity;
			else
				CurrentActivity.Queue(nextActivity);
		}

		public void CancelActivity()
		{
			CurrentActivity?.Cancel(this);
		}

		public override int GetHashCode()
		{
			return (int)ActorID;
		}

		public override bool Equals(object obj)
		{
			return obj is Actor o && Equals(o);
		}

		public bool Equals(Actor other)
		{
			return ActorID == other.ActorID;
		}

		public override string ToString()
		{
			// PERF: Avoid format strings.
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
			// If CurrentActivity isn't null, run OnActorDisposeOuter in case some cleanups are needed.
			// This should be done before the FrameEndTask to avoid dependency issues.
			CurrentActivity?.OnActorDisposeOuter(this);

			// Allow traits/activities to prevent a race condition when they depend on disposing the actor (e.g. Transforms)
			WillDispose = true;

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

				luaInterface?.Value.OnActorDestroyed();
			});
		}

		public void ResolveOrder(Order order)
		{
			foreach (var r in resolveOrders)
				r.ResolveOrder(this, order);
		}

		// TODO: move elsewhere.
		public void ChangeOwner(Player newOwner)
		{
			World.AddFrameEndTask(_ => ChangeOwnerSync(newOwner));
		}

		/// <summary>
		/// Change the actors owner without queuing a FrameEndTask.
		/// This must only be called from inside an existing FrameEndTask.
		/// </summary>
		public void ChangeOwnerSync(Player newOwner)
		{
			if (Disposed)
				return;

			var oldOwner = Owner;
			var wasInWorld = IsInWorld;

			// momentarily remove from world so the ownership queries don't get confused
			if (wasInWorld)
				World.Remove(this);

			Owner = newOwner;
			Generation++;

			foreach (var t in TraitsImplementing<INotifyOwnerChanged>())
				t.OnOwnerChanged(this, oldOwner, newOwner);

			foreach (var t in World.WorldActor.TraitsImplementing<INotifyOwnerChanged>())
				t.OnOwnerChanged(this, oldOwner, newOwner);

			if (wasInWorld)
				World.Add(this);
		}

		public DamageState GetDamageState()
		{
			if (Disposed)
				return DamageState.Dead;

			return (health == null) ? DamageState.Undamaged : health.DamageState;
		}

		public void InflictDamage(Actor attacker, Damage damage)
		{
			if (Disposed || health == null)
				return;

			health.InflictDamage(this, attacker, damage, false);
		}

		public void Kill(Actor attacker, BitSet<DamageType> damageTypes = default)
		{
			if (Disposed || health == null)
				return;

			health.Kill(this, attacker, damageTypes);
		}

		public bool CanBeViewedByPlayer(Player player)
		{
			// PERF: Avoid LINQ.
			foreach (var visibilityModifier in visibilityModifiers)
				if (!visibilityModifier.IsVisible(this, player))
					return false;

			return defaultVisibility.IsVisible(this, player);
		}

		public BitSet<TargetableType> GetAllTargetTypes()
		{
			// PERF: Avoid LINQ.
			var targetTypes = default(BitSet<TargetableType>);
			foreach (var targetable in Targetables)
				targetTypes = targetTypes.Union(targetable.TargetTypes);
			return targetTypes;
		}

		public BitSet<TargetableType> GetEnabledTargetTypes()
		{
			// PERF: Avoid LINQ.
			var targetTypes = default(BitSet<TargetableType>);
			foreach (var targetable in Targetables)
				if (targetable.IsTraitEnabled())
					targetTypes = targetTypes.Union(targetable.TargetTypes);
			return targetTypes;
		}

		public bool IsTargetableBy(Actor byActor)
		{
			// PERF: Avoid LINQ.
			foreach (var targetable in Targetables)
				if (targetable.TargetableBy(this, byActor))
					return true;

			return false;
		}

		public IEnumerable<WPos> GetTargetablePositions()
		{
			if (EnabledTargetablePositions.Any())
				return enabledTargetableWorldPositions;

			return new[] { CenterPosition };
		}

		#region Conditions

		void UpdateConditionState(string condition, int token, bool isRevoke)
		{
			var conditionState = conditionStates.GetOrAdd(condition);

			if (isRevoke)
				conditionState.Tokens.Remove(token);
			else
				conditionState.Tokens.Add(token);

			conditionCache[condition] = conditionState.Tokens.Count;

			// Conditions may be granted or revoked before the state is initialized.
			// These notifications will be processed after INotifyCreated.Created.
			if (created)
				foreach (var notify in conditionState.Notifiers)
					notify(this, readOnlyConditionCache);
		}

		/// <summary>
		/// Grants a specified condition if it is valid.
		/// Otherwise, just returns InvalidConditionToken.
		/// </summary>
		/// <returns>The token that is used to revoke this condition.</returns>
		public int GrantCondition(string condition)
		{
			if (string.IsNullOrEmpty(condition))
				return InvalidConditionToken;

			var token = nextConditionToken++;
			conditionTokens.Add(token, condition);
			UpdateConditionState(condition, token, false);
			return token;
		}

		/// <summary>
		/// Revokes a previously granted condition.
		/// </summary>
		/// <param name="token">The token ID returned by GrantCondition.</param>
		/// <returns>The invalid token ID.</returns>
		public int RevokeCondition(int token)
		{
			if (!conditionTokens.TryGetValue(token, out var condition))
				throw new InvalidOperationException($"Attempting to revoke condition with invalid token {token} for {this}.");

			conditionTokens.Remove(token);
			UpdateConditionState(condition, token, true);
			return InvalidConditionToken;
		}

		/// <summary>Returns whether the specified token is valid for RevokeCondition</summary>
		public bool TokenValid(int token)
		{
			return conditionTokens.ContainsKey(token);
		}

		#endregion

		#region Scripting interface

		Lazy<ScriptActorInterface> luaInterface;
		public void OnScriptBind(ScriptContext context)
		{
			if (luaInterface == null)
				luaInterface = Exts.Lazy(() => new ScriptActorInterface(context, this));
		}

		public LuaValue this[LuaRuntime runtime, LuaValue keyValue]
		{
			get => luaInterface.Value[runtime, keyValue];
			set => luaInterface.Value[runtime, keyValue] = value;
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			if (!left.TryGetClrValue(out Actor a) || !right.TryGetClrValue(out Actor b))
				return false;

			return a == b;
		}

		public LuaValue ToString(LuaRuntime runtime)
		{
			return $"Actor ({this})";
		}

		public bool HasScriptProperty(string name)
		{
			return luaInterface.Value.ContainsKey(name);
		}

		#endregion
	}
}
