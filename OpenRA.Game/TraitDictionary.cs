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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;

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

	public interface ITraitEnumberable<T> : IEnumerable<T>
	{
		ITraitEnumberable<T> Where(Func<T, bool> predicate);
		List<T> ToList();
		T[] ToArray();
	}

	/// <summary>
	/// Provides efficient ways to query a set of actors by their traits.
	/// </summary>
	class TraitDictionary
	{
		static readonly Func<Type, ITraitContainer> CreateTraitContainer = t =>
			(ITraitContainer)typeof(TraitContainer<>).MakeGenericType(t).GetConstructor(Type.EmptyTypes).Invoke(null);

		readonly Dictionary<Type, ITraitContainer> traits = new Dictionary<Type, ITraitContainer>();

		ITraitContainer InnerGet(Type t)
		{
			return traits.GetOrAdd(t, CreateTraitContainer);
		}

		TraitContainer<T> InnerGet<T>()
		{
			return (TraitContainer<T>)InnerGet(typeof(T));
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
			if (actor.Disposed)
				throw new InvalidOperationException("Attempted to get trait from destroyed object ({0})".F(actor));
		}

		public T Get<T>(Actor actor)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().Get(actor.ActorID);
		}

		public T GetOrDefault<T>(Actor actor)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().GetOrDefault(actor.ActorID);
		}

		public ITraitEnumberable<T> WithInterface<T>(Actor actor)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().GetMultiple(actor.ActorID);
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
				t.Value.RemoveActor(a.ActorID);
		}

		interface ITraitContainer
		{
			void Add(Actor actor, object trait);
			void RemoveActor(uint actor);

			int Queries { get; }
		}

		class TraitContainer<T> : ITraitContainer
		{
			static readonly T[] EmptyArray = new T[0];
			readonly List<Actor> actors = new List<Actor>();
			readonly List<T> traits = new List<T>();

			static bool ActorHasTrait(List<Actor> actors, uint actor, int index)
			{
				return index < actors.Count && actors[index].ActorID == actor;
			}

			static bool FindFirstTrait(List<Actor> actors, uint actor, out int index)
			{
				index = actors.BinarySearchMany(actor);
				return ActorHasTrait(actors, actor, index);
			}

			static int FindTraitRange(List<Actor> actors, uint actor, out int firstIndex, out int endIndex)
			{
				var actorCount = actors.Count;
				firstIndex = actors.BinarySearchMany(actor);
				var end = firstIndex;
				while (end < actorCount && actors[end].ActorID == actor)
					end++;

				endIndex = end;
				return end - firstIndex;
			}

			public int Queries { get; private set; }

			public void Add(Actor actor, object trait)
			{
				var insertIndex = actors.BinarySearchMany(actor.ActorID + 1);
				actors.Insert(insertIndex, actor);
				traits.Insert(insertIndex, (T)trait);
			}

			public T Get(uint actor)
			{
				var result = GetOrDefault(actor);
				if (result == null)
					throw new InvalidOperationException("Actor does not have trait of type `{0}`".F(typeof(T)));
				return result;
			}

			public T GetOrDefault(uint actor)
			{
				++Queries;
				int index;
				if (!FindFirstTrait(actors, actor, out index))
					return default(T);
				else if (ActorHasTrait(actors, actor, index + 1))
					throw new InvalidOperationException("Actor {0} has multiple traits of type `{1}`".F(actors[index].Info.Name, typeof(T)));
				else return traits[index];
			}

			public ITraitEnumberable<T> GetMultiple(uint actor)
			{
				// PERF: Custom enumerator for efficiency - using `yield` is slower.
				++Queries;
				return new MultipleEnumerable(this, actor);
			}

			class MultipleEnumerable : ITraitEnumberable<T>
			{
				protected readonly TraitContainer<T> Container;
				protected readonly uint Actor;
				public MultipleEnumerable(TraitContainer<T> container, uint actor) { Container = container; Actor = actor; }
				public virtual IEnumerator<T> GetEnumerator() { return new MultipleEnumerator(Container, Actor); }
				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
				public virtual ITraitEnumberable<T> Where(Func<T, bool> predicate) { return new MultipleEnumerableWhere(Container, Actor, predicate); }
				public virtual List<T> ToList()
				{
					int startIndex, endIndex;
					var result = new List<T>(FindTraitRange(Container.actors, Actor, out startIndex, out endIndex));
					var traits = Container.traits;
					while (startIndex < endIndex)
						result.Add(traits[startIndex++]);

					return result;
				}

				public virtual T[] ToArray()
				{
					int startIndex, endIndex;
					var count = FindTraitRange(Container.actors, Actor, out startIndex, out endIndex);
					if (count == 0)
						return EmptyArray;

					// PERF: fastest for ~75 entries or less
					var result = new T[count];
					var traits = Container.traits;
					for (var i = 0; startIndex < endIndex;)
						result[i++] = traits[startIndex++];

					return result;
				}
			}

			class MultipleEnumerator : IEnumerator<T>
			{
				protected readonly List<Actor> Actors;
				protected readonly List<T> Traits;
				protected readonly uint Actor;
				protected int index;
				public MultipleEnumerator(TraitContainer<T> container, uint actor)
				{
					Actors = container.actors;
					Traits = container.traits;
					Actor = actor;
					Reset();
				}

				public void Reset() { FindFirstTrait(Actors, Actor, out index); index--; }
				public virtual bool MoveNext() { return ActorHasTrait(Actors, Actor, ++index); }
				public T Current { get { return Traits[index]; } }
				object System.Collections.IEnumerator.Current { get { return Current; } }
				public void Dispose() { }
			}

			class MultipleEnumerableWhere : MultipleEnumerable
			{
				protected readonly Func<T, bool> Predicate;
				public MultipleEnumerableWhere(TraitContainer<T> container, uint actor, Func<T, bool> predicate)
					: base(container, actor) { Predicate = predicate; }
				public override IEnumerator<T> GetEnumerator() { return new MultipleEnumeratorWhere(Container, Actor, Predicate); }
				public override ITraitEnumberable<T> Where(Func<T, bool> predicate)
				{
					return new MultipleEnumerableWhere(Container, Actor, t => Predicate(t) && predicate(t));
				}

				public override List<T> ToList()
				{
					int startIndex, endIndex;
					var result = new List<T>(FindTraitRange(Container.actors, Actor, out startIndex, out endIndex));
					var traits = Container.traits;
					while (startIndex < endIndex)
					{
						var trait = traits[startIndex++];
						if (Predicate(trait))
							result.Add(trait);
					}

					return result;
				}

				public override T[] ToArray()
				{
					int startIndex, endIndex;
					var allCount = FindTraitRange(Container.actors, Actor, out startIndex, out endIndex);
					if (allCount == 0)
						return EmptyArray;

					// PERF: fastest for ~75 entries or less
					var buffer = new T[allCount];
					var count = 0;
					var traits = Container.traits;
					while (startIndex < endIndex)
					{
						var trait = traits[startIndex++];
						if (Predicate(trait))
							buffer[count++] = trait;
					}

					if (count == allCount)
						return buffer;

					var result = new T[count];

					// PERF: fastest for ~75 entries or less
					for (var i = 0; i < count; i++)
						result[i] = buffer[i];

					return result;
				}
			}

			class MultipleEnumeratorWhere : MultipleEnumerator
			{
				readonly Func<T, bool> predicate;
				public MultipleEnumeratorWhere(TraitContainer<T> container, uint actor, Func<T, bool> predicate)
					: base(container, actor) { this.predicate = predicate; }

				public override bool MoveNext()
				{
					while (ActorHasTrait(Actors, Actor, ++index))
						if (predicate(Traits[index]))
							return true;

					return false;
				}
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
					if (actors[i] == last)
						continue;
					yield return actors[i];
					last = actors[i];
				}
			}

			public IEnumerable<Actor> Actors(Func<T, bool> predicate)
			{
				++Queries;
				Actor last = null;

				for (var i = 0; i < actors.Count; i++)
				{
					if (actors[i] == last || !predicate(traits[i]))
						continue;
					yield return actors[i];
					last = actors[i];
				}
			}

			class AllEnumerable : IEnumerable<TraitPair<T>>
			{
				readonly TraitContainer<T> container;
				public AllEnumerable(TraitContainer<T> container) { this.container = container; }
				public IEnumerator<TraitPair<T>> GetEnumerator() { return new AllEnumerator(container); }
				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
			}

			class AllEnumerator : IEnumerator<TraitPair<T>>
			{
				readonly List<Actor> actors;
				readonly List<T> traits;
				int index;
				public AllEnumerator(TraitContainer<T> container)
				{
					actors = container.actors;
					traits = container.traits;
					Reset();
				}

				public void Reset() { index = -1; }
				public bool MoveNext() { return ++index < actors.Count; }
				public TraitPair<T> Current { get { return new TraitPair<T>(actors[index], traits[index]); } }
				object System.Collections.IEnumerator.Current { get { return Current; } }
				public void Dispose() { }
			}

			public void RemoveActor(uint actor)
			{
				int startIndex, endIndex;
				var count = FindTraitRange(actors, actor, out startIndex, out endIndex);
				if (count == 0)
					return;

				actors.RemoveRange(startIndex, count);
				traits.RemoveRange(startIndex, count);
			}
		}
	}
}
