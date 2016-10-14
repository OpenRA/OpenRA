
IdlingUnits = { }
Yaks = { }

AttackGroupSizes =
{
	easy = 6,
	normal = 8,
	hard = 10
}

AttackDelays =
{
	easy = { DateTime.Seconds(4), DateTime.Seconds(9) },
	normal = { DateTime.Seconds(2), DateTime.Seconds(7) },
	hard = { DateTime.Seconds(1), DateTime.Seconds(5) }
}

AttackRallyPoints =
{
	{ SovietRally1.Location, SovietRally3.Location, SovietRally5.Location, SovietRally4.Location, SovietRally13.Location, PlayerBase.Location },
	{ SovietRally7.Location, SovietRally10.Location, PlayerBase.Location },
	{ SovietRally1.Location, SovietRally3.Location, SovietRally5.Location, SovietRally4.Location, SovietRally12.Location, PlayerBase.Location },
	{ SovietRally7.Location, ParadropPoint1.Location, PlayerBase.Location },
	{ SovietRally8.Location, SovietRally9.Location, ParadropPoint1.Location, PlayerBase.Location, }
}

SovietInfantryTypes = { "e1", "e1", "e2" }
SovietVehicleTypes = { "3tnk", "3tnk", "3tnk", "ftrk", "ftrk", "apc" }
SovietAircraftType = { "yak" }

AttackOnGoing = false
HoldProduction = false
HarvesterKilled = false

Powr1 = { name = "powr", pos = CPos.New(47, 21), prize = 500, exists = true }
Barr = { name = "barr", pos = CPos.New(53, 26), prize = 400, exists = true }
Proc = { name = "proc", pos = CPos.New(54, 21), prize = 1400, exists = true }
Weap = { name = "weap", pos = CPos.New(48, 28), prize = 2000, exists = true }
Powr2 = { name = "powr", pos = CPos.New(51, 21), prize = 500, exists = true }
Powr3 = { name = "powr", pos = CPos.New(46, 25), prize = 500, exists = true }
Powr4 = { name = "powr", pos = CPos.New(49, 21), prize = 500, exists = true }
Ftur1 = { name = "powr", pos = CPos.New(56, 27), prize = 600, exists = true }
Ftur2 = { name = "powr", pos = CPos.New(51, 32), prize = 600, exists = true }
Ftur3 = { name = "powr", pos = CPos.New(54, 30), prize = 600, exists = true }
Afld1 = { name = "afld", pos = CPos.New(43, 23), prize = 500, exists = true }
Afld2 = { name = "afld", pos = CPos.New(43, 21), prize = 500, exists = true }
BaseBuildings = { Powr1, Barr, Proc, Weap, Powr2, Powr3, Powr4, Ftur1, Ftur2, Ftur3, Afld1, Afld2 }
InitialBase = { Barracks, Refinery, PowerPlant1, PowerPlant2, PowerPlant3, PowerPlant4, Warfactory, Flametur1, Flametur2, Flametur3, Airfield1, Airfield2 }

BuildBase = function()
	if Conyard.IsDead or Conyard.Owner ~= ussr then
		return
	elseif Harvester.IsDead and ussr.Resources <= 299 then
		return
	end

	for i,v in ipairs(BaseBuildings) do
		if not v.exists then
			BuildBuilding(v)
			return
		end
	end

	Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
end

BuildBuilding = function(building)
	Trigger.AfterDelay(Actor.BuildTime(building.name), function()
		local actor = Actor.Create(building.name, true, { Owner = ussr, Location = building.pos })
		ussr.Cash = ussr.Cash - building.prize

		building.exists = true
		Trigger.OnKilled(actor, function() building.exists = false end)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == ussr and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
				DefendActor(actor)
			end
		end)

		Trigger.AfterDelay(DateTime.Seconds(10), BuildBase)
	end)
end

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

