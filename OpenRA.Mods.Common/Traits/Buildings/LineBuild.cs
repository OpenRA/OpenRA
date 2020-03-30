#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum LineBuildDirection { Unset, X, Y }
	public class LineBuildDirectionInit : IActorInit<LineBuildDirection>
	{
		[FieldFromYamlKey]
		readonly LineBuildDirection value = LineBuildDirection.Unset;

		public LineBuildDirectionInit() { }
		public LineBuildDirectionInit(LineBuildDirection init) { value = init; }
		public LineBuildDirection Value(World world) { return value; }
	}

	public class LineBuildParentInit : IActorInit<Actor[]>
	{
		[FieldFromYamlKey]
		public readonly string[] ParentNames = new string[0];

		readonly Actor[] parents = null;

		public LineBuildParentInit() { }
		public LineBuildParentInit(Actor[] init) { parents = init; }
		public Actor[] Value(World world)
		{
			if (parents != null)
				return parents;

			var sma = world.WorldActor.Trait<SpawnMapActors>();
			return ParentNames.Select(n => sma.Actors[n]).ToArray();
		}
	}

	public interface INotifyLineBuildSegmentsChanged
	{
		void SegmentAdded(Actor self, Actor segment);
		void SegmentRemoved(Actor self, Actor segment);
	}

	[Desc("Place the second actor in line to build more of the same at once (used for walls).")]
	public class LineBuildInfo : ITraitInfo
	{
		[Desc("The maximum allowed length of the line.")]
		public readonly int Range = 5;

		[Desc("LineBuildNode 'Types' to attach to.")]
		public readonly HashSet<string> NodeTypes = new HashSet<string> { "wall" };

		[ActorReference(typeof(LineBuildInfo))]
		[Desc("Actor type for line-built segments (defaults to same actor).")]
		public readonly string SegmentType = null;

		[Desc("Delete generated segments when destroyed or sold.")]
		public readonly bool SegmentsRequireNode = false;

		public object Create(ActorInitializer init) { return new LineBuild(init, this); }
	}

	public class LineBuild : INotifyKilled, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyLineBuildSegmentsChanged
	{
		readonly LineBuildInfo info;
		readonly Actor[] parentNodes = new Actor[0];
		HashSet<Actor> segments;

		public LineBuild(ActorInitializer init, LineBuildInfo info)
		{
			this.info = info;
			if (init.Contains<LineBuildParentInit>())
				parentNodes = init.Get<LineBuildParentInit>().Value(init.World);
		}

		void INotifyLineBuildSegmentsChanged.SegmentAdded(Actor self, Actor segment)
		{
			if (segments == null)
				segments = new HashSet<Actor>();

			segments.Add(segment);
		}

		void INotifyLineBuildSegmentsChanged.SegmentRemoved(Actor self, Actor segment)
		{
			if (segments == null)
				return;

			segments.Remove(segment);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			foreach (var parent in parentNodes)
				if (!parent.Disposed)
					foreach (var n in parent.TraitsImplementing<INotifyLineBuildSegmentsChanged>())
						n.SegmentAdded(parent, self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			foreach (var parent in parentNodes)
				if (!parent.Disposed)
					foreach (var n in parent.TraitsImplementing<INotifyLineBuildSegmentsChanged>())
						n.SegmentRemoved(parent, self);

			if (info.SegmentsRequireNode && segments != null)
				foreach (var s in segments)
					s.Dispose();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (info.SegmentsRequireNode && segments != null)
				foreach (var s in segments)
					s.Kill(e.Attacker);
		}
	}
}
