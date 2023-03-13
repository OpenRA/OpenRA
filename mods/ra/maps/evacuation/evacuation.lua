--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
DeathThreshold =
{
	easy = 200,
	normal = 100,
}

TanyaType = "e7"
if Difficulty ~= "easy" then
	TanyaType = "e7.noautotarget"
end

RepairTriggerThreshold =
{
	easy = 50,
	normal = 75,
}

Sams = { Sam1, Sam2, Sam3, Sam4 }
TownUnits =
{
	Einstein, Engineer,
	TownUnit01, TownUnit02, TownUnit03, TownUnit04, TownUnit05, TownUnit06, TownUnit07,
	TownUnit08, TownUnit09, TownUnit10, TownUnit11, TownUnit12, TownUnit13, TownUnit14,
}

ParabombDelay = DateTime.Seconds(30)
ParatroopersDelay = DateTime.Minutes(5)
Paratroopers =
{
	{
		proxy = "powerproxy.paras1",
		entry = BadgerEntryPoint1.Location,
		drop  = BadgerDropPoint1.Location,
	},
	{
		proxy = "powerproxy.paras2",
		entry = BadgerEntryPoint1.Location + CVec.New(3, 0),
		drop  = BadgerDropPoint2.Location,
	},
	{
		proxy = "powerproxy.paras2",
		entry = BadgerEntryPoint1.Location + CVec.New(6, 0),
		drop  = BadgerDropPoint3.Location,
	},
}

AttackGroup = { }
AttackGroupSize = 5
SovietInfantry = { "e1", "e2", "e3" }
SovietVehiclesUpgradeDelay = DateTime.Minutes(4)
SovietVehicleType = "Normal"
SovietVehicles =
{
	Normal = { "3tnk" },
	Upgraded = { "3tnk", "v2rl" },
}
ProductionInterval =
{
	easy = DateTime.Seconds(10),
	normal = DateTime.Seconds(2),
}

ReinforcementsDelay = DateTime.Minutes(16)
ReinforcementsUnits = { "2tnk", "2tnk", "2tnk", "2tnk", "2tnk", "2tnk", "1tnk", "1tnk", "jeep", "e1",
	"e1", "e1", "e1", "e3", "e3", "mcv", "truk", "truk", "truk", "truk", "truk", "truk" }

SpawnAlliedReinforcements = function()
	if Allies2.IsLocalPlayer then
		UserInterface.SetMissionText("")
		Media.PlaySpeechNotification(Allies2, "AlliedReinforcementsArrived")
	end
	Reinforcements.Reinforce(Allies2, ReinforcementsUnits, { ReinforcementsEntryPoint.Location, Allies2BasePoint.Location })
end

Yak = nil
YakAttack = function()
	local targets = Map.ActorsInCircle(YakAttackPoint.CenterPosition, WDist.FromCells(10), function(a)
		return a.Owner == Allies1 and not a.IsDead and a ~= Einstein and a ~= Tanya and a ~= Engineer and Yak.CanTarget(a)
	end)

	if (#targets > 0) then
		Yak.Attack(Utils.Random(targets))
	end
	Yak.Move(Map.ClosestEdgeCell(Yak.Location))
	Yak.Destroy()
	Trigger.OnRemovedFromWorld(Yak, function()
		Yak = nil
	end)
end

SovietTownAttack = function()
	local units = Utils.Shuffle(Utils.Where(Map.ActorsWithTag("TownAttacker"), function(a) return not a.IsDead end))

	Utils.Do(Utils.Take(5, units), function(unit)
		unit.AttackMove(TownPoint.Location)
		Trigger.OnIdle(unit, unit.Hunt)
	end)
end

SendParabombs = function()
	local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = Soviets })
	proxy.TargetAirstrike(ParabombPoint1.CenterPosition, (BadgerEntryPoint2.CenterPosition - ParabombPoint1.CenterPosition).Facing)
	proxy.TargetAirstrike(ParabombPoint2.CenterPosition, (Map.CenterOfCell(BadgerEntryPoint2.Location + CVec.New(0, 3)) - ParabombPoint2.CenterPosition).Facing)
	proxy.Destroy()
end

SendParatroopers = function()
	Utils.Do(Paratroopers, function(para)
		local proxy = Actor.Create(para.proxy, false, { Owner = Soviets })
		local target = Map.CenterOfCell(para.drop)
		local dir = target - Map.CenterOfCell(para.entry)

		local aircraft = proxy.TargetParatroopers(target, dir.Facing)
		Utils.Do(aircraft, function(a)
			Trigger.OnPassengerExited(a, function(t, p)
				IdleHunt(p)
			end)
		end)
		proxy.Destroy()
	end)
