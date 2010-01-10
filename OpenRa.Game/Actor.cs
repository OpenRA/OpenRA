using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game
{
	class Actor
	{
		[Sync]
		public readonly TypeDictionary traits = new TypeDictionary();
		
		[Obsolete]
		public readonly LegacyUnitInfo LegacyInfo;
		public readonly NewUnitInfo Info;

		public readonly uint ActorID;
		[Sync]
		public int2 Location;
		[Sync]
		public Player Owner;
		[Sync]
		public int Health;
		IActivity currentActivity;

		public Actor( ActorInfo info, int2 location, Player owner )
		{
			ActorID = Game.world.NextAID();
			LegacyInfo = (LegacyUnitInfo)info; // temporary
			Location = location;
			CenterLocation = Traits.Util.CenterOfCell(Location);
			Owner = owner;

			if (LegacyInfo == null) return;

			Health = LegacyInfo.Strength;	/* todo: fix walls, etc so this is always true! */

			if( LegacyInfo.Traits == null )
				throw new InvalidOperationException( "No Actor traits for {0}; add Traits= to units.ini for appropriate unit".F(LegacyInfo.Name) );

			Info = Rules.NewUnitInfo[LegacyInfo.Name.ToLowerInvariant()];

			foreach (var trait in Info.Traits.WithInterface<ITraitInfo>())
				traits.Add(trait.Create(this));
		}

		public void Tick()
		{
			while (currentActivity != null)
			{
				var a = currentActivity;
				currentActivity = a.Tick(this) ?? new Idle();
				if (a == currentActivity || currentActivity is Idle) break;
			}

			foreach (var tick in traits.WithInterface<ITick>())
				tick.Tick(this);
		}

		public bool IsIdle
		{
			get { return currentActivity == null || currentActivity is Idle; }
		}

		public float2 CenterLocation;
		public float2 SelectedSize
		{
			get
			{
				if (LegacyInfo != null && LegacyInfo.SelectionSize != null)
					return new float2(LegacyInfo.SelectionSize[0], LegacyInfo.SelectionSize[1]);

				var firstSprite = Render().FirstOrDefault();
				if (firstSprite.Sprite == null) return float2.Zero;
				return firstSprite.Sprite.size;
			}
		}

		public IEnumerable<Renderable> Render()
		{
			var mods = traits.WithInterface<IRenderModifier>();
			var sprites = traits.WithInterface<IRender>().SelectMany(x => x.Render(this));
			return mods.Aggregate(sprites, (m, p) => p.ModifyRender(this, m));
		}

		public Order Order( int2 xy, MouseInput mi )
		{
			if (Owner != Game.LocalPlayer)
				return null;

			if (!Rules.Map.IsInMap(xy.X, xy.Y))
				return null;
			
			var loc = mi.Location + Game.viewport.Location;
			var underCursor = Game.FindUnits(loc, loc).FirstOrDefault();

			if (underCursor != null && !underCursor.LegacyInfo.Selectable)
				underCursor = null;

			return traits.WithInterface<IIssueOrder>()
				.Select( x => x.IssueOrder( this, xy, mi, underCursor ) )
				.FirstOrDefault( x => x != null );
		}

		public RectangleF GetBounds(bool useAltitude)
		{
			var size = SelectedSize;
			var loc = CenterLocation - 0.5f * size;
			if (LegacyInfo != null && LegacyInfo.SelectionSize != null && LegacyInfo.SelectionSize.Length > 2)
				loc += new float2(LegacyInfo.SelectionSize[2], LegacyInfo.SelectionSize[3]);

			if (useAltitude)
			{
				var unit = traits.GetOrDefault<Unit>();
				if (unit != null) loc -= new float2(0, unit.Altitude);
			}

			return new RectangleF(loc.X, loc.Y, size.X, size.Y);
		}

		public bool IsDead { get { return Health <= 0; } }
		public bool IsInWorld { get; set; }

		public DamageState GetDamageState()
		{
			if (Health <= 0) return DamageState.Dead;
			var halfStrength = LegacyInfo.Strength * Rules.General.ConditionYellow;
			return Health < halfStrength ? DamageState.Half : DamageState.Normal;
		}

		public void InflictDamage(Actor attacker, int damage, WarheadInfo warhead)
		{
			if (IsDead) return;		/* overkill! don't count extra hits as more kills! */

			var oldState = GetDamageState();

			/* apply the damage modifiers, if we have any. */
			damage = (int)traits.WithInterface<IDamageModifier>().Aggregate(
				(float)damage, (a, t) => t.GetDamageModifier() * a);

			Health -= damage;
			if (Health <= 0)
			{
				Health = 0;
				if (attacker.Owner != null)
					attacker.Owner.Kills++;

				Game.world.AddFrameEndTask(w => w.Remove(this));
			}
			if (Health > LegacyInfo.Strength)
				Health = LegacyInfo.Strength;

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
	}
}
