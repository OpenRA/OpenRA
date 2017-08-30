using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Yupgi_alert.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("Can this actor be tracted with a traction beam?")]
	public class TractableInfo : ConditionalTraitInfo
	{
		[Desc("Condition to grant when the unit is under tracting state")]
		[GrantedConditionReference]
		public readonly string TractingCondition = null;

		[Desc("Altitude this victim gets tracted at.")]
		public readonly WDist CruiseAltitude = new WDist(1280);

		[Desc("How fast does this actor get dragged?",
			"0 to only lift the target in air (Yuriko in RA3!).",
			"You can also have negative speed for push back.")]
		public readonly WDist CruiseSpeed = new WDist(20);

		[Desc("How fast this actor ascends when being pulled to TractionAltitude?")]
		public readonly WDist AltitudeVelocity = new WDist(20);

		[Desc("Acceleration this actor descends on traction done or cancel")]
		public readonly WDist FallGravity = new WDist(43);

		[Desc("Damage factor that deterimes the actor receives when it falls. (Damage = MaxHP * DamageFactor / 100")]
		public readonly int DamageFactor = 25;

		[Desc("Minimum altitude where this victim is considered airborne")]
		public readonly int MinAirborneAltitude = 1;

		[Desc("When the unit land on these terrain and their movement speed is 0 on there, we destroy this unit.")]
		public readonly string[] DeathTerrainTypes = { "Rock", "Debris", "Cliffs", "Water", "River" };

		[Desc("We consider traction to be timed out after this period.")]
		public readonly int Timeout = 20;
	
		[GrantedConditionReference]
		[Desc("The condition to grant to self while airborne.")]
		public readonly string AirborneCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while at \"cruise\" altitude.")]
		public readonly string CruisingCondition = null;

		[WeaponReference]
		[Desc("When the unit falls to the ground, we explode this weapon, for showing FX.")]
		public readonly string Explosion = "UnitExplode";

		public WeaponInfo ExplosionWeapon { get; private set; }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);
			ExplosionWeapon = string.IsNullOrEmpty(Explosion) ? null : rules.Weapons[Explosion.ToLowerInvariant()];
		}

		public override object Create(ActorInitializer init) { return new Tractable(init.Self, this); }
	}

	public class Tractable : ConditionalTrait<TractableInfo>, INotifyCreated, ITick
	{
		int airborneToken = ConditionManager.InvalidConditionToken;
		int tractingToken = ConditionManager.InvalidConditionToken;
		int cruisingToken = ConditionManager.InvalidConditionToken;
		int cruiseSpeedMultiplier = 1;
		Actor tractor;
		IMove tractorMove;
		IPositionable positionable;
		IOccupySpace ios;
		ConditionManager conditionManager;

		int timeoutTicks = 0;

		bool airborne = false;
		bool cruising = false;

		public Tractable(Actor self, TractableInfo info) : base(info)
		{
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			positionable = self.TraitOrDefault<IPositionable>();
			ios = self.TraitOrDefault<IOccupySpace>();
		}

		void ITick.Tick(Actor self)
		{
			--timeoutTicks;

			// Check if we are tracted or not.
			if (timeoutTicks <= 0)
			{
				timeoutTicks = 0;
				EndTract(self);
			}

			var altitude = self.World.Map.DistanceAboveTerrain(self.CenterPosition);

			// Not in tracting state.
			if (tractor == null || tractor.IsDead)
			{
				// But it may still fall!
				if (altitude != WDist.Zero && !(self.CurrentActivity is TractionFallToEarth))
					self.QueueActivity(new TractionFallToEarth(self, this));

				return;
			}

			if (altitude < Info.CruiseAltitude)
			{
				int dz = CalcAltitudeDelta(self, altitude, Info.CruiseAltitude);
				SetPosition(self, self.CenterPosition + new WVec(0, 0, dz));
				return;
			}

			// Lets get pulled
			FlyToward(self, tractorMove.NearestMoveableCell(tractor.Location));
		}

		/// <summary>
		/// Pull self to dest.
		/// </summary>
		/// <returns>true when done, false otherwise.</returns>
		bool FlyToward(Actor self, CPos dest)
		{
			var step = self.World.Map.CenterOfCell(dest) - self.CenterPosition;
			if (step.HorizontalLengthSquared == 0)
				return true;

			step = new WVec(step.X, step.Y, 0);
			step = cruiseSpeedMultiplier * Info.CruiseSpeed.Length * step / step.Length;

			var altitude = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
			var altitudeDelta = new WVec(0, 0, CalcAltitudeDelta(self, altitude, Info.CruiseAltitude));

			// SetPosition(self, self.CenterPosition + step); No need for SetPosition!
			positionable.SetVisualPosition(self, self.CenterPosition + step + altitudeDelta);

			return false;
		}

		int CalcAltitudeDelta(Actor self, WDist altitude, WDist targetAltitude)
		{
			if (altitude == targetAltitude)
				return 0;

			var delta = Info.AltitudeVelocity.Length;
			var dz = (targetAltitude - altitude).Length.Clamp(-delta, delta);

			return dz;
		}

		// CnP from Aircraft.cs + modified a little
		public void SetPosition(Actor self, WPos pos)
		{
			positionable.SetPosition(self, pos);

			if (!self.IsInWorld)
				return;

			self.World.UpdateMaps(self, ios);

			var altitude = self.World.Map.DistanceAboveTerrain(pos);
			var isAirborne = altitude.Length >= Info.MinAirborneAltitude;

			if (isAirborne && !airborne)
				OnAirborneAltitudeReached(self);
			else if (!isAirborne && airborne)
				OnAirborneAltitudeLeft(self);

			var isCruising = altitude == Info.CruiseAltitude;

			if (isCruising && !cruising)
				OnCruisingAltitudeReached(self);
			else if (!isCruising && cruising)
				OnCruisingAltitudeLeft(self);
		}

		public void Tract(Actor self, Actor tractor, int cruiseSpeedMultiplier)
		{
			// I am already being pulled by someone else
			if (this.tractor != null && !this.tractor.IsDead && this.tractor != tractor)
				return;

			// I am falling and the activity must not be interrupted.
			if (self.CurrentActivity is TractionFallToEarth)
				return;

			timeoutTicks = Info.Timeout;
			this.cruiseSpeedMultiplier = cruiseSpeedMultiplier;

			// Stop self.
			// No need to drop their attack target though, turreted units should be able to fire.
			self.CancelActivity();

			this.tractor = tractor;
			tractorMove = tractor.TraitOrDefault<IMove>();
			if (conditionManager != null && !string.IsNullOrEmpty(Info.TractingCondition) && tractingToken == ConditionManager.InvalidConditionToken)
				tractingToken = conditionManager.GrantCondition(self, Info.TractingCondition);
		}

		public void EndTract(Actor self)
		{
			tractor = null;
		}

		#region altitudes
		public void RevokeTractingCondition(Actor self)
		{
			if (conditionManager != null && tractingToken != ConditionManager.InvalidConditionToken)
				tractingToken = conditionManager.RevokeCondition(self, tractingToken);
		}

		// CnP from Aircraft.cs
		void OnAirborneAltitudeReached(Actor self)
		{
			if (airborne)
				return;

			airborne = true;
			if (conditionManager != null && !string.IsNullOrEmpty(Info.AirborneCondition) && airborneToken == ConditionManager.InvalidConditionToken)
				airborneToken = conditionManager.GrantCondition(self, Info.AirborneCondition);
		}

		// CnP from Aircraft.cs
		void OnAirborneAltitudeLeft(Actor self)
		{
			if (!airborne)
				return;

			airborne = false;
			if (conditionManager != null && airborneToken != ConditionManager.InvalidConditionToken)
				airborneToken = conditionManager.RevokeCondition(self, airborneToken);
		}

		// CnP from Aircraft.cs
		void OnCruisingAltitudeReached(Actor self)
		{
			if (cruising)
				return;

			cruising = true;
			if (conditionManager != null && !string.IsNullOrEmpty(Info.CruisingCondition) && cruisingToken == ConditionManager.InvalidConditionToken)
				cruisingToken = conditionManager.GrantCondition(self, Info.CruisingCondition);
		}

		// CnP from Aircraft.cs
		void OnCruisingAltitudeLeft(Actor self)
		{
			if (!cruising)
				return;

			cruising = false;
			if (conditionManager != null && cruisingToken != ConditionManager.InvalidConditionToken)
				cruisingToken = conditionManager.RevokeCondition(self, cruisingToken);
		}
		#endregion
	}
}
