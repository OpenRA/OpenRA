using System;
using System.Drawing;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class UnitStanceDefensiveInfo : UnitStanceInfo
	{
		public override object Create(ActorInitializer init) { return new UnitStanceDefensive(init.self, this); }
		public readonly int MaxDistance = 5;
	}

	/// <summary>
	/// Return Fire
	/// 
	/// Will fire only when fired upon
	/// </summary>
	public class UnitStanceDefensive : UnitStance, INotifyDamage
	{
		public enum ETargetType
		{
			None,
			Location,
			Actor
		}

		[Sync] public int MaxDistance;
		public Target DefendTarget = Target.None;
		public ETargetType TargetType = ETargetType.None;
		public bool WaitingForIdle = false;
		[Sync]
		public bool IsReturning { get; protected set; }

		public UnitStanceDefensive(Actor self, UnitStanceDefensiveInfo info)
			: base(self, info)
		{
			MaxDistance = info.MaxDistance;

			base.AllowMultiTrigger = true;
		}

		protected override void OnActivate(Actor self)
		{
			DefendThis(self.CenterLocation);

			if (!self.IsIdle)
				WaitForIdle();
		}

		protected void DefendThis(float2 target)
		{
			DefendTarget = Target.FromPos(target);
			TargetType = ETargetType.Location;
		}

		protected void DefendThis(Actor target)
		{
			DefendTarget = Target.FromActor(target);
			TargetType = ETargetType.Actor;
		}

		protected override void OnScan(Actor self)
		{
			if (TargetType == ETargetType.None) return;
			if (IsReturning) return;
			if (!self.IsIdle) return;
			if (!self.HasTrait<AttackBase>()) return;

			var target = ScanForTarget(self);
			if (target == null)
				return;

			AttackTarget(self, target, false);
		}

		protected override void OnTick(Actor self)
		{
			if (!self.HasTrait<AttackBase>()) return;

			// when the unit is doing nothing or the target actor is gone, tell him to defend the current location
			if ((WaitingForIdle && self.IsIdle) || (self.IsIdle && (TargetType == ETargetType.Actor && !DefendTarget.IsValid)))
			{
				IsReturning = false;
				WaitingForIdle = false;
				DefendThis(self.CenterLocation);

				return;
			}
			if (IsReturning && self.IsIdle)
			{
				IsReturning = false;
			}
			
			if (TargetType != ETargetType.None)
			{
				if ((self.CenterLocation - DefendTarget.CenterLocation).Length > MaxDistance * Game.CellSize)
				{
					Return(self);
				}
			}
		}
		
		protected override void OnOrder(Actor self, Order order)
		{
			WaitForIdle();
		}

		private void WaitForIdle()
		{
			// could be an attack or move order ... => 'disable' the stance for now (invalidate the target)
			DefendTarget = Target.None;
			TargetType = ETargetType.None;
			WaitingForIdle = true;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!Active) return;
			if (TargetType == ETargetType.None) return;
			if (IsReturning) return;
			if (!self.HasTrait<AttackBase>()) return;

			ReturnFire(self, e, false); // only triggers when standing still
		}

		public override string OrderString
		{
			get { return "StanceDefensive"; }
		}

		public override Color SelectionColor
		{
			get { return Color.LightGoldenrodYellow; }
		}

		protected override string Shape
		{
			get { return "xxxx\nxxxx"; }
		}
 
		protected void Return(Actor self)
		{
			if ((TargetType == ETargetType.None) || (!DefendTarget.IsValid && (TargetType == ETargetType.Actor && !DefendTarget.IsValid))) return;
			IsReturning = true;
			
			var attackBase = self.TraitOrDefault<AttackBase>();



			// Reset the attack target => otherwise it will not pick up enemies anymore!

			// This should result in unsetting the target (could do it directly, this seems more 'valid')
			self.World.AddFrameEndTask(w =>
			{
				self.CancelActivity();
				attackBase.ResolveOrder(self, new Order("Stop", self));
				self.QueueActivity(self.Trait<Mobile>().MoveWithinRange(DefendTarget, 1));
				WaitForIdle();
			});
		}
	}
}