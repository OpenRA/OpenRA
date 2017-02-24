
WTransWays =
{
	{ WaterUnloadEntry1.Location, WaterUnload1.Location },
	{ WaterUnloadEntry2.Location, WaterUnload2.Location }
}
WTransUnits =
{
	hard = { { "3tnk", "3tnk", "3tnk", "v2rl", "v2rl" }, { "v2rl", "v2rl", "e4", "e4", "3tnk" } },
	normal = { { "e1", "e1", "3tnk", "3tnk", "v2rl" }, { "e4", "e4", "e4", "e4", "v2rl" } },
	easy = { { "e1", "e1", "e1", "e2", "e2" }, { "e2", "e2", "3tnk" } }
}
WTransDelays =
{
	easy = 4,
	normal = 3,
	hard = 1
}

BuildDelays =
{
	easy = 90,
	normal = 60,
	hard = 30
}

WaterAttacks =
{
	easy = 1,
	normal = 2,
	hard = 3
}

WaterAttackTypes =
{
	easy = { "ss" },
	normal = { "ss", "ss" },
	hard = { "ss", "ss", "ss" }
}

VehicleTypes = { "v2rl", "3tnk", "3tnk", "3tnk", "3tnk", "harv" }

InfTypes =
{
	{ "e1", "e1", "e1"},
	{ "e2", "e1", "e1"},
	{ "e4", "e4", "e1"}
}

AttackRallyPoints =
{
	{ SovietOreAttackStart.Location, SovietOreAttack1.Location },
	{ SovietBaseAttack.Location },
	{ SovietOreAttack2.Location }
}

ImportantBuildings = { WeaponsFactory, Airfield, dome2, SovietConyard }
SovietAircraftType = { "yak" }
Yaks = { }
IdlingUnits = { }
IdlingTanks = { tank1, tank2, tank3, tank4, tank5, tank6, tank7, tank8 }
IdlingNavalUnits = { }

InitialiseAttack = function()
	Utils.Do(ImportantBuildings, function(a)
		Trigger.OnDamaged(a, function()
			Utils.Do(IdlingTanks, function(unit)
				if not unit.IsDead then
					unit.Hunt()
				end
			end)
		end)
	end)
end

Attack = 0
ProduceInfantry = function()
	if SovietBarracks.IsDead or SovietBarracks.Owner ~= ussr then
		return
	end

	Attack = Attack + 1
	local toBuild = Utils.Random(InfTypes)
	ussr.Build(toBuild, function(units)
		if Attack == 2 and not AttackTnk1.IsDead then
			units[#units + 1] = AttackTnk1
		elseif Attack == 4 and not AttackTnk2.IsDead then
			units[#units + 1] = AttackTnk2
		end

		SendAttack(units, Utils.Random(AttackRallyPoints))
		Trigger.AfterDelay(DateTime.Seconds(BuildDelays), ProduceInfantry)
	end)
end

ProduceVehicles = function()
	if WeaponsFactory.IsDead or WeaponsFactory.Owner ~= ussr then
		return
	end
	ussr.Build(VehicleTypes, function(units)
		Utils.Do(units, function(unit)
			if unit.Type ~= "harv" then
				IdlingTanks[#IdlingTanks + 1] = unit
			end
		end)
	end)
end

ProduceNaval = function()
	if not shouldProduce and #Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == player and self.Type == "syrd" end) < 1 then
		Trigger.AfterDelay(DateTime.Minutes(1), ProduceNaval)
		return
	end

	shouldProduce = true

	if SubPen.IsDead or SubPen.Owner ~= ussr then
		return
	end

	ussr.Build(WaterAttackTypes, function(units)
		Utils.Do(units, function(unit)
			IdlingNavalUnits[#IdlingNavalUnits + 1] = unit
		end)

		Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(40), ProduceNaval)
		if #IdlingNavalUnits >= WaterAttacks then
			Trigger.AfterDelay(DateTime.Seconds(20), function()
				SendAttack(SetupNavalAttackGroup(), { Harbor.Location })
			end)
		end
	end)
end

ProduceAircraft = function()
	if Airfield.IsDead or Airfield.Owner ~= ussr then
		return
	end

	ussr.Build(SovietAircraftType, function(units)
		local yak = units[1]
		Yaks[#Yaks + 1] = yak

		Trigger.OnKilled(yak, ProduceAircraft)
		if #Yaks == 1 then
			Trigger.AfterDelay(DateTime.Seconds(BuildDelays), ProduceAircraft)
		end

		TargetAndAttack(yak)
	end)
end

TargetAndAttack = function(yak, target)
	if not target or target.IsDead or (not target.IsInWorld) then
		local enemies = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == player and self.HasProperty("Health") end)
		if #enemies > 0 then
			target = Utils.Random(enemies)
		else
			yak.Wait(DateTime.Seconds(5))
		end
	end

	if target and yak.AmmoCount() > 0 then
		yak.Attack(target)
	else
		yak.ReturnToBase()
	end

	yak.CallFunc(function()
		TargetAndAttack(yak, target)
	end)
end

IdleHunt = function(unit) if not unit.IsDead then Trigger.OnIdle(unit, unit.Hunt) end end

SendAttack = function(units, path)
	Utils.Do(units, function(unit)
		unit.Patrol(path, false)
		IdleHunt(unit)
	end)
end

SetupNavalAttackGroup = function()
	local units = { }
	for i = 0, 3 do
		if #IdlingNavalUnits == 0 then
			return units
		end

		local number = Utils.RandomInteger(1, #IdlingNavalUnits)
		if IdlingNavalUnits[number] and not IdlingNavalUnits[number].IsDead then
			units[i] = IdlingNavalUnits[number]
			table.remove(IdlingNavalUnits, number)
		end
	end

	return units
end

WTransWaves = function()
	local way = Utils.Random(WTransWays)
	local units = Utils.Random(WTransUnits)
	local attackUnits = Reinforcements.ReinforceWithTransport(ussr, "lst", units , way, { way[2], way[1] })[2]
	Utils.Do(attackUnits, function(a)
		Trigger.OnAddedToWorld(a, function()
			a.AttackMove(UnitBStopLocation.Location)
			IdleHunt(a)
		end)
	end)

	Trigger.AfterDelay(DateTime.Minutes(WTransDelays), WTransWaves)
end

ActivateAI = function()
	local difficulty = Map.LobbyOption("difficulty")
	WaterAttackTypes = WaterAttackTypes[difficulty]
	WaterAttacks = WaterAttacks[difficulty]
	WTransUnits = WTransUnits[difficulty]
	WTransDelays = WTransDelays[difficulty]
	BuildDelays = BuildDelays[difficulty]

	InitialiseAttack()
	Trigger.AfterDelay(DateTime.Seconds(10), ProduceInfantry)
	Trigger.AfterDelay(DateTime.Minutes(1) + DateTime.Seconds(10), function()
		ProduceAircraft()
		ProduceVehicles()
	end)

	WeaponsFactory.RallyPoint = WeaponMeetPoint.Location
	SubPen.RallyPoint = SubMeetPoint.Location
	Trigger.AfterDelay(DateTime.Minutes(5) + DateTime.Seconds(10), ProduceNaval)
	Trigger.AfterDelay(DateTime.Minutes(WTransDelays), WTransWaves)
end