SetupAttackGroup = function()
	local units = { }

	for i = 0, AttackGroupSize, 1 do
		if #IdlingUnits == 0 then
			return units
		end

		local number = Utils.RandomInteger(1, #IdlingUnits)

		if IdlingUnits[number] and not IdlingUnits[number].IsDead then
			units[i] = IdlingUnits[number]
			table.remove(IdlingUnits, number)
		end
	end

	return units
end

SendAttack = function()
	if Attacking then
		return
	end
	Attacking = true
	HoldProduction = true

	local units = SetupAttackGroup()
	local path = Utils.Random(AttackRallyPoints)
	Utils.Do(units, function(unit)
		unit.Patrol(path)
		IdleHunt(unit)
	end)

	Trigger.OnAllRemovedFromWorld(units, function()
		Attacking = false
		HoldProduction = false
	end)
end

ProtectHarvester = function(unit)
	DefendActor(unit)
	Trigger.OnKilled(unit, function() HarvesterKilled = true end)
end

DefendActor = function(unit)
	Trigger.OnDamaged(unit, function(self, attacker)
		if AttackOnGoing then
			return
		end
		AttackOnGoing = true

		local Guards = SetupAttackGroup()

		if #Guards <= 0 then
			AttackOnGoing = false
			return
		end

		Utils.Do(Guards, function(unit)
			if not self.IsDead then
				unit.AttackMove(self.Location)
			end
			IdleHunt(unit)
		end)

		Trigger.OnAllRemovedFromWorld(Guards, function() AttackOnGoing = false end)
	end)
end

InitAIUnits = function()
	IdlingUnits = ussr.GetGroundAttackers()

	DefendActor(Conyard)
	for i,v in ipairs(InitialBase) do
		DefendActor(v)
		Trigger.OnDamaged(v, function(building)
			if building.Owner == ussr and building.Health < building.MaxHealth * 3/4 then
				building.StartBuildingRepairs()
			end
		end)

		Trigger.OnKilled(v, function()
			BaseBuildings[i].exists = false
		end)
	end
end

ProduceInfantry = function()
	if HoldProduction then
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceInfantry)
		return
	end

	-- See AttackDelay in WorldLoaded
	local delay = Utils.RandomInteger(AttackDelay[1], AttackDelay[2])
	local toBuild = { Utils.Random(SovietInfantryTypes) }
	ussr.Build(toBuild, function(unit)
		IdlingUnits[#IdlingUnits + 1] = unit[1]
		Trigger.AfterDelay(delay, ProduceInfantry)

		-- See AttackGroupSize in WorldLoaded
		if #IdlingUnits >= (AttackGroupSize * 2.5) then
			SendAttack()
		end
	end)
end

ProduceVehicles = function()
	if HoldProduction then
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceVehicles)
		return
	end

	-- See AttackDelay in WorldLoaded
	local delay = Utils.RandomInteger(AttackDelay[1], AttackDelay[2])
	if HarvesterKilled then
		ussr.Build({ "harv" }, function(harv)
			ProtectHarvester(harv[1])
			HarvesterKilled = false
			Trigger.AfterDelay(delay, ProduceVehicles)
		end)
	else
		local toBuild = { Utils.Random(SovietVehicleTypes) }
		ussr.Build(toBuild, function(unit)
			IdlingUnits[#IdlingUnits + 1] = unit[1]
			Trigger.AfterDelay(delay, ProduceVehicles)

			-- See AttackGroupSize in WorldLoaded
			if #IdlingUnits >= (AttackGroupSize * 2.5) then
				SendAttack()
			end
		end)
	end
end

ProduceAircraft = function()
	ussr.Build(SovietAircraftType, function(units)
		local yak = units[1]
		Yaks[#Yaks + 1] = yak

		Trigger.OnKilled(yak, ProduceAircraft)
		if #Yaks == 1 then
			Trigger.AfterDelay(DateTime.Minutes(1), ProduceAircraft)
		end

		TargetAndAttack(yak)
	end)
end

TargetAndAttack = function(yak, target)
	if not target or target.IsDead or (not target.IsInWorld) then
		local enemies = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == player and self.HasProperty("Health") and yak.CanTarget(self) end)

		if #enemies > 0 then
			target = Utils.Random(enemies)
		end
	end

	if target and yak.AmmoCount() > 0 and yak.CanTarget(target) then
		yak.Attack(target)
	else
		yak.ReturnToBase()
	end

	yak.CallFunc(function()
		TargetAndAttack(yak, target)
	end)
end

ActivateAI = function()
	InitAIUnits()
	ProtectHarvester(Harvester)

	local difficulty = Map.LobbyOption("difficulty")
	AttackDelay = AttackDelays[difficulty]
	AttackGroupSize = AttackGroupSizes[difficulty]
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		ProduceInfantry()
		ProduceVehicles()
		ProduceAircraft()
	end)
end
