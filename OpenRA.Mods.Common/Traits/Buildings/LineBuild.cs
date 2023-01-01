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

namespace OpenRA.Mods.Common.Traits
{
	public enum LineBuildDirection { Unset, X, Y }
	public class LineBuildDirectionInit : ValueActorInit<LineBuildDirection>, ISingleInstanceInit
	{
		public LineBuildDirectionInit(LineBuildDirection value)
			: base(value) { }
	}

	public class LineBuildParentInit : ValueActorInit<string[]>, ISingleInstanceInit
	{
		readonly Actor[] parents = null;

		public LineBuildParentInit(Actor[] value)
			: base(Array.Empty<string>())
		{
			parents = value;
		}

		public Actor[] ActorValue(World world)
		{
			if (parents != null)
				return parents;

			var sma = world.WorldActor.Trait<SpawnMapActors>();
			return Value.Select(n => sma.Actors[n]).ToArray();
		}
	}

	public interface INotifyLineBuildSegmentsChanged
	{
		void SegmentAdded(Actor self, Actor segment);
		void SegmentRemoved(Actor self, Actor segment);
	}

	[Desc("Place the second actor in line to build more of the same at once (used for walls).")]
	public class LineBuildInfo : TraitInfo
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

		public override object Create(ActorInitializer init) { return new LineBuild(init, this); }
	}

	public class LineBuild : INotifyKilled, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyLineBuildSegmentsChanged
	{
		readonly LineBuildInfo info;
		readonly Actor[] parentNodes = Array.Empty<Actor>();
		HashSet<Actor> segments;

		public LineBuild(ActorInitializer init, LineBuildInfo info)
		{
			this.info = info;
			var lineBuildParentInit = init.GetOrDefault<LineBuildParentInit>();
			if (lineBuildParentInit != null)
				parentNodes = lineBuildParentInit.ActorValue(init.World);
		}

		void INotifyLineBuildSegmentsChanged.SegmentAdded(Actor self, Actor segment)
		{
			if (segments == null)
				segments = new HashSet<Actor>();

			segments.Add(segment);
		}

		void INotifyLineBuildSegmentsChanged.SegmentRemoved(Actor self, Actor segment)
		{
			segments?.Remove(segment);
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
