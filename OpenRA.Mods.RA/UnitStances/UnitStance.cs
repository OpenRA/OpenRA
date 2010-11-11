using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{

	public class UnitStanceInfo : ITraitInfo
	{
		public readonly bool Default = false;
		public readonly int ScanDelayMin = 12;
		public readonly int ScanDelayMax = 24;

		#region ITraitInfo Members

		public virtual object Create(ActorInitializer init)
		{
			throw new Exception("UnitStanceInfo: Override me!");
		}

		#endregion
	}

	public abstract class UnitStance : ITick, IResolveOrder, ISelectionColorModifier, IPostRenderSelection
	{
		[Sync]
		public int NextScanTime;

		public UnitStanceInfo Info { get; protected set; }
		public abstract Color SelectionColor { get; }

		#region ITick Members

		protected UnitStance(Actor self, UnitStanceInfo info)
		{
			Info = info;
			Active = Info.Default;
		}

		public virtual void Tick(Actor self)
		{
			if (!Active) return;

			TickScan(self);
		}

		private void TickScan(Actor self)
		{
			NextScanTime--;

			if (NextScanTime <= 0)
			{
				NextScanTime = GetNextScanTime(self);
				OnScan(self);
			}
		}

		private int GetNextScanTime(Actor self)
		{
			return self.World.SharedRandom.Next(Info.ScanDelayMin, Info.ScanDelayMax+1);
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
			NextScanTime = 0;
			DeactivateOthers(self);
			OnActivate(self);
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

		public static void DeactivateOthers(Actor self, UnitStance stance)
		{
			self.TraitsImplementing<UnitStance>().Where(t => t != stance).Do(t => t.Deactivate(self));
		}

		public abstract string OrderString { get; }

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
				attack.ResolveOrder(self, new Order((holdStill) ? "AttackHold" : "Attack", self, target, false));
			}
		}

		public static void StopAttack(Actor self)
		{
			if (self.GetCurrentActivity() is Activities.Attack)
				self.GetCurrentActivity().Cancel(self);
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
		protected virtual void OnActivate(Actor self)
		{
		}

		public static Actor ScanForTarget(Actor self)
		{
			return self.Trait<AttackBase>().ScanForTarget(self);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderString)
				return;

			// Its our order, activate the stance
			Activate(self);
		}

		public static void OrderStance(Actor self, UnitStance stance)
		{
			self.World.IssueOrder(new Order(stance.OrderString, self, false));
		}

		public Color GetSelectionColorModifier(Actor self, Color defaultColor)
		{

			if (self.World.LocalPlayer != null && self.Owner.Stances[self.World.LocalPlayer] != Stance.Ally)
				return defaultColor;

			return Active ? SelectionColor : defaultColor;
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (!Active) return;
			if (!self.IsInWorld) return;
			if (self.World.LocalPlayer != null && self.Owner.Stances[self.World.LocalPlayer] != Stance.Ally)
				return;

			RenderStance(self);
		}

		protected virtual string Shape
		{
			get { return "xxxx\nx  x\nx  x\nxxxx"; }
		}

		private void RenderStance(Actor self)
		{
			var bounds = self.GetBounds(true);
			var loc = new float2(bounds.Left, bounds.Top) + new float2(0, 1);
			var max = Math.Max(bounds.Height, bounds.Width);

			var shape = Shape;

			// 'Resize' for large actors
			if (max >= Game.CellSize)
			{
				shape = shape.Replace(" ", "  ");
				shape = shape.Replace("x", "xx");
			}
			var color = Color.FromArgb(125, Color.Black);


			int y = 0;
			var shapeLines = shape.Split('\n');

			foreach (var shapeLine in shapeLines)
			{
				for (int yt = 0; yt < ((max >= Game.CellSize) ? 2 : 1); yt++)
				{
					int x = 0;

					foreach (var shapeKey in shapeLine)
					{
						if (shapeKey == 'x')
						{
							Game.Renderer.LineRenderer.DrawLine(loc + new float2(x, y), loc + new float2(x + 1f, y), color, color);
						}

						x++;
					}
					y++;
				}
			}


			y = 0;
			shapeLines = shape.Split('\n');

			color = SelectionColor;
			foreach (var shapeLine in shapeLines)
			{
				for (int yt = 0; yt < ((max >= Game.CellSize) ? 2 : 1); yt++)
				{
					int x = 0;

					foreach (var shapeKey in shapeLine)
					{
						if (shapeKey == 'x')
						{
							Game.Renderer.LineRenderer.DrawLine(loc + new float2(x + 1, y + 1), loc + new float2(x + 1 + 1f, y + 1), color, color);
						}

						x++;
					}
					y++;
				}
			}
		}
	}
}