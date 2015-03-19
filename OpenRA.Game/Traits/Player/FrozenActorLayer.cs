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

namespace OpenRA.Traits
{
	[Desc("Required for FrozenUnderFog to work. Attach this to the player actor.")]
	public class FrozenActorLayerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new FrozenActorLayer(init.Self); }
	}

	public class FrozenActor
	{
		public readonly MPos[] Footprint;
		public readonly CellRegion FootprintRegion;
		public readonly WPos CenterPosition;
		public readonly Rectangle Bounds;
		readonly Actor actor;

		public Player Owner;

		public ITooltipInfo TooltipInfo;
		public Player TooltipOwner;

		public int HP;
		public DamageState DamageState;

		public bool Visible;

		public bool IsRendering { get; private set; }

		public FrozenActor(Actor self, MPos[] footprint, CellRegion footprintRegion, Shroud shroud)
		{
			actor = self;
			Footprint = footprint;
			FootprintRegion = footprintRegion;

			CenterPosition = self.CenterPosition;
			Bounds = self.Bounds;

			UpdateVisibility(shroud);
		}

		public uint ID { get { return actor.ActorID; } }
		public bool IsValid { get { return Owner != null; } }
		public ActorInfo Info { get { return actor.Info; } }
		public Actor Actor { get { return !actor.IsDead ? actor : null; } }

		static readonly IRenderable[] NoRenderables = new IRenderable[0];

		int flashTicks;
		IRenderable[] renderables = NoRenderables;
		bool needRenderables;

		public void Tick(Shroud shroud)
		{
			UpdateVisibility(shroud);

			if (flashTicks > 0)
				flashTicks--;
		}

		void UpdateVisibility(Shroud shroud)
		{
			var wasVisible = Visible;

			// We are doing the following LINQ manually to avoid allocating an extra delegate since this is a hot path.
			// Visible = !Footprint.Any(shroud.IsVisibleTest(FootprintRegion));
			var isVisibleTest = shroud.IsVisibleTest(FootprintRegion);
			Visible = true;
			foreach (var uv in Footprint)
				if (isVisibleTest(uv))
				{
					Visible = false;
					break;
				}

			if (Visible && !wasVisible)
				needRenderables = true;
		}

		public void Flash()
		{
			flashTicks = 5;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (needRenderables)
			{
				needRenderables = false;
				if (!actor.Destroyed)
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
		Dictionary<uint, FrozenActor> frozen;

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
				frozenActor.Tick(self.Owner.Shroud);

				if (frozenActor.Visible)
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
