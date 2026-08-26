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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Tcd.Formations;
using OpenRA.Traits;

namespace OpenRA.Mods.Tcd.Traits
{
	public enum FormationCaptureMode
	{
		None,
		Points,
	}

	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Holds the shape the player is currently marking out for a formation.",
		"Client-side only: nothing here reaches the simulation, so it cannot desync.")]
	public sealed class FormationCaptureInfo : TraitInfo
	{
		[Desc("Most corners a marked shape may have. Reaching the cap places the units",
			"straight away, so the tool always has an end.")]
		public readonly int MaxPoints = 12;

		public override object Create(ActorInitializer init) { return new FormationCapture(init.World, this); }
	}

	public sealed class FormationCapture
	{
		readonly World world;
		readonly FormationCaptureInfo info;
		readonly List<WPos> points = [];

		public FormationCaptureMode Mode { get; private set; }
		public IReadOnlyList<WPos> Points => points;

		// Marked points close into a shape; a drawn line stays open.
		public bool Closed => Mode == FormationCaptureMode.Points && points.Count > 2;

		public FormationCapture(World world, FormationCaptureInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public bool IsFull => points.Count >= info.MaxPoints;

		public void Begin(FormationCaptureMode mode)
		{
			if (Mode == mode)
				return;

			Mode = mode;
			points.Clear();
		}

		public void AddPoint(WPos pos)
		{
			points.Add(pos);
		}

		public void Cancel()
		{
			Mode = FormationCaptureMode.None;
			points.Clear();
		}

		// Lays the current selection out along whatever has been drawn, then clears it.
		public int Commit()
		{
			var closed = Closed;
			var drawn = points.ToList();
			var mode = Mode;
			Cancel();

			if (mode == FormationCaptureMode.None || drawn.Count == 0)
				return 0;

			var members = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead && a.TraitOrDefault<Mobile>() != null)
				.ToList();

			if (members.Count == 0)
				return 0;

			var slots = FormationPath.Distribute(drawn, members.Count, closed);

			// Walk the drawn path in order so units take the nearest slot rather than
			// trading places across the whole shape.
			var direction = slots[^1] - slots[0];
			var origin = slots[0];
			var ordered = members
				.OrderBy(a => Along(a.CenterPosition - origin, direction))
				.ToList();

			long x = 0;
			long y = 0;
			foreach (var s in slots)
			{
				x += s.X;
				y += s.Y;
			}

			var midpoint = new WPos((int)(x / slots.Length), (int)(y / slots.Length), 0);
			var offsets = slots.Select(s => s - midpoint).ToArray();

			return FormationPlanner.IssueMoves(world, ordered, offsets, midpoint, WRot.FromYaw(WAngle.Zero));
		}

		static long Along(WVec v, WVec direction)
		{
			return (long)v.X * direction.X + (long)v.Y * direction.Y;
		}
	}
}
