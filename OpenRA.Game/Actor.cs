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
		public Cached<Rectangle> Bounds;
		public Cached<Rectangle> ExtendedBounds;

		public IOccupySpace OccupiesSpace { get { return occupySpace.Value; } }

		public int2 Location { get { return occupySpace.Value.TopLeft; } }

		public int2 CenterLocation
		{
			get
			{
				if (HasLocation == null)
					HasLocation = Trait<IHasLocation>();
				return HasLocation.PxPosition;
			}
		}

		[Sync]
		public Player Owner;

		Activity currentActivity;
		public Group Group;

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

			Move = Lazy.New( () => TraitOrDefault<IMove>() );

			Size = Lazy.New(() =>
			{
				var si = Info.Traits.GetOrDefault<SelectableInfo>();
				if (si != null && si.Bounds != null)
					return new int2(si.Bounds[0], si.Bounds[1]);

				// auto size from render
				var firstSprite = TraitsImplementing<IRender>().SelectMany(ApplyIRender).FirstOrDefault();
				if (firstSprite.Sprite == null) return int2.Zero;
				return (firstSprite.Sprite.size * firstSprite.Scale).ToInt2();
			});

			ApplyIRender = x => x.Render(this);
			ApplyRenderModifier = (m, p) => p.ModifyRender(this, m);

			Bounds = Cached.New( () => CalculateBounds(false) );
			ExtendedBounds = Cached.New( () => CalculateBounds(true) );
		}

		public void Tick()
		{
			Bounds.Invalidate();
			ExtendedBounds.Invalidate();

			currentActivity = Util.RunActivity( this, currentActivity );
		}

		public bool IsIdle
		{
			get { return currentActivity == null; }
		}

		OpenRA.FileFormats.Lazy<int2> Size;

		// note: these delegates are cached to avoid massive allocation.
		Func<IRender, IEnumerable<Renderable>> ApplyIRender;
		Func<IEnumerable<Renderable>, IRenderModifier, IEnumerable<Renderable>> ApplyRenderModifier;
		public IEnumerable<Renderable> Render()
		{
			var mods = TraitsImplementing<IRenderModifier>();
			var sprites = TraitsImplementing<IRender>().SelectMany(ApplyIRender);
			return mods.Aggregate(sprites, ApplyRenderModifier);
		}

		// When useAltitude = true, the bounding box is extended
		// vertically to altitude = 0 to support FindUnitsInCircle queries
		// When false, the bounding box is given for the actor
		// at its current altitude
		Rectangle CalculateBounds(bool useAltitude)
		{
			var size = Size.Value;
			var loc = CenterLocation - size / 2;

			var si = Info.Traits.GetOrDefault<SelectableInfo>();
			if (si != null && si.Bounds != null && si.Bounds.Length > 2)
			{
				loc.X += si.Bounds[2];
				loc.Y += si.Bounds[3];
			}

			var move = Move.Value;
			if (move != null)
			{
				loc.Y -= move.Altitude;
				if (useAltitude)
					size = new int2(size.X, size.Y + move.Altitude);
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

		// todo: move elsewhere.
		public void ChangeOwner(Player newOwner)
		{
			World.AddFrameEndTask(w =>
			{
				// momentarily remove from world so the ownership queries don't get confused
				w.Remove(this);
				Owner = newOwner;
				w.Add(this);
			});
		}
	}
}
