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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

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
		public readonly HashSet<string> TargetTypes;
		readonly IRemoveFrozenActor[] removeFrozenActors;
		readonly Actor actor;
		readonly Shroud shroud;

		public Player Owner;

		public ITooltipInfo TooltipInfo;
		public Player TooltipOwner;

		public int HP;
		public DamageState DamageState;

		public bool Visible = true;
		public bool NeedRenderables { get; private set; }
		public bool IsRendering { get; private set; }

		public FrozenActor(Actor self, PPos[] footprint, Shroud shroud, bool startsRevealed)
		{
			actor = self;
			this.shroud = shroud;
			NeedRenderables = startsRevealed;
			removeFrozenActors = self.TraitsImplementing<IRemoveFrozenActor>().ToArray();

			// Consider all cells inside the map area (ignoring the current map bounds)
			Footprint = footprint
				.Where(m => shroud.Contains(m))
				.ToArray();

			CenterPosition = self.CenterPosition;
			Bounds = self.Bounds;
			TargetTypes = self.TraitsImplementing<ITargetable>().Where(Exts.IsTraitEnabled).SelectMany(t => t.TargetTypes).ToHashSet();

			UpdateVisibility();
		}

		public uint ID { get { return actor.ActorID; } }
		public bool IsValid { get { return Owner != null; } }
		public ActorInfo Info { get { return actor.Info; } }
		public Actor Actor { get { return !actor.IsDead ? actor : null; } }

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
			var wasVisible = Visible;

			// We are doing the following LINQ manually for performance since this is a hot path.
			// Visible = !Footprint.Any(shroud.IsVisible);
			Visible = true;
			foreach (var puv in Footprint)
			{
				if (shroud.IsVisible(puv))
				{
					Visible = false;
					break;
				}
			}

			if (Visible && !wasVisible)
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

	public class FrozenActorLayer : IRender, ITick, ISync
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
				else if (frozenActor.Visible)
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
				.Where(f => f.Visible)
				.SelectMany(ff => ff.Render(wr));
		}

		public FrozenActor FromID(uint id)
		{
			FrozenActor ret;
			if (!frozen.TryGetValue(id, out ret))
				return null;

			return ret;
		}
	}
}
