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

	class TraitDictionary
	{
		// construct this delegate once.
		static Func<Type, ITraitContainer> doCreateTraitContainer = CreateTraitContainer;
		static ITraitContainer CreateTraitContainer(Type t)
		{
			return (ITraitContainer)typeof(TraitContainer<>).MakeGenericType(t)
				.GetConstructor(Type.EmptyTypes).Invoke(null);
		}

		Dictionary<Type, ITraitContainer> traits = new Dictionary<Type, ITraitContainer>();

		ITraitContainer InnerGet(Type t)
		{
			return traits.GetOrAdd(t, doCreateTraitContainer);
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

		public T Single<T>(Actor actor)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().Single(actor.ActorID);
		}

		public T Single<T>(Actor actor, Func<T, bool> predicate)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().Single(actor.ActorID, predicate);
		}

		public T SingleOrDefault<T>(Actor actor)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().SingleOrDefault(actor.ActorID);
		}

		public T SingleOrDefault<T>(Actor actor, Func<T, bool> predicate)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().SingleOrDefault(actor.ActorID, predicate);
		}

		public T First<T>(Actor actor)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().First(actor.ActorID);
		}

		public T First<T>(Actor actor, Func<T, bool> predicate)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().First(actor.ActorID, predicate);
		}

		public T FirstOrDefault<T>(Actor actor)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().FirstOrDefault(actor.ActorID);
		}

		public T FirstOrDefault<T>(Actor actor, Func<T, bool> predicate)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().FirstOrDefault(actor.ActorID, predicate);
		}

		public bool Any<T>(Actor actor, Func<T, bool> predicate)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().Any(actor.ActorID, predicate);
		}

		public IEnumerable<T> Where<T>(Actor actor)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().WhereActor(actor.ActorID);
		}

		public IEnumerable<T> Where<T>(Actor actor, Func<T, bool> predicate)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().WhereActor(actor.ActorID, predicate);
		}

		public IEnumerable<TraitPair<T>> ActorsWithTrait<T>()
		{
			return InnerGet<T>().ActorsWithTrait();
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
			readonly List<Actor> actors = new List<Actor>();
			readonly List<T> traits = new List<T>();

			public int Queries { get; private set; }

			public void Add(Actor actor, object trait)
			{
				var insertIndex = actors.BinarySearchMany(actor.ActorID + 1);
				actors.Insert(insertIndex, actor);
				traits.Insert(insertIndex, (T)trait);
			}

			public T First(uint actor)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor);
				if (index >= actors.Count || actors[index].ActorID != actor)
					throw new InvalidOperationException("Actor does not have trait of type `{0}`".F(typeof(T)));
				return traits[index];
			}

			public T First(uint actor, Func<T, bool> predicate)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor);
				if (index >= actors.Count || actors[index].ActorID != actor)
					throw new InvalidOperationException("Actor does not have trait of type `{0}`".F(typeof(T)));
				do
					if (predicate(traits[index]))
						return traits[index];
				while (++index < actors.Count && actors[index].ActorID == actor);
				throw new InvalidOperationException("Actor does not have matching trait of type `{0}`".F(typeof(T)));
			}

			public T FirstOrDefault(uint actor)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor);
				if (index >= actors.Count || actors[index].ActorID != actor)
					return default(T);
				return traits[index];
			}

			public T FirstOrDefault(uint actor, Func<T, bool> predicate)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor);
				for (; index < actors.Count && actors[index].ActorID == actor; index++)
					if (predicate(traits[index]))
						return traits[index];
				return default(T);
			}

			public T Single(uint actor)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor);
				if (index >= actors.Count || actors[index].ActorID != actor)
					throw new InvalidOperationException("Actor does not have trait of type `{0}`".F(typeof(T)));
				else if (index + 1 < actors.Count && actors[index + 1].ActorID == actor)
					throw new InvalidOperationException("Actor {0} has multiple traits of type `{1}`".F(actors[index].Info.Name, typeof(T)));
				return traits[index];
			}

			public T Single(uint actor, Func<T, bool> predicate)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor);
				for (; index < actors.Count && actors[index].ActorID == actor; index++)
					if (predicate(traits[index]))
						break;
				if (index >= actors.Count)
					throw new InvalidOperationException("Actor does not have matching trait of type `{0}`".F(typeof(T)));
				for (var i = index + 1; i < actors.Count && actors[i].ActorID == actor; i++)
					if (predicate(traits[i]))
						throw new InvalidOperationException("Actor {0} has multiple matching traits of type `{1}`".F(actors[index].Info.Name, typeof(T)));
				return traits[index];
			}

			public T SingleOrDefault(uint actor)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor);
				if (index >= actors.Count || actors[index].ActorID != actor)
					return default(T);
				else if (index + 1 < actors.Count && actors[index + 1].ActorID == actor)
					throw new InvalidOperationException("Actor {0} has multiple traits of type `{1}`".F(actors[index].Info.Name, typeof(T)));
				return traits[index];
			}

			public T SingleOrDefault(uint actor, Func<T, bool> predicate)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor);
				for (; index < actors.Count && actors[index].ActorID == actor; index++)
					if (predicate(traits[index]))
						break;
				if (index >= actors.Count)
					return default(T);
				for (var i = index + 1; i < actors.Count && actors[i].ActorID == actor; i++)
					if (predicate(traits[i]))
						throw new InvalidOperationException("Actor {0} has multiple matching traits of type `{1}`".F(actors[index].Info.Name, typeof(T)));
				return traits[index];
			}

			public bool Any(uint actor, Func<T, bool> predicate)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor);
				for (; index < actors.Count && actors[index].ActorID == actor; index++)
					if (predicate(traits[index]))
						return true;
				return false;
			}

			public IEnumerable<T> WhereActor(uint actor)
			{
				++Queries;
				return new MultipleEnumerable(this, actor);
			}

			public IEnumerable<T> WhereActor(uint actor, Func<T, bool> predicate)
			{
				++Queries;
				return new MultipleWhereEnumerable(this, actor, predicate);
			}

			class MultipleEnumerable : IEnumerable<T>
			{
				protected readonly TraitContainer<T> Container;
				protected readonly uint Actor;
				public MultipleEnumerable(TraitContainer<T> container, uint actor) { Container = container; Actor = actor; }
				public virtual IEnumerator<T> GetEnumerator() { return new MultipleEnumerator(Container, Actor); }
				System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
			}

			class MultipleWhereEnumerable : MultipleEnumerable
			{
				readonly Func<T, bool> predicate;
				public MultipleWhereEnumerable(TraitContainer<T> container, uint actor, Func<T, bool> predicate)
					: base(container, actor) { this.predicate = predicate; }
				public override IEnumerator<T> GetEnumerator() { return new MultipleWhereEnumerator(Container, Actor, predicate); }
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

				public void Reset() { index = Actors.BinarySearchMany(Actor) - 1; }
				public virtual bool MoveNext() { return ++index < Actors.Count && Actors[index].ActorID == Actor; }
				public T Current { get { return Traits[index]; } }
				object System.Collections.IEnumerator.Current { get { return Current; } }
				public void Dispose() { }
			}

			class MultipleWhereEnumerator : MultipleEnumerator
			{
				readonly Func<T, bool> predicate;
				public MultipleWhereEnumerator(TraitContainer<T> container, uint actor, Func<T, bool> predicate)
					: base(container, actor) { this.predicate = predicate; }

				public override bool MoveNext()
				{
					while (++index < Actors.Count && Actors[index].ActorID == Actor)
						if (predicate(Traits[index]))
							return true;
					return false;
				}
			}

			public IEnumerable<TraitPair<T>> ActorsWithTrait()
			{
				++Queries;
				return new AllEnumerable(this);
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
				public TraitPair<T> Current { get { return new TraitPair<T> { Actor = actors[index], Trait = traits[index] }; } }
				object System.Collections.IEnumerator.Current { get { return Current; } }
				public void Dispose() { }
			}

			public void RemoveActor(uint actor)
			{
				var startIndex = actors.BinarySearchMany(actor);
				if (startIndex >= actors.Count || actors[startIndex].ActorID != actor)
					return;
				var endIndex = startIndex + 1;
				while (endIndex < actors.Count && actors[endIndex].ActorID == actor)
					endIndex++;
				var count = endIndex - startIndex;
				actors.RemoveRange(startIndex, count);
				traits.RemoveRange(startIndex, count);
			}
		}
	}
}
