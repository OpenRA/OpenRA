#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	[Desc("Required for FrozenUnderFog to work. Attach this to the player actor.")]
	public class FrozenActorLayerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new FrozenActorLayer(init.Self); }
	}

	public class FrozenActor
	{
		public readonly PPos[] Footprint;
		public readonly WPos CenterPosition;
		public readonly Rectangle Bounds;
		readonly IRemoveFrozenActor[] removeFrozenActors;
		readonly Actor actor;
		readonly Shroud shroud;

		public Player Owner;

		public ITooltipInfo TooltipInfo;
		public Player TooltipOwner;

		public int HP;
		public DamageState DamageState;

		/// <summary>
		/// Is the frozen preview of the real actor currently visible?
		/// That means the real actor is currently under fog.
		/// </summary>
		public bool IsVisible = true;
		public bool NeedRenderables;
		public bool IsRendering { get; private set; }

		public uint ID { get { return actor.ActorID; } }
		public bool IsValid { get { return Owner != null; } }
		public ActorInfo Info { get { return actor.Info; } }
		public Actor Actor { get { return !actor.IsDead ? actor : null; } }

		public Color RadarColor;
		public HashSet<CPos> OccupiedCells;

		public FrozenActor(Actor self, PPos[] footprint, Shroud shroud)
		{
			actor = self;
			this.shroud = shroud;
			removeFrozenActors = self.TraitsImplementing<IRemoveFrozenActor>().ToArray();

			// Consider all cells inside the map area (ignoring the current map bounds)
			Footprint = footprint.Where(shroud.Contains).ToArray();

			CenterPosition = self.CenterPosition;
			Bounds = self.Bounds;

			var mod = self.TraitsImplementing<IRadarColorModifier>().FirstOrDefault();
			RadarColor = mod != null ? mod.RadarColorOverride(self) : self.Owner.Color.RGB;
			OccupiedCells = new HashSet<CPos>(footprint.SelectMany(uv => self.World.Map.Unproject(uv).Select(m => m.ToCPos(self.World.Map))));

			UpdateVisibility();
		}

		static readonly IRenderable[] NoRenderables = new IRenderable[0];

		int flashTicks;
		IRenderable[] renderables = NoRenderables;

		public void Tick()
		{
			UpdateVisibility();

			if (flashTicks > 0)
				flashTicks--;
		}

		void UpdateVisibility()
		{
			var wasVisible = IsVisible;
			var isVisibleTest = shroud.IsVisibleTest;

			// We are doing the following LINQ manually for performance since this is a hot path.
			// IsVisible = !Footprint.Any(isVisibleTest);
			IsVisible = true;
			foreach (var uv in Footprint)
			{
				if (isVisibleTest(uv))
				{
					IsVisible = false;
					break;
				}
			}

			if (IsVisible && !wasVisible)
				NeedRenderables = true;
		}

		public void Flash()
		{
			flashTicks = 5;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (NeedRenderables)
			{
				NeedRenderables = false;
				if (!actor.Disposed)
				{
					IsRendering = true;
					renderables = actor.Render(wr).ToArray();
					IsRendering = false;
				}
			}

			if (flashTicks > 0 && flashTicks % 2 == 0)
			{
				var highlight = wr.Palette("highlight");
				return renderables.Concat(renderables.Where(r => !r.IsDecoration)
					.Select(r => r.WithPalette(highlight)));
			}

			return renderables;
		}

		public bool HasRenderables { get { return renderables.Any(); } }

		public bool ShouldBeRemoved(Player owner)
		{
			// We use a loop here for performance reasons
			foreach (var rfa in removeFrozenActors)
				if (rfa.RemoveActor(actor, owner))
					return true;

			return false;
		}

		public override string ToString()
		{
			return "{0} {1}{2}".F(Info.Name, ID, IsValid ? "" : " (invalid)");
		}
	}

	public class FrozenActorLayer : IRender, ITick, ISync, IRadarSignature
	{
		[Sync] public int VisibilityHash;
		[Sync] public int FrozenHash;

		readonly World world;
		readonly Player owner;
		readonly Dictionary<uint, FrozenActor> frozen;

		public FrozenActorLayer(Actor self)
		{
			world = self.World;
			owner = self.Owner;
			frozen = new Dictionary<uint, FrozenActor>();
		}

		public void Add(FrozenActor fa)
		{
			frozen.Add(fa.ID, fa);
			world.ScreenMap.Add(owner, fa);
		}

		public void Tick(Actor self)
		{
			var remove = new List<uint>();
			VisibilityHash = 0;
			FrozenHash = 0;

			foreach (var kvp in frozen)
			{
				var hash = (int)kvp.Key;
				FrozenHash += hash;

				var frozenActor = kvp.Value;
				frozenActor.Tick();

				if (frozenActor.ShouldBeRemoved(owner))
					remove.Add(kvp.Key);
				else if (frozenActor.IsVisible)
					VisibilityHash += hash;
				else if (frozenActor.Actor == null)
					remove.Add(kvp.Key);
			}

			foreach (var actorID in remove)
			{
				world.ScreenMap.Remove(owner, frozen[actorID]);
				frozen.Remove(actorID);
			}
		}

		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			return world.ScreenMap.FrozenActorsInBox(owner, wr.Viewport.TopLeft, wr.Viewport.BottomRight)
				.Where(f => f.IsVisible)
				.SelectMany(ff => ff.Render(wr));
		}

		public FrozenActor FromID(uint id)
		{
			FrozenActor ret;
			if (!frozen.TryGetValue(id, out ret))
				return null;

			return ret;
		}

		public IEnumerable<Pair<CPos, Color>> RadarSignatureCells(Actor self)
		{
			// Filter only actors that have been discovered by the player
			// HasRenderables is only True once the actor enters the viewport, so is not enough for the minimap
			foreach (var frozenActor in frozen.Values.Where(f => f.NeedRenderables || f.HasRenderables))
				foreach (var cell in frozenActor.OccupiedCells)
					yield return Pair.New(cell, frozenActor.RadarColor);
		}
	}
}
