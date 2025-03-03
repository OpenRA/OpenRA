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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	/// <summary>
	/// Maintains an index of actors in the world.
	/// </summary>
	public abstract class ActorIndex : IDisposable
	{
		readonly World world;
		readonly HashSet<Actor> actors = [];

		public IReadOnlyCollection<Actor> Actors => actors;

		ActorIndex(World world, IEnumerable<Actor> initialActorsToIndex)
		{
			this.world = world;
			world.ActorAdded += AddActor;
			world.ActorRemoved += RemoveActor;

			actors.UnionWith(initialActorsToIndex);
		}

		protected abstract bool ShouldIndexActor(Actor actor);

		void AddActor(Actor actor)
		{
			if (ShouldIndexActor(actor))
				actors.Add(actor);
		}

		void RemoveActor(Actor actor)
		{
			actors.Remove(actor);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				world.ActorAdded -= AddActor;
				world.ActorRemoved -= RemoveActor;
			}
		}

		// No OwnerAndTrait class is provided. As the world can provide actors by trait anyway,
		// an additional filter on owner isn't sufficiently selective to justify the overhead of manging the index.
		// Whereas a filter with actor names is much more selective, so OwnerAndNames is worthwhile.

		/// <summary>
		/// Maintains an index of actors in the world that
		/// are owned by a given <see cref="Player"/>
		/// and have one of the given <see cref="ActorInfo.Name"/>.
		/// </summary>
		public sealed class OwnerAndNames : ActorIndex
		{
			readonly HashSet<string> names;
			readonly Player owner;

			public OwnerAndNames(World world, IReadOnlyCollection<string> names, Player owner)
				: base(world, ActorsToIndex(world, names.ToHashSet(), owner))
			{
				this.names = names.ToHashSet();
				this.owner = owner;
			}

			static IEnumerable<Actor> ActorsToIndex(World world, HashSet<string> names, Player owner)
			{
				return world.Actors.Where(a => a.Owner == owner && names.Contains(a.Info.Name));
			}

			protected override bool ShouldIndexActor(Actor actor)
			{
				return actor.Owner == owner && names.Contains(actor.Info.Name);
			}
		}

		/// <summary>
		/// Maintains an index of actors in the world that
		/// have one of the given <see cref="ActorInfo.Name"/>
		/// and have the trait with info of type <typeparamref name="TTraitInfo"/>.
		/// </summary>
		public sealed class NamesAndTrait<TTraitInfo> : ActorIndex where TTraitInfo : ITraitInfoInterface
		{
			readonly HashSet<string> names;

			public NamesAndTrait(World world, IReadOnlyCollection<string> names)
				: base(world, ActorsToIndex(world, names.ToHashSet()))
			{
				this.names = names.ToHashSet();
			}

			static IEnumerable<Actor> ActorsToIndex(World world, HashSet<string> names)
			{
				return world.Actors.Where(a => names.Contains(a.Info.Name) && a.Info.HasTraitInfo<TTraitInfo>());
			}

			protected override bool ShouldIndexActor(Actor actor)
			{
				return names.Contains(actor.Info.Name) && actor.Info.HasTraitInfo<TTraitInfo>();
			}
		}

		/// <summary>
		/// Maintains an index of actors in the world that
		/// are owned by a given <see cref="Player"/>,
		/// have one of the given <see cref="ActorInfo.Name"/>
		/// and have the trait with info of type <typeparamref name="TTraitInfo"/>.
		/// </summary>
		public sealed class OwnerAndNamesAndTrait<TTraitInfo> : ActorIndex where TTraitInfo : ITraitInfoInterface
		{
			readonly HashSet<string> names;
			readonly Player owner;

			public OwnerAndNamesAndTrait(World world, IReadOnlyCollection<string> names, Player owner)
				: base(world, ActorsToIndex(world, names.ToHashSet(), owner))
			{
				this.names = names.ToHashSet();
				this.owner = owner;
			}

			static IEnumerable<Actor> ActorsToIndex(World world, HashSet<string> names, Player owner)
			{
				return world.Actors.Where(a => a.Owner == owner && names.Contains(a.Info.Name) && a.Info.HasTraitInfo<TTraitInfo>());
			}

			protected override bool ShouldIndexActor(Actor actor)
			{
				return actor.Owner == owner && names.Contains(actor.Info.Name) && actor.Info.HasTraitInfo<TTraitInfo>();
			}
		}
	}
}
