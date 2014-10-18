#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Make the unit go prone when under attack, in an attempt to reduce damage.")]
	public class TakeCoverInfo : TurretedInfo
	{
		[Desc("How long (in ticks) the actor remains prone.")]
		public readonly int ProneTime = 100;

		[Desc("Prone movement speed as a percentage of the normal speed.")]
		public readonly int SpeedModifier = 50;

		public readonly WVec ProneOffset = new WVec(85, 0, -171);

		public readonly string ProneSequencePrefix = "prone-";

		public override object Create(ActorInitializer init) { return new TakeCover(init, this); }
	}

	public class TakeCover : Turreted, INotifyDamage, IDamageModifier, ISpeedModifier, ISync, IRenderUnitAnimatedSequenceModifier
	{
		readonly TakeCoverInfo info;
		[Sync] int remainingProneTime = 0;
		bool isProne { get { return remainingProneTime > 0; } }

		public bool IsModifyingSequence { get { return isProne; } }
		public string SequencePrefix { get { return info.ProneSequencePrefix ; } }

		public TakeCover(ActorInitializer init, TakeCoverInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage > 0 && (e.Warhead == null || !e.Warhead.PreventProne)) /* Don't go prone when healed */
			{
				if (!isProne)
					localOffset = info.ProneOffset;

				remainingProneTime = info.ProneTime;
			}
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			if (isProne && --remainingProneTime == 0)
				localOffset = WVec.Zero;
		}

		public int GetDamageModifier(Actor attacker, DamageWarhead warhead)
		{
			return isProne && warhead != null ? warhead.ProneModifier : 100;
		}

		public int GetSpeedModifier()
		{
			return isProne ? info.SpeedModifier : 100;
		}
	}
}
