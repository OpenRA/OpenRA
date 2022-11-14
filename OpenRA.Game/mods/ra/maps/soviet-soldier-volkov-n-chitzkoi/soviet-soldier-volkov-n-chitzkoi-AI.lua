--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

AlliedInfantryTypes = { "e1", "e3" }
if Difficulty == "easy" then
	AlliedArmorTypes = { "1tnk", "1tnk" }
else
	AlliedArmorTypes = { "1tnk", "2tnk" }
end
if Difficulty == "hard" then
	AlliedNavyGuard = { "ca", "ca" }
else
	AlliedNavyGuard = { "ca" }
end
ArmorAttackNumbers =
{
	easy = 2,
	normal = 5,
	hard = 8
}
ArmorAttackDelays =
{
	easy = DateTime.Seconds(45),
	normal = DateTime.Seconds(30),
	hard = DateTime.Seconds(10)
}
AlliedWarFactRally = { waypoint2, waypoint9, waypoint10, waypoint11 }
InfAttack = { }
ArmorAttack = { }

SendAttackToBase = function(units)
	Utils.Do(units, function(unit)
		if not unit.IsDead and unit.HasProperty("Hunt") then
			unit.AttackMove(waypoint77.Location, 2)
			Trigger.OnIdle(unit, unit.Hunt)
		end
	end)
end

UnitsJustHunt = function(units)
	Utils.Do(units, function(unit)
		if not unit.IsDead and unit.HasProperty("Hunt") then
			Trigger.OnIdle(unit, unit.Hunt)
		end
	end)
end

ProduceInfantry = function()
	if AlliedBarracks01.IsDead then
		return
	elseif (OreRefinery01.IsDead and OreRefinery02.IsDead or GreeceHarvestersAreDead) and greece.Resources <= 299 then
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(1), DateTime.Seconds(2))
	local toBuild = { Utils.Random(AlliedInfantryTypes) }
	greece.Build(toBuild, function(unit)
		InfAttack[#InfAttack + 1] = unit[1]

		if #InfAttack >= 5 then
			UnitsJustHunt(InfAttack)
			InfAttack = { }
			Trigger.AfterDelay(DateTime.Seconds(1), ProduceInfantry)
		else
			Trigger.AfterDelay(delay, ProduceInfantry)
		end
	end)
end

ProduceArmor = function()
	if AlliedWarFact01.IsDead and AlliedWarFact02.IsDead then
		return
	elseif (OreRefinery01.IsDead and OreRefinery02.IsDead or GreeceHarvestersAreDead) and greece.Resources <= 699 then
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(7), DateTime.Seconds(10))
	local toBuild = { Utils.Random(AlliedArmorTypes) }
	local Rally = Utils.Random(AlliedWarFactRally)
	Utils.Do(AlliedWarFact, function(fact) fact.RallyPoint = Rally.Location end)
	greece.Build(toBuild, function(unit)
		ArmorAttack[#ArmorAttack + 1] = unit[1]

		if #ArmorAttack >= ArmorAttackNumbers[Difficulty] then
			SendAttackToBase(ArmorAttack)
			ArmorAttack = { }
			Trigger.AfterDelay(ArmorAttackDelays[Difficulty], ProduceArmor)
		else
			Trigger.AfterDelay(delay, ProduceArmor)
		end
	end)
end

ProduceNavyGuard = function()
	if NavalYard01.IsDead then
		return
	elseif (OreRefinery01.IsDead and OreRefinery02.IsDead or GreeceHarvestersAreDead) and greece.Resources <= 2399 then
		return
	end
	NavalYard01.RallyPoint = waypoint26.Location
	greece.Build(AlliedNavyGuard, function(nvgrd)
		Utils.Do(nvgrd, function(unit)
			Trigger.OnKilled(unit, ProduceNavyGuard)
		end)
	end)
end
