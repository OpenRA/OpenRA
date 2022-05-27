--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
IdlingUnits = function()
	local lazyUnits = enemy.GetGroundAttackers()

	Utils.Do(lazyUnits, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
end

BaseApwr =  { type = "apwr", pos = CVec.New(5, -9), cost = 500, exists = true }
BaseTent =  { type = "tent", pos = CVec.New(-4, -4), cost = 400, exists = true }
BaseProc =  { type = "proc", pos = CVec.New(0, -8), cost = 1400, exists = true }
BaseWeap =  { type = "weap", pos = CVec.New(-4, -8), cost = 2000, exists = true }
BaseApwr2 = { type = "apwr", pos = CVec.New(6, -5), cost = 500, exists = true }

BaseBuildings = { BaseApwr, BaseTent, BaseProc, BaseWeap, BaseApwr2 }

BuildBase = function()
	for i,v in ipairs(BaseBuildings) do
		if not v.exists then
			BuildBuilding(v)
			return
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
end

BuildBuilding = function(building)
	Trigger.AfterDelay(Actor.BuildTime(building.type), function()
		if CYard.IsDead or CYard.Owner ~= enemy then
			return
		elseif Harvester.IsDead and enemy.Resources <= 299 then
			return
		end

		local actor = Actor.Create(building.type, true, { Owner = enemy, Location = CYardLocation.Location + building.pos })
		enemy.Cash = enemy.Cash - building.cost

		building.exists = true
		Trigger.OnKilled(actor, function() building.exists = false end)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == enemy and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
	end)
end

ProduceInfantry = function()
	if not BaseTent.exists then
		return
	elseif Harvester.IsDead and enemy.Resources <= 299 then
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(3), DateTime.Seconds(9))
	local toBuild = { Utils.Random(AlliedInfantryTypes) }
	local Path = Utils.Random(AttackPaths)
	enemy.Build(toBuild, function(unit)
		InfAttack[#InfAttack + 1] = unit[1]

		if #InfAttack >= 10 then
			SendUnits(InfAttack, Path)
			InfAttack = { }
			Trigger.AfterDelay(DateTime.Minutes(2), ProduceInfantry)
		else
			Trigger.AfterDelay(delay, ProduceInfantry)
		end
	end)
end

ProduceArmor = function()
	if not BaseWeap.exists then
		return
	elseif Harvester.IsDead and enemy.Resources <= 599 then
		return
	end

	local delay = Utils.RandomInteger(DateTime.Seconds(12), DateTime.Seconds(17))
	local toBuild = { Utils.Random(AlliedArmorTypes) }
	local Path = Utils.Random(AttackPaths)
	enemy.Build(toBuild, function(unit)
		ArmorAttack[#ArmorAttack + 1] = unit[1]

		if #ArmorAttack >= 6 then
			SendUnits(ArmorAttack, Path)
			ArmorAttack = { }
			Trigger.AfterDelay(DateTime.Minutes(3), ProduceArmor)
		else
			Trigger.AfterDelay(delay, ProduceArmor)
		end
	end)
end

SendUnits = function(units, waypoints)
	Utils.Do(units, function(unit)
		if not unit.IsDead then
			Utils.Do(waypoints, function(waypoint)
				unit.AttackMove(waypoint.Location)
			end)
			IdleHunt(unit)
		end
	end)
end
