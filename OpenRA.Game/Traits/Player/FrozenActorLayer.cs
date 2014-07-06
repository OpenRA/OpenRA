#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
	public class FrozenActorLayerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new FrozenActorLayer(init.self); }
	}

	public class FrozenActor
	{
		public readonly CPos[] Footprint;
		public readonly WPos CenterPosition;
		public readonly Rectangle Bounds;
		readonly Actor actor;

		public IRenderable[] Renderables { private get; set; }
		public Player Owner;

		public string TooltipName;
		public Player TooltipOwner;

		public int HP;
		public DamageState DamageState;

		public bool Visible;

		public FrozenActor(Actor self, IEnumerable<CPos> footprint)
		{
			actor = self;
			Footprint = footprint.ToArray();
			CenterPosition = self.CenterPosition;
			Bounds = self.Bounds.Value;
		}

		public uint ID { get { return actor.ActorID; } }
		public bool IsValid { get { return Owner != null && HasRenderables; } }
		public ActorInfo Info { get { return actor.Info; } }
		public Actor Actor { get { return !actor.IsDead() ? actor : null; } }

		int flashTicks;
		public void Tick(World world, Shroud shroud)
		{
			Visible = true;
			foreach (var pos in Footprint)
			{
				if (shroud.IsVisible(pos))
				{
					Visible = false;
					break;
				}
			}

			if (flashTicks > 0)
				flashTicks--;
		}

		public void Flash()
		{
			flashTicks = 5;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (Renderables == null)
				return SpriteRenderable.None;

			if (flashTicks > 0 && flashTicks % 2 == 0)
			{
				var highlight = wr.Palette("highlight");
				return Renderables.Concat(Renderables.Where(r => !r.IsDecoration)
					.Select(r => r.WithPalette(highlight)));
			}

			return Renderables;
		}

		public bool HasRenderables { get { return Renderables != null && Renderables.Any(); } }

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

			foreach (var kv in frozen)
			{
				FrozenHash += (int)kv.Key;

				kv.Value.Tick(self.World, self.Owner.Shroud);
				if (kv.Value.Visible)
					VisibilityHash += (int)kv.Key;

				if (!kv.Value.Visible && kv.Value.Actor == null)
					remove.Add(kv.Key);
			}

			foreach (var r in remove)
			{
				world.ScreenMap.Remove(owner, frozen[r]);
				frozen.Remove(r);
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
