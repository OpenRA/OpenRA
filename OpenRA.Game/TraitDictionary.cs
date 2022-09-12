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
using System.Linq;
using System.Reflection;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	static class ListExts
	{
		public static int BinarySearchMany(this List<Actor> list, uint searchFor)
		{
			var start = 0;
			var end = list.Count;
			while (start != end)
			{
				var mid = (start + end) / 2;
				if (list[mid].ActorID < searchFor)
					start = mid + 1;
				else
					end = mid;
			}

			return start;
		}
	}

	/// <summary>
	/// Provides efficient ways to query a set of actors by their traits.
	/// </summary>
	class TraitDictionary
	{
		static TraitMultiplicity GetTraitMultiplicity(Type traitType)
		{
			var tma = traitType.GetCustomAttribute<TraitMultiplicityAttribute>(false);
			if (tma == null)
				return TraitMultiplicity.None;

			return tma.Multiplicity;
		}

		static readonly Func<Type, ITraitContainerBase> CreateTraitContainer = t =>
		{
			var ct = GetTraitMultiplicity(t) == TraitMultiplicity.OnePerActor ? typeof(TraitMap<>) : typeof(TraitList<>);
			return (ITraitContainerBase)ct.MakeGenericType(t).GetConstructor(Type.EmptyTypes).Invoke(null);
		};

		readonly Dictionary<Type, ITraitContainerBase> traits = new Dictionary<Type, ITraitContainerBase>();

		ITraitContainerBase InnerGet(Type t)
		{
			return traits.GetOrAdd(t, CreateTraitContainer);
		}

		ITraitContainer<T> InnerGet<T>()
		{
			return (ITraitContainer<T>)InnerGet(typeof(T));
		}

		public void PrintReport()
		{
			Log.AddChannel("traitreport", "traitreport.log");
			foreach (var t in traits.OrderByDescending(t => t.Value.Queries).TakeWhile(t => t.Value.Queries > 0))
				Log.Write("traitreport", "{0}: {1}", t.Key.Name, t.Value.Queries);
		}

		public void AddTrait(Actor actor, object val)
		{
			var t = val.GetType();

			foreach (var i in t.GetInterfaces())
				InnerAdd(actor, i, val);
			foreach (var tt in t.BaseTypes())
				InnerAdd(actor, tt, val);
		}

		void InnerAdd(Actor actor, Type t, object val)
		{
			InnerGet(t).Add(actor, val);
		}

		static void CheckDestroyed(Actor actor)
		{
			// PERF: The compiler does not inline this function. Likely due to formatting the message.
			if (actor.Disposed)
				throw new InvalidOperationException($"Attempted to get trait from destroyed object ({actor})");
		}

		public T Get<T>(Actor actor)
		{
			// PERF: Inline check.
			if (actor.Disposed)
				CheckDestroyed(actor);

			return InnerGet<T>().Get(actor);
		}

		public T GetOrDefault<T>(Actor actor)
		{
			// PERF: Inline check.
			if (actor.Disposed)
				CheckDestroyed(actor);

			return InnerGet<T>().GetOrDefault(actor);
		}

		public IEnumerable<T> WithInterface<T>(Actor actor)
		{
			// PERF: Inline check.
			if (actor.Disposed)
				CheckDestroyed(actor);

			return InnerGet<T>().GetMultiple(actor);
		}

		public IEnumerable<TraitPair<T>> ActorsWithTrait<T>()
		{
			return InnerGet<T>().All();
		}

		public IEnumerable<Actor> ActorsHavingTrait<T>()
		{
			return InnerGet<T>().Actors();
		}

		public IEnumerable<Actor> ActorsHavingTrait<T>(Func<T, bool> predicate)
		{
			return InnerGet<T>().Actors(predicate);
		}

		public void RemoveActor(Actor a)
		{
			foreach (var t in traits)
				t.Value.RemoveActor(a);
		}

		public void ApplyToActorsWithTrait<T>(Action<Actor, T> action)
		{
			InnerGet<T>().ApplyToAll(action);
		}

		public void ApplyToActorsWithTraitTimed<T>(Action<Actor, T> action, string text)
		{
			InnerGet<T>().ApplyToAllTimed(action, text);
		}

		interface ITraitContainerBase
		{
			void Add(Actor actor, object trait);
			void RemoveActor(Actor actor);

			int Queries { get; }
		}

		interface ITraitContainer<T> : ITraitContainerBase
		{
			T Get(Actor actor);
			T GetOrDefault(Actor actor);
			IEnumerable<T> GetMultiple(Actor actor);
			IEnumerable<TraitPair<T>> All();
			IEnumerable<Actor> Actors();
			IEnumerable<Actor> Actors(Func<T, bool> predicate);
			void ApplyToAll(Action<Actor, T> action);
			void ApplyToAllTimed(Action<Actor, T> action, string text);
		}

		class TraitList<T> : ITraitContainer<T>
		{
			readonly List<Actor> actors = new List<Actor>();
			readonly List<T> traits = new List<T>();
			readonly PerfTickLogger perfLogger = new PerfTickLogger();

			public int Queries { get; private set; }

			public void Add(Actor actor, object trait)
			{
				var insertIndex = actors.BinarySearchMany(actor.ActorID + 1);
				actors.Insert(insertIndex, actor);
				traits.Insert(insertIndex, (T)trait);
			}

			public T Get(Actor actor)
			{
				var result = GetOrDefault(actor);
				if (result == null)
					throw new InvalidOperationException($"Actor {actor.Info.Name} does not have trait of type `{typeof(T)}`");

				return result;
			}

			public T GetOrDefault(Actor actor)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor.ActorID);
				if (index >= actors.Count || actors[index] != actor)
					return default;

				if (index + 1 < actors.Count && actors[index + 1] == actor)
					throw new InvalidOperationException($"Actor {actor.Info.Name} has multiple traits of type `{typeof(T)}`");

				return traits[index];
			}

			public IEnumerable<T> GetMultiple(Actor actor)
			{
				// PERF: Custom enumerator for efficiency - using `yield` is slower.
				++Queries;
				return new MultipleEnumerable(this, actor.ActorID);
			}

			class MultipleEnumerable : IEnumerable<T>
			{
				readonly TraitList<T> container;
				readonly uint actor;
				public MultipleEnumerable(TraitList<T> container, uint actor) { this.container = container; this.actor = actor; }
				public IEnumerator<T> GetEnumerator() { return new MultipleEnumerator(container, actor); }
				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
			}

			struct MultipleEnumerator : IEnumerator<T>
			{
				readonly List<Actor> actors;
				readonly List<T> traits;
				readonly uint actor;
				int index;
				public MultipleEnumerator(TraitList<T> container, uint actor)
					: this()
				{
					actors = container.actors;
					traits = container.traits;
					this.actor = actor;
					Reset();
				}

				public void Reset() { index = actors.BinarySearchMany(actor) - 1; }
				public bool MoveNext() { return ++index < actors.Count && actors[index].ActorID == actor; }
				public T Current => traits[index];
				object System.Collections.IEnumerator.Current => Current;
				public void Dispose() { }
			}

			public IEnumerable<TraitPair<T>> All()
			{
				// PERF: Custom enumerator for efficiency - using `yield` is slower.
				++Queries;
				return new AllEnumerable(this);
			}

			public IEnumerable<Actor> Actors()
			{
				++Queries;
				Actor last = null;
				for (var i = 0; i < actors.Count; i++)
				{
					var current = actors[i];
					if (current == last)
						continue;

					yield return current;
					last = current;
				}
			}

			public IEnumerable<Actor> Actors(Func<T, bool> predicate)
			{
				++Queries;
				Actor last = null;

				for (var i = 0; i < actors.Count; i++)
				{
					var current = actors[i];
					if (current == last || !predicate(traits[i]))
						continue;

					yield return current;
					last = current;
				}
			}

			readonly struct AllEnumerable : IEnumerable<TraitPair<T>>
			{
				readonly TraitList<T> container;
				public AllEnumerable(TraitList<T> container) { this.container = container; }
				public IEnumerator<TraitPair<T>> GetEnumerator() { return new AllEnumerator(container); }
				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
			}

			struct AllEnumerator : IEnumerator<TraitPair<T>>
			{
				readonly List<Actor> actors;
				readonly List<T> traits;
				int index;
				public AllEnumerator(TraitList<T> container)
					: this()
				{
					actors = container.actors;
					traits = container.traits;
					Reset();
				}

				public void Reset() { index = -1; }
				public bool MoveNext() { return ++index < actors.Count; }
				public TraitPair<T> Current => new TraitPair<T>(actors[index], traits[index]);
				object System.Collections.IEnumerator.Current => Current;
				public void Dispose() { }
			}

			public void RemoveActor(Actor actor)
			{
				var actorID = actor.ActorID;
				var startIndex = actors.BinarySearchMany(actorID);
				if (startIndex >= actors.Count || actors[startIndex].ActorID != actorID)
					return;

				var endIndex = startIndex + 1;
				while (endIndex < actors.Count && actors[endIndex].ActorID == actorID)
					endIndex++;

				var count = endIndex - startIndex;
				actors.RemoveRange(startIndex, count);
				traits.RemoveRange(startIndex, count);
			}

			public void ApplyToAll(Action<Actor, T> action)
			{
				for (var i = 0; i < actors.Count; i++)
					action(actors[i], traits[i]);
			}

			public void ApplyToAllTimed(Action<Actor, T> action, string text)
			{
				perfLogger.Start();
				for (var i = 0; i < actors.Count; i++)
				{
					var actor = actors[i];
					var trait = traits[i];
					action(actor, trait);

					perfLogger.LogTickAndRestartTimer(text, trait);
				}
			}
		}

		class TraitMap<T> : ITraitContainer<T>
		{
			readonly Dictionary<Actor, T> values = new Dictionary<Actor, T>();
			readonly PerfTickLogger perfLogger = new PerfTickLogger();

			public int Queries { get; private set; }

			public void Add(Actor actor, object trait)
			{
				try
				{
					values.Add(actor, (T)trait);
				}
				catch (ArgumentException)
				{
					throw new ArgumentException($"A trait of type {typeof(T)} is already associated with actor {actor.Info.Name}. Trait was declared to have a multiplicity of OnePerActor.");
				}
			}

			public T Get(Actor actor)
			{
				if (values.TryGetValue(actor, out var trait))
					return trait;

				throw new InvalidOperationException($"Actor {actor.Info.Name} does not have trait of type `{typeof(T)}`");
			}

			public T GetOrDefault(Actor actor)
			{
				++Queries;
				values.TryGetValue(actor, out var trait);
				return trait;
			}

			public IEnumerable<T> GetMultiple(Actor actor)
			{
				++Queries;
				if (values.TryGetValue(actor, out var trait))
					yield return trait;
			}

			public IEnumerable<TraitPair<T>> All()
			{
				// PERF: Custom enumerator for efficiency - using `yield` is slower.
				++Queries;
				return new AllEnumerable(values);
			}

			public IEnumerable<Actor> Actors()
			{
				++Queries;
				return values.Keys;
			}

			public IEnumerable<Actor> Actors(Func<T, bool> predicate)
			{
				++Queries;
				foreach (var pair in values)
				{
					if (!predicate(pair.Value))
						continue;

					yield return pair.Key;
				}
			}

			readonly struct AllEnumerable : IEnumerable<TraitPair<T>>
			{
				readonly IEnumerable<KeyValuePair<Actor, T>> enumerable;
				public AllEnumerable(Dictionary<Actor, T> values) { enumerable = values; }
				public IEnumerator<TraitPair<T>> GetEnumerator() { return new AllEnumerator(enumerable); }
				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
			}

			struct AllEnumerator : IEnumerator<TraitPair<T>>
			{
				readonly IEnumerator<KeyValuePair<Actor, T>> enumerator;

				public AllEnumerator(IEnumerable<KeyValuePair<Actor, T>> enumerable)
					: this()
				{
					enumerator = enumerable.GetEnumerator();
				}

				public void Reset() { enumerator.Reset(); }
				public bool MoveNext() { return enumerator.MoveNext(); }
				public TraitPair<T> Current => new TraitPair<T>(enumerator.Current.Key, enumerator.Current.Value);
				object System.Collections.IEnumerator.Current => Current;
				public void Dispose() { enumerator.Dispose(); }
			}

			public void RemoveActor(Actor actor)
			{
				values.Remove(actor);
			}

			public void ApplyToAll(Action<Actor, T> action)
			{
				foreach (var pair in values)
					action(pair.Key, pair.Value);
			}

			public void ApplyToAllTimed(Action<Actor, T> action, string text)
			{
				perfLogger.Start();

				foreach (var pair in values)
				{
					action(pair.Key, pair.Value);
					perfLogger.LogTickAndRestartTimer(text, pair.Value);
				}
			}
		}
	}
}
