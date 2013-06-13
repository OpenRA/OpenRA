#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA
{
	public class Actor
	{
		public readonly ActorInfo Info;

		public readonly World World;
		public readonly uint ActorID;

		Lazy<IOccupySpace> occupySpace;
		IHasLocation HasLocation;
		Lazy<IMove> Move;
		Lazy<IFacing> Facing;

		public Cached<Rectangle> Bounds;
		public Cached<Rectangle> ExtendedBounds;

		public IOccupySpace OccupiesSpace { get { return occupySpace.Value; } }

		public CPos Location { get { return occupySpace.Value.TopLeft; } }

		public PPos CenterLocation
		{
			get
			{
				if (HasLocation == null)
					HasLocation = Trait<IHasLocation>();
				return HasLocation.PxPosition;
			}
		}

		public WPos CenterPosition
		{
			get
			{
				var altitude = Move.Value != null ? Move.Value.Altitude : 0;
				return CenterLocation.ToWPos(altitude);
			}
		}

		public WRot Orientation
		{
			get
			{
				// TODO: Support non-zero pitch/roll in IFacing (IOrientation?)
				var facing = Facing.Value != null ? Facing.Value.Facing : 0;
				return new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(facing));
			}
		}

		public Shroud.ActorVisibility Sight;

		[Sync] public Player Owner;

		Activity currentActivity;
		public Group Group;
		public int Generation;

		internal Actor(World world, string name, TypeDictionary initDict )
		{
			var init = new ActorInitializer( this, initDict );

			World = world;
			ActorID = world.NextAID();
			if( initDict.Contains<OwnerInit>() )
				Owner = init.Get<OwnerInit,Player>();

			occupySpace = Lazy.New( () => TraitOrDefault<IOccupySpace>() );

			if (name != null)
			{
				if (!Rules.Info.ContainsKey(name.ToLowerInvariant()))
					throw new NotImplementedException("No rules definition for unit {0}".F(name.ToLowerInvariant()));

				Info = Rules.Info[name.ToLowerInvariant()];
				foreach (var trait in Info.TraitsInConstructOrder())
					AddTrait(trait.Create(init));
			}

			Move = Lazy.New(() => TraitOrDefault<IMove>());
			Facing = Lazy.New(() => TraitOrDefault<IFacing>());

			Size = Lazy.New(() =>
			{
				var si = Info.Traits.GetOrDefault<SelectableInfo>();
				if (si != null && si.Bounds != null)
					return new int2(si.Bounds[0], si.Bounds[1]);

				return TraitsImplementing<IAutoSelectionSize>().Select(x => x.SelectionSize(this)).FirstOrDefault();
			});

			if (this.HasTrait<RevealsShroud>())
			{
				Sight = new Shroud.ActorVisibility
				{
					range = this.Trait<RevealsShroud>().RevealRange,
					vis = Shroud.GetVisOrigins(this).ToArray()
				};
			}

			ApplyIRender = (x, wr) => x.Render(this, wr);
			ApplyRenderModifier = (m, p, wr) => p.ModifyRender(this, wr, m);

			Bounds = Cached.New(() => CalculateBounds(false));
			ExtendedBounds = Cached.New(() => CalculateBounds(true));
		}

		public void Tick()
		{
			Bounds.Invalidate();
			ExtendedBounds.Invalidate();

			currentActivity = Traits.Util.RunActivity( this, currentActivity );
		}
		
		public void UpdateSight()
		{
			Sight.vis = Shroud.GetVisOrigins(this).ToArray();
		}

		public bool IsIdle
		{
			get { return currentActivity == null; }
		}

		OpenRA.FileFormats.Lazy<int2> Size;

		// note: these delegates are cached to avoid massive allocation.
		Func<IRender, WorldRenderer, IEnumerable<IRenderable>> ApplyIRender;
		Func<IEnumerable<IRenderable>, IRenderModifier, WorldRenderer, IEnumerable<IRenderable>> ApplyRenderModifier;
		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var mods = TraitsImplementing<IRenderModifier>();
			var sprites = TraitsImplementing<IRender>().SelectMany(x => ApplyIRender(x, wr));
			return mods.Aggregate(sprites, (m,p) => ApplyRenderModifier(m,p,wr));
		}

		// When useAltitude = true, the bounding box is extended
		// vertically to altitude = 0 to support FindUnitsInCircle queries
		// When false, the bounding box is given for the actor
		// at its current altitude
		Rectangle CalculateBounds(bool useAltitude)
		{
			var size = (PVecInt)(Size.Value);
			var loc = CenterLocation - size / 2;

			var si = Info.Traits.GetOrDefault<SelectableInfo>();
			if (si != null && si.Bounds != null && si.Bounds.Length > 2)
			{
				loc += new PVecInt(si.Bounds[2], si.Bounds[3]);
			}

			var move = Move.Value;
			if (move != null)
			{
				loc -= new PVecInt(0, move.Altitude);

				if (useAltitude)
					size = new PVecInt(size.X, size.Y + move.Altitude);
			}

			return new Rectangle(loc.X, loc.Y, size.X, size.Y);
		}

		public bool IsInWorld { get; internal set; }

		public void QueueActivity( bool queued, Activity nextActivity )
		{
			if( !queued )
				CancelActivity();
			QueueActivity( nextActivity );
		}

		public void QueueActivity( Activity nextActivity )
		{
			if( currentActivity == null )
				currentActivity = nextActivity;
			else
				currentActivity.Queue( nextActivity );
		}

		public void CancelActivity()
		{
			if( currentActivity != null )
				currentActivity.Cancel( this );
		}

		public Activity GetCurrentActivity()
		{
			return currentActivity;
		}

		public override int GetHashCode()
		{
			return (int)ActorID;
		}

		public override bool Equals( object obj )
		{
			var o = obj as Actor;
			return ( o != null && o.ActorID == ActorID );
		}

		public override string ToString()
		{
			return "{0} {1}{2}".F( Info.Name, ActorID, IsInWorld ? "" : " (not in world)" );
		}

		public T Trait<T>()
		{
			return World.traitDict.Get<T>( this );
		}

		public T TraitOrDefault<T>()
		{
			return World.traitDict.GetOrDefault<T>( this );
		}

		public IEnumerable<T> TraitsImplementing<T>()
		{
			return World.traitDict.WithInterface<T>( this );
		}

		public bool HasTrait<T>()
		{
			return World.traitDict.Contains<T>( this );
		}

		public void AddTrait( object trait )
		{
			World.traitDict.AddTrait( this, trait );
		}

		public bool Destroyed { get; private set; }

		public void Destroy()
		{
			World.AddFrameEndTask( w =>
			{
				if (Destroyed) return;

				World.Remove( this );
				World.traitDict.RemoveActor( this );
				Destroyed = true;
			} );
		}

		// TODO: move elsewhere.
		public void ChangeOwner(Player newOwner)
		{
			World.AddFrameEndTask(w =>
			{
				var oldOwner = Owner;

				// momentarily remove from world so the ownership queries don't get confused
				w.Remove(this);
				Owner = newOwner;
				Generation++;
				w.Add(this);

				foreach (var t in this.TraitsImplementing<INotifyOwnerChanged>())
					t.OnOwnerChanged(this, oldOwner, newOwner);
			});
		}
	}
}
