#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	public enum DamageModel
	{
		Normal,								// classic RA damage model: point actors, distance-based falloff
		PerCell,							// like RA's "nuke damage"
		HealthPercentage,					// for MAD Tank
		Absolute							// Specify absolute spread ranges with associated factors
	}

	public class DamagerWarheadInfo : BaseWarhead, IWarheadInfo
	{
		[Desc("For Normal DamageModel: Distance from the explosion center at which damage is 1/2.", "For Abolsute DamageModel: Maximum spread of the associated SpreadFactor.")]
		public readonly WRange[] Spread = { new WRange(43) };

		[Desc("What factor to multiply the Damage by for this spread range.", "Each factor specified must have an associated Spread defined.")]
		public readonly float[] SpreadFactor = { 1f };

		[Desc("Size of the area. Damage will be applied to this area.", "Used in PerCell and HealthPercentage damage models.")]
		public readonly int Size = 0;

		[FieldLoader.LoadUsing("LoadVersus")]
		[Desc("Damage vs each armortype. 0% = can't target.")]
		public readonly Dictionary<string, float> Versus;

		[Desc("How much (raw) damage to deal")]
		public readonly int Damage = 0;

		[Desc("Which damage model to use.")]
		public readonly DamageModel DamageModel = DamageModel.Normal;

		[Desc("Infantry death animation to use")]
		public readonly string InfDeath = "1";

		[Desc("Whether we should prevent prone response for infantry.")]
		public readonly bool PreventProne = false;

		[Desc("By what percentage should damage be modified against prone infantry.")]
		public readonly int ProneModifier = 50;

		public DamagerWarheadInfo() : base() { }

		static object LoadVersus(MiniYaml y)
		{
			var nd = y.ToDictionary();
			return nd.ContainsKey("Versus")
				? nd["Versus"].ToDictionary(my => FieldLoader.GetValue<float>("(value)", my.Value))
				: new Dictionary<string, float>();
		}

		public new void DoImpact(WPos pos, WeaponInfo weapon, Actor firedBy, float firepowerModifier)
		{
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);

			switch (DamageModel)
			{
				case DamageModel.Normal:
					{
						var maxSpread = new WRange((int)(Spread[0].Range * (float)Math.Log(Math.Abs(Damage), 2)));
						var hitActors = world.FindActorsInCircle(pos, maxSpread);

						foreach (var victim in hitActors)
						{
							if (IsValidAgainst(victim, firedBy))
							{
								var damage = (int)GetDamageToInflict(pos, victim, firedBy, weapon, firepowerModifier, true);
								victim.InflictDamage(firedBy, damage, this);
							}
						}
					}
					break;

				case DamageModel.PerCell:
					{
						foreach (var t in world.Map.FindTilesInCircle(targetTile, Size))
						{
							foreach (var victim in world.ActorMap.GetUnitsAt(t))
							{
								if (IsValidAgainst(victim, firedBy))
								{
									var damage = (int)GetDamageToInflict(pos, victim, firedBy, weapon, firepowerModifier, false);
									victim.InflictDamage(firedBy, damage, this);
								}
							}
						}
					}
					break;

				case DamageModel.HealthPercentage:
					{
						var range = new WRange(Size * 1024);
						var hitActors = world.FindActorsInCircle(pos, range);

						foreach (var victim in hitActors)
						{
							if (IsValidAgainst(victim, firedBy))
							{
								var damage = GetDamageToInflict(pos, victim, firedBy, weapon, firepowerModifier, false);
								if (damage != 0) // will be 0 if the target doesn't have HealthInfo
								{
									var healthInfo = victim.Info.Traits.Get<HealthInfo>();
									damage = (float)(damage / 100 * healthInfo.HP);
								}

								victim.InflictDamage(firedBy, (int)damage, this);
							}
						}
					}
					break;

				case DamageModel.Absolute:
					{
						for (int i = 0; i < Spread.Length; i++)
						{
							var currentSpread = Spread[i];
							var currentFactor = SpreadFactor[i];
							var previousSpread = WRange.Zero;
							if (i > 0)
								previousSpread = Spread[i - 1];
							if (currentFactor <= 0f)
								continue;

							var hitActors = world.FindActorsInCircle(pos, currentSpread);
							if (previousSpread.Range > 0)
								hitActors.Except(world.FindActorsInCircle(pos, previousSpread));

							foreach (var victim in hitActors)
							{
								if (IsValidAgainst(victim, firedBy))
								{
									var damage = (int)GetDamageToInflict(pos, victim, firedBy, weapon, firepowerModifier * currentFactor, true);
									victim.InflictDamage(firedBy, damage, this);
								}
							}
						}
					}
					break;
			}
		}

		public new float EffectivenessAgainst(ActorInfo ai)
		{
			var health = ai.Traits.GetOrDefault<HealthInfo>();
			if (health == null)
				return 0f;

			var armor = ai.Traits.GetOrDefault<ArmorInfo>();
			if (armor == null || armor.Type == null)
				return 1;

			float versus;
			return Versus.TryGetValue(armor.Type, out versus) ? versus : 1;
		}

		public float GetDamageToInflict(WPos pos, Actor target, Actor firedBy, WeaponInfo weapon, float modifier, bool withFalloff)
		{
			// don't hit air units with splash from ground explosions, etc
			if (!weapon.IsValidAgainst(target, firedBy))
				return 0;

			var healthInfo = target.Info.Traits.GetOrDefault<HealthInfo>();
			if (healthInfo == null)
				return 0;

			var rawDamage = (float)Damage;
			if (withFalloff)
			{
				var distance = Math.Max(0, (target.CenterPosition - pos).Length - healthInfo.Radius.Range);
				var falloff = (float)GetDamageFalloff(distance * 1f / Spread[0].Range);
				rawDamage = (float)(falloff * rawDamage);
			}

			return (float)(rawDamage * modifier * (float)EffectivenessAgainst(target.Info));
		}

		static readonly float[] falloff =
		{
			1f, 0.3678795f, 0.1353353f, 0.04978707f,
			0.01831564f, 0.006737947f, 0.002478752f, 0.000911882f
		};

		static float GetDamageFalloff(float x)
		{
			var u = (int)x;
			if (u >= falloff.Length - 1) return 0;
			var t = x - u;
			return (falloff[u] * (1 - t)) + (falloff[u + 1] * t);
		}
	}
}
