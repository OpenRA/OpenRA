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
			if (actor.Destroyed)
				throw new InvalidOperationException("Attempted to get trait from destroyed object ({0})".F(actor));
		}

		public bool Contains<T>(Actor actor)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().GetMultiple(actor.ActorID).Any();
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

		public IEnumerable<T> WithInterface<T>(Actor actor)
		{
			CheckDestroyed(actor);
			return InnerGet<T>().GetMultiple(actor.ActorID);
		}

		public IEnumerable<TraitPair<T>> ActorsWithTrait<T>()
		{
			return InnerGet<T>().All();
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
				var index = actors.BinarySearchMany(actor);
				if (index >= actors.Count || actors[index].ActorID != actor)
					return default(T);
				else if (index + 1 < actors.Count && actors[index + 1].ActorID == actor)
					throw new InvalidOperationException("Actor has multiple traits of type `{0}`".F(typeof(T)));
				else return traits[index];
			}

			public IEnumerable<T> GetMultiple(uint actor)
			{
				++Queries;
				var index = actors.BinarySearchMany(actor);
				while (index < actors.Count && actors[index].ActorID == actor)
				{
					yield return traits[index];
					++index;
				}
			}

			public IEnumerable<TraitPair<T>> All()
			{
				++Queries;
				for (var i = 0; i < actors.Count; i++)
					yield return new TraitPair<T> { Actor = actors[i], Trait = traits[i] };
			}

			public void RemoveActor(uint actor)
			{
				for (var i = actors.Count - 1; i >= 0; i--)
				{
					if (actors[i].ActorID == actor)
					{
						actors.RemoveAt(i);
						traits.RemoveAt(i);
					}
				}
			}
		}
	}
}
