#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA
{
	public class Actor
	{
		[Sync]
		public readonly TypeDictionary traits = new TypeDictionary();
		public readonly ActorInfo Info;

		public readonly World World;
		public readonly uint ActorID;

		public int2 Location { get { return traits.Get<IOccupySpace>().TopLeft; } }
		[Sync]
		public Player Owner;

		IActivity currentActivity;
		public Group Group;

		public Actor(World world, string name, int2 location, Player owner)
		{
			World = world;
			ActorID = world.NextAID();
			Owner = owner;

			var init = new ActorInitializer( this, location );

			if (name != null)
			{
				if (!Rules.Info.ContainsKey(name.ToLowerInvariant()))
					throw new NotImplementedException("No rules definition for unit {0}".F(name.ToLowerInvariant()));

				Info = Rules.Info[name.ToLowerInvariant()];
				foreach (var trait in Info.TraitsInConstructOrder())
					traits.Add(trait.Create(init));
			}

			if( CenterLocation == float2.Zero && traits.Contains<IOccupySpace>() )
				CenterLocation = Traits.Util.CenterOfCell(Location);

			Size = Lazy.New(() =>
			{
				var si = Info.Traits.GetOrDefault<SelectableInfo>();
				if (si != null && si.Bounds != null)
					return new float2(si.Bounds[0], si.Bounds[1]);

				// auto size from render
				var firstSprite = traits.WithInterface<IRender>().SelectMany(x => x.Render(this)).FirstOrDefault();
				if (firstSprite.Sprite == null) return float2.Zero;
				return firstSprite.Sprite.size;
			});
		}

		public void Tick()
		{
			var wasIdle = currentActivity is Idle;
			while (currentActivity != null)
			{
				var a = currentActivity;
				currentActivity = a.Tick(this) ?? new Idle();

				if (a == currentActivity) break;

				if (currentActivity is Idle)
				{
					if (!wasIdle)
						foreach (var ni in traits.WithInterface<INotifyIdle>())
							ni.Idle(this);

					break;
				}
			}
		}

		public bool IsIdle
		{
			get { return currentActivity == null || currentActivity is Idle; }
		}

		public float2 CenterLocation;

        OpenRA.FileFormats.Lazy<float2> Size;

		public IEnumerable<Renderable> Render()
		{
			var mods = traits.WithInterface<IRenderModifier>();
			var sprites = traits.WithInterface<IRender>().SelectMany(x => x.Render(this));
			return mods.Aggregate(sprites, (m, p) => p.ModifyRender(this, m));
		}

		public Order Order( int2 xy, MouseInput mi )
		{
			if (Owner != World.LocalPlayer)
				return null;

			if (!World.Map.IsInMap(xy.X, xy.Y))
				return null;

			var underCursor = World.FindUnitsAtMouse(mi.Location)
				//.Where(a => a.Info.Traits.Contains<SelectableInfo>())
				.OrderByDescending(a => a.Info.Traits.Contains<SelectableInfo>() ? a.Info.Traits.Get<SelectableInfo>().Priority : int.MinValue)
				.FirstOrDefault();
			
			return traits.WithInterface<IIssueOrder>()
				.Select( x => x.IssueOrder( this, xy, mi, underCursor ) )
				.FirstOrDefault( x => x != null );
		}

		public RectangleF GetBounds(bool useAltitude)
		{
			var si = Info.Traits.GetOrDefault<SelectableInfo>();

			var size = Size.Value;
			var loc = CenterLocation - 0.5f * size;
			
			if (si != null && si.Bounds != null && si.Bounds.Length > 2)
				loc += new float2(si.Bounds[2], si.Bounds[3]);

			if (useAltitude)
			{
				var unit = traits.GetOrDefault<Unit>();
				if (unit != null) loc -= new float2(0, unit.Altitude);
			}

			return new RectangleF(loc.X, loc.Y, size.X, size.Y);
		}

		public bool IsInWorld { get; set; }

		public void QueueActivity( IActivity nextActivity )
		{
			if( currentActivity == null )
			{
				currentActivity = nextActivity;
				return;
			}
			var act = currentActivity;
			while( act.NextActivity != null )
			{
				act = act.NextActivity;
			}
			act.NextActivity = nextActivity;
		}

		public void CancelActivity()
		{
			if( currentActivity != null )
				currentActivity.Cancel( this );
		}

		// For pathdebug, et al
		public IActivity GetCurrentActivity()
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
	}

	public class ActorInitializer
	{
		public readonly Actor self;
		public World world { get { return self.World; } }
		public readonly int2 location;

		public ActorInitializer( Actor actor, int2 location )
		{
			this.self = actor;
			this.location = location;
		}
	}
}
