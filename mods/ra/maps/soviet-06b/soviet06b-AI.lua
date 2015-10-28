IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

IdlingUnits = function()
	local lazyUnits = enemy.GetGroundAttackers()

	Utils.Do(lazyUnits, function(unit)
		Trigger.OnDamaged(unit, function()
			Trigger.ClearAll(unit)
			Trigger.AfterDelay(0, function() IdleHunt(unit) end)
		end)
	end)
end

BaseBuildings =
{
	{ "apwr", CVec.New(5, -9), 500, true },
	{ "tent", CVec.New(-4, -4), 400, true },
	{ "proc", CVec.New(0, -8), 1400, true },
	{ "weap", CVec.New(-4, -8), 2000, true },
	{ "apwr", CVec.New(6, -5), 500, true }
}

BuildBase = function()
	if CYard.IsDead or CYard.Owner ~= enemy then
		return
	elseif Harvester.IsDead and enemy.Resources <= 299 then
		return
	end

	for i,v in ipairs(BaseBuildings) do
		if not v[4] then
			BuildBuilding(v)
			return
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
end

BuildBuilding = function(building)
	Trigger.AfterDelay(Actor.BuildTime(building[1]), function()
		local actor = Actor.Create(building[1], true, { Owner = enemy, Location = CYardLocation.Location + building[2] })
		enemy.Cash = enemy.Cash - building[3]

		building[4] = true
		Trigger.OnKilled(actor, function() building[4] = false end)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == enemy and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
	end)
end

ProduceInfantry = function()
	if not BaseBuildings[2][4] then
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
	if not BaseBuildings[4][4] then
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