end

SendAttackGroup = function()
	if #AttackGroup < AttackGroupSize then
		return
	end

	Utils.Do(AttackGroup, function(unit)
		if not unit.IsDead then
			Trigger.OnIdle(unit, unit.Hunt)
		end
	end)

	AttackGroup = { }
end

ProduceInfantry = function()
	if SovietBarracks.IsDead or SovietBarracks.Owner ~= Soviets then
		return
	end

	Soviets.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceInfantry)
	end)
end

ProduceVehicles = function()
	if SovietWarFactory.IsDead or SovietWarFactory.Owner ~= Soviets then
		return
	end

	Soviets.Build({ Utils.Random(SovietVehicles[SovietVehicleType]) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Difficulty], ProduceVehicles)
	end)
end

NumBaseBuildings = function()
	local buildings = Map.ActorsInBox(AlliedBaseTopLeft.CenterPosition, AlliedBaseBottomRight.CenterPosition, function(a)
		return not a.IsDead and a.Owner == Allies2 and a.HasProperty("StartBuildingRepairs")
	end)

	return #buildings
end

Tick = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 and NumBaseBuildings() == 0 then
		Allies2.MarkFailedObjective(HoldPositionObjective)
	end

	if not Allies2.IsObjectiveCompleted(CutSovietPowerObjective) and Soviets.PowerState ~= "Normal" then
		Allies2.MarkCompletedObjective(CutSovietPowerObjective)
	end

	if not Allies2.IsObjectiveCompleted(LimitLossesObjective) and Allies2.UnitsLost > DeathThreshold[Difficulty] then
		Allies2.MarkFailedObjective(LimitLossesObjective)
	end

	if Allies2.IsLocalPlayer and DateTime.GameTime <= ReinforcementsDelay then
		if DateTime.GameTime % DateTime.Seconds(1) == 0 then
			local time = Utils.FormatTime(ReinforcementsDelay - DateTime.GameTime)
			local timer = UserInterface.Translate("allied-reinforcements-arrive-in", { ["time"] = time })
			UserInterface.SetMissionText(timer)
		end
	else
		UserInterface.SetMissionText("")
	end
end

SetupSoviets = function()
	Soviets.Cash = 1000

	if Difficulty == "easy" then
		Utils.Do(Sams, function(sam)
			local camera = Actor.Create("Camera.SAM", true, { Owner = Allies1, Location = sam.Location })
			Trigger.OnKilledOrCaptured(sam, function()
				camera.Destroy()
			end)
		end)
	end

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == Soviets and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == Soviets and building.Health < (building.MaxHealth * RepairTriggerThreshold[Difficulty] / 100) then
				building.StartBuildingRepairs()
			end
		end)
	end)

	SovietBarracks.IsPrimaryBuilding = true
	SovietBarracks.RallyPoint = SovietRallyPoint.Location
	SovietWarFactory.IsPrimaryBuilding = true
	SovietWarFactory.RallyPoint = SovietRallyPoint.Location

	Trigger.AfterDelay(SovietVehiclesUpgradeDelay, function() SovietVehicleType = "Upgraded" end)
	Trigger.AfterDelay(0, function()
		ProduceInfantry()
		ProduceVehicles()
	end)
end

