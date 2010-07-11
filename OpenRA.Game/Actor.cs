#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
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
		[Sync]
		public int Health;

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
				Health = this.GetMaxHP();

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
				.Where(a => a.Info.Traits.Contains<SelectableInfo>())
				.OrderByDescending(a => a.Info.Traits.Get<SelectableInfo>().Priority)
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

		public bool IsDead { get { return Health <= 0; } }
		public bool IsInWorld { get; set; }
		public bool RemoveOnDeath = true;

		public DamageState GetDamageState()
		{
			if (Health <= 0)
				return DamageState.Dead;
			
			if (Health < this.GetMaxHP() * World.Defaults.ConditionYellow)
				return DamageState.Half;

			return DamageState.Normal;
		}

		public void InflictDamage(Actor attacker, int damage, WarheadInfo warhead)
		{
			if (IsDead) return;		/* overkill! don't count extra hits as more kills! */

			var oldState = GetDamageState();

			/* apply the damage modifiers, if we have any. */
			var modifier = (float)traits.WithInterface<IDamageModifier>()
				.Select(t => t.GetDamageModifier(warhead)).Product();

			damage = (int)(damage * modifier);

			Health -= damage;
			if (Health <= 0)
			{
				Health = 0;

				attacker.Owner.Kills++;
				Owner.Deaths++;

				if (RemoveOnDeath)
					World.AddFrameEndTask(w => w.Remove(this));

				Log.Write("debug", "{0} #{1} killed by {2} #{3}", Info.Name, ActorID, attacker.Info.Name, attacker.ActorID);
			}

			var maxHP = this.GetMaxHP();

			if (Health > maxHP)	Health = maxHP;

//			Log.Write("debug", "InflictDamage: {0} #{1} -> {2} #{3} raw={4} adj={5} hp={6} mod={7}",
//				attacker.Info.Name, attacker.ActorID, Info.Name, ActorID, rawDamage, damage, Health, modifier);

			var newState = GetDamageState();

			foreach (var nd in traits.WithInterface<INotifyDamage>())
				nd.Damaged(this, new AttackInfo
				{
					Attacker = attacker,
					Damage = damage,
					DamageState = newState,
					DamageStateChanged = newState != oldState,
					Warhead = warhead
				});
		}

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
