using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public interface IUnitStance
	{
		bool Active { get; set; }
		bool IsDefault { get; }
		void Activate(Actor self);
		void Deactivate(Actor self);
	}

	public class UnitStanceInfo : ITraitInfo
	{
		public readonly bool Default;

		#region ITraitInfo Members

		public virtual object Create(ActorInitializer init)
		{
			throw new Exception("Do not use UnitStance at the rules!");
		}

		#endregion
	}

	public abstract class UnitStance : IUnitStance, ITick
	{
		public int NextScantime;
		public int ScanDelay = 12; // 2x - second
		private bool _unsetFirstTick;

		public UnitStanceInfo Info { get; protected set; }

		public bool IsFirstTick { get; private set; }

		public bool IsScanAvailable
		{
			get
			{
				NextScantime--;
				if (NextScantime <= 0)
				{
					NextScantime = ScanDelay;
					return true;
				}

				return false;
			}
		}

		#region ITick Members

		public virtual void Tick(Actor self)
		{
			if (!Active) return;

			if (IsFirstTick && _unsetFirstTick)
			{
				IsFirstTick = false;
				_unsetFirstTick = false;
			}
			else if (IsFirstTick)
			{
				_unsetFirstTick = true;
				OnFirstTick(self);
			}

			if (IsScanAvailable)
			{
				OnScan(self);
			}
		}

		#endregion

		#region IUnitStance Members

		public bool Active { get; set; }

		public virtual bool IsDefault
		{
			get { return Info.Default; }
		}

		public virtual void Activate(Actor self)
		{
			if (Active) return;

			Active = true;
			IsFirstTick = true;
			NextScantime = 0;
			_unsetFirstTick = false;

			DeactivateOthers(self);
		}

		public virtual void Deactivate(Actor self)
		{
			if (Active)
			{
				Active = false;
			}
		}

		#endregion

		public virtual void DeactivateOthers(Actor self)
		{
			DeactivateOthers(self, this);
		}

		public static bool IsActive<T>(Actor self) where T : UnitStance
		{
			var stance = self.TraitOrDefault<T>();

			return stance != null && stance.Active;
		}

		public static void ActivateDefault(Actor self)
		{
			if (!self.TraitsImplementing<IUnitStance>().Where(t => t.IsDefault).Any())
			{
				// deactive all of them as a default if nobody has a default
				DeactivateOthers(self, null);
				return;
			}

			self.TraitsImplementing<IUnitStance>().Where(t => t.IsDefault).First().Activate(self);
		}

		public static void DeactivateOthers(Actor self, IUnitStance stance)
		{
			self.TraitsImplementing<IUnitStance>().Where(t => t != stance).Do(t => t.Deactivate(self));
		}

		public static bool ReturnFire(Actor self, AttackInfo e, bool allowActivity, bool allowTargetSwitch, bool holdStill)
		{
			if (!self.IsIdle && !allowActivity) return false;
			if (e.Attacker.Destroyed) return false;

			var attack = self.TraitOrDefault<AttackBase>();

			// this unit cannot fight back at all (no guns)
			if (attack == null) return false;

			// if attacking already and force was used, return (ie to respond to attacks while moving around)
			if (attack.IsAttacking && (!allowTargetSwitch)) return false;

			// don't fight back if we dont have the guns to do so
			if (!attack.HasAnyValidWeapons(Target.FromActor(e.Attacker))) return false;

			// don't retaliate against allies
			if (self.Owner.Stances[e.Attacker.Owner] == Stance.Ally) return false;

			// don't retaliate against healers
			if (e.Damage < 0) return false;

			// perform the attack
			AttackTarget(self, e.Attacker, holdStill);

			return true;
		}

		public static bool ReturnFire(Actor self, AttackInfo e, bool allowActivity, bool allowTargetSwitch)
		{
			return ReturnFire(self, e, allowActivity, allowTargetSwitch, false);
		}

		public static bool ReturnFire(Actor self, AttackInfo e, bool allowActivity)
		{
			return ReturnFire(self, e, allowActivity, false);
		}

		public static UnitStance GetActive(Actor self)
		{
			return self.TraitsImplementing<UnitStance>().Where(t => t.Active).FirstOrDefault();
		}

		public static void AttackTarget(Actor self, Actor target, bool holdStill)
		{
			var attack = self.Trait<AttackBase>();

			if (attack != null && target != null)
			{
				self.World.IssueOrder(new Order((holdStill) ? "AttackHold" : "Attack", self, target, false));
			}
		}

		public static void StopAttack(Actor self)
		{
			self.World.IssueOrder(new Order("StopAttack", self, self, false));
		}

		/// <summary>
		/// Called when on the first tick after the stance has been activated
		/// </summary>
		/// <param name="self"></param>
		protected virtual void OnScan(Actor self)
		{
		}

		/// <summary>
		/// Called when on the first tick after the stance has been activated
		/// </summary>
		/// <param name="self"></param>
		protected virtual void OnFirstTick(Actor self)
		{
		}

		public static Actor ScanForTarget(Actor self)
		{
			return self.Trait<AttackBase>().ScanForTarget(self);
		}
	}
}