SetupTriggers = function()
	Trigger.OnKilled(Tanya, function()
		Allies1.MarkFailedObjective(TanyaMustSurviveObjective)
	end)

	Trigger.OnAllKilledOrCaptured(Sams, function()
		Allies1.MarkCompletedObjective(DestroySamSitesObjective)
		ExtractEinsteinObjective = AddPrimaryObjective(Allies1, "wait-for-helicopter-extract-einstein")
		Actor.Create("flare", true, { Owner = Allies1, Location = ExtractionLZ.Location + CVec.New(1, -1) })
		Beacon.New(Allies1, ExtractionLZ.CenterPosition)
		Media.PlaySpeechNotification(Allies1, "SignalFlareNorth")

		ExtractionHeli = Reinforcements.ReinforceWithTransport(Allies1, "tran", nil, { ExtractionLZEntryPoint.Location, ExtractionLZ.Location })[1]
		Trigger.OnKilled(ExtractionHeli, function()
			Allies1.MarkFailedObjective(ExtractEinsteinObjective)
		end)
		Trigger.OnPassengerEntered(ExtractionHeli, function(heli, passenger)
			if passenger == Einstein then
				heli.Move(ExtractionLZEntryPoint.Location)
				heli.Destroy()
				Trigger.OnRemovedFromWorld(heli, function()
					Allies2.MarkCompletedObjective(LimitLossesObjective)
					Allies2.MarkCompletedObjective(HoldPositionObjective)
					Allies1.MarkCompletedObjective(TanyaMustSurviveObjective)
					Allies1.MarkCompletedObjective(EinsteinSurvivalObjective)
					Allies1.MarkCompletedObjective(ExtractEinsteinObjective)
				end)
			end
		end)
	end)

	Trigger.OnEnteredProximityTrigger(TownPoint.CenterPosition, WDist.FromCells(15), function(actor, trigger)
		if actor.Owner == Allies1 then
			ReassignActors(TownUnits, Neutral, Allies1)
			Utils.Do(TownUnits, function(a) a.Stance = "Defend" end)
			Allies1.MarkCompletedObjective(FindEinsteinObjective)
			EinsteinSurvivalObjective = AddPrimaryObjective(Allies1, "keep-einstein-alive-at-all-costs")
			Trigger.OnKilled(Einstein, function()
				Allies1.MarkFailedObjective(EinsteinSurvivalObjective)
			end)
			Trigger.RemoveProximityTrigger(trigger)
			SovietTownAttack()
		end
	end)

	Trigger.OnEnteredProximityTrigger(YakAttackPoint.CenterPosition, WDist.FromCells(5), function(actor, trigger)
		if not (Yak == nil or Yak.IsDead) or actor.Owner ~= Allies1 then
			return
		end

		Yak = Actor.Create("yak", true, { Owner = Soviets, Location = YakEntryPoint.Location, CenterPosition = YakEntryPoint.CenterPosition + WVec.New(0, 0, Actor.CruiseAltitude("yak")) })
		Yak.Move(YakAttackPoint.Location + CVec.New(0, -10))
		Yak.CallFunc(YakAttack)
	end)

	Trigger.AfterDelay(ParabombDelay, SendParabombs)
	Trigger.AfterDelay(ParatroopersDelay, SendParatroopers)
	Trigger.AfterDelay(ReinforcementsDelay, SpawnAlliedReinforcements)
end

SpawnTanya = function()
	Tanya = Actor.Create(TanyaType, true, { Owner = Allies1, Location = TanyaLocation.Location })

	if Difficulty ~= "easy" and Allies1.IsLocalPlayer then
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.DisplayMessage(UserInterface.Translate("tanya-rules-of-engagement"), UserInterface.Translate("tanya"))
		end)
	end
end

ReassignActors = function(actors, from, to)
	Utils.Do(actors, function(a)
		if a.Owner == from then
			a.Owner = to
			a.Stance = "Defend"
		end
	end)
end

WorldLoaded = function()
	Neutral = Player.GetPlayer("Neutral")

	-- Allies is the pre-set owner of units that get assigned to either the second player, if any, or the first player otherwise.
	Allies = Player.GetPlayer("Allies")

	-- Allies1 is the player starting on the right, controlling Tanya
	Allies1 = Player.GetPlayer("Allies1")

	-- Allies2 is the player starting on the left, defending the base
	Allies2 = Player.GetPlayer("Allies2")

	Soviets = Player.GetPlayer("Soviets")

	Utils.Do({ Allies1, Allies2 }, function(player)
		if player and player.IsLocalPlayer then
			InitObjectives(player)
		end
	end)

	if not Allies2 or Allies2.IsLocalPlayer then
		Camera.Position = Allies2BasePoint.CenterPosition
	else
		Camera.Position = ChinookHusk.CenterPosition
	end

	if not Allies2 then
		Allies2 = Allies1
	end

	ReassignActors(Map.ActorsInWorld, Allies, Allies2)
	SpawnTanya()

	TanyaMustSurviveObjective = AddPrimaryObjective(Allies1, "tanya-survive")
	FindEinsteinObjective = AddPrimaryObjective(Allies1, "find-einstein-crashed-helicopter")
	DestroySamSitesObjective = AddPrimaryObjective(Allies1, "destroy-sam-sites")

	HoldPositionObjective = AddPrimaryObjective(Allies2, "hold-position-protect-base")
	local dontLoseMoreThan = UserInterface.Translate("keep-civilians-alive", { ["units"] = DeathThreshold[Difficulty] })
	LimitLossesObjective = AddSecondaryObjective(Allies2, dontLoseMoreThan)
	CutSovietPowerObjective = AddSecondaryObjective(Allies2, "take-out-the-soviet-power-grid")

	SetupTriggers()
	SetupSoviets()
end
