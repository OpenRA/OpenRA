DeathThreshold =
{
	easy = 200,
	normal = 100,
}

TanyaType = "e7"
TanyaStance = "AttackAnything"
if Map.LobbyOption("difficulty") ~= "easy" then
	TanyaType = "e7.noautotarget"
	TanyaStance = "HoldFire"
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
	if allies2.IsLocalPlayer then
		UserInterface.SetMissionText("")
		Media.PlaySpeechNotification(allies2, "AlliedReinforcementsArrived")
	end
	Reinforcements.Reinforce(allies2, ReinforcementsUnits, { ReinforcementsEntryPoint.Location, Allies2BasePoint.Location })
end

Yak = nil
YakAttack = function(yak)
	local targets = Map.ActorsInCircle(YakAttackPoint.CenterPosition, WDist.FromCells(10), function(a)
		return a.Owner == allies1 and not a.IsDead and a ~= Einstein and a ~= Tanya and a ~= Engineer
	end)

	if (#targets > 0) then
		yak.Attack(Utils.Random(targets))
	end
	yak.Move(Map.ClosestEdgeCell(yak.Location))
	yak.Destroy()
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
	local proxy = Actor.Create("powerproxy.parabombs", false, { Owner = soviets })
	proxy.SendAirstrikeFrom(BadgerEntryPoint2.Location, ParabombPoint1.Location)
	proxy.SendAirstrikeFrom(BadgerEntryPoint2.Location + CVec.New(0, 3), ParabombPoint2.Location)
	proxy.Destroy()
end

SendParatroopers = function()
	Utils.Do(Paratroopers, function(para)
		local proxy = Actor.Create(para.proxy, false, { Owner = soviets })
		local units = proxy.SendParatroopersFrom(para.entry, para.drop)
		proxy.Destroy()

		Utils.Do(units, function(unit)
			Trigger.OnIdle(unit, function(a)
				if a.IsInWorld then
					a.Hunt()
				end
			end)
		end)
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
	if SovietBarracks.IsDead or SovietBarracks.Owner ~= soviets then
		return
	end

	soviets.Build({ Utils.Random(SovietInfantry) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Map.LobbyOption("difficulty")], ProduceInfantry)
	end)
end

ProduceVehicles = function()
	if SovietWarFactory.IsDead or SovietWarFactory.Owner ~= soviets then
		return
	end

	soviets.Build({ Utils.Random(SovietVehicles[SovietVehicleType]) }, function(units)
		table.insert(AttackGroup, units[1])
		SendAttackGroup()
		Trigger.AfterDelay(ProductionInterval[Map.LobbyOption("difficulty")], ProduceVehicles)
	end)
end

NumBaseBuildings = function()
	local buildings = Map.ActorsInBox(AlliedBaseTopLeft.CenterPosition, AlliedBaseBottomRight.CenterPosition, function(a)
		return not a.IsDead and a.Owner == allies2 and a.HasProperty("StartBuildingRepairs")
	end)

	return #buildings
end

Tick = function()
	if DateTime.GameTime > 1 and DateTime.GameTime % 25 == 0 and NumBaseBuildings() == 0 then
		allies2.MarkFailedObjective(objHoldPosition)
	end

	if not allies2.IsObjectiveCompleted(objCutSovietPower) and soviets.PowerState ~= "Normal" then
		allies2.MarkCompletedObjective(objCutSovietPower)
	end

	if not allies2.IsObjectiveCompleted(objLimitLosses) and allies2.UnitsLost > DeathThreshold[Map.LobbyOption("difficulty")] then
		allies2.MarkFailedObjective(objLimitLosses)
	end

	if allies2.IsLocalPlayer and DateTime.GameTime <= ReinforcementsDelay then
		UserInterface.SetMissionText("Allied reinforcements arrive in " .. Utils.FormatTime(ReinforcementsDelay - DateTime.GameTime))
	else
		UserInterface.SetMissionText("")
	end
end

SetupSoviets = function()
	soviets.Cash = 1000

	if Map.LobbyOption("difficulty") == "easy" then
		Utils.Do(Sams, function(sam)
			local camera = Actor.Create("Camera.SAM", true, { Owner = allies1, Location = sam.Location })
			Trigger.OnKilledOrCaptured(sam, function()
				camera.Destroy()
			end)
		end)
	end

	local buildings = Utils.Where(Map.ActorsInWorld, function(self) return self.Owner == soviets and self.HasProperty("StartBuildingRepairs") end)
	Utils.Do(buildings, function(actor)
		Trigger.OnDamaged(actor, function(building)
			if building.Owner == soviets and building.Health < (building.MaxHealth * RepairTriggerThreshold[Map.LobbyOption("difficulty")] / 100) then
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
		allies1.MarkFailedObjective(objTanyaMustSurvive)
	end)

	Trigger.OnAllKilledOrCaptured(Sams, function()
		allies1.MarkCompletedObjective(objDestroySamSites)
		objExtractEinstein = allies1.AddPrimaryObjective("Wait for a helicopter at the LZ and extract Einstein.")
		Actor.Create("flare", true, { Owner = allies1, Location = ExtractionLZ.Location + CVec.New(1, -1) })
		Beacon.New(allies1, ExtractionLZ.CenterPosition)
		Media.PlaySpeechNotification(allies1, "SignalFlareNorth")

		ExtractionHeli = Reinforcements.ReinforceWithTransport(allies1, "tran", nil, { ExtractionLZEntryPoint.Location, ExtractionLZ.Location })[1]
		Trigger.OnKilled(ExtractionHeli, function()
			allies1.MarkFailedObjective(objExtractEinstein)
		end)
		Trigger.OnPassengerEntered(ExtractionHeli, function(heli, passenger)
			if passenger == Einstein then
				heli.Move(ExtractionLZEntryPoint.Location)
				heli.Destroy()
				Trigger.OnRemovedFromWorld(heli, function()
					allies2.MarkCompletedObjective(objLimitLosses)
					allies2.MarkCompletedObjective(objHoldPosition)
					allies1.MarkCompletedObjective(objTanyaMustSurvive)
					allies1.MarkCompletedObjective(objEinsteinSurvival)
					allies1.MarkCompletedObjective(objExtractEinstein)
				end)
			end
		end)
	end)

	Trigger.OnEnteredProximityTrigger(TownPoint.CenterPosition, WDist.FromCells(15), function(actor, trigger)
		if actor.Owner == allies1 then
			ReassignActors(TownUnits, neutral, allies1)
			Utils.Do(TownUnits, function(a) a.Stance = "Defend" end)
			allies1.MarkCompletedObjective(objFindEinstein)
			objEinsteinSurvival = allies1.AddPrimaryObjective("Keep Einstein alive at all costs.")
			Trigger.OnKilled(Einstein, function()
				allies1.MarkFailedObjective(objEinsteinSurvival)
			end)
			Trigger.RemoveProximityTrigger(trigger)
			SovietTownAttack()
		end
	end)

	Trigger.OnEnteredProximityTrigger(YakAttackPoint.CenterPosition, WDist.FromCells(5), function(actor, trigger)
		if not (Yak == nil or Yak.IsDead) or actor.Owner ~= allies1 then
			return
		end

		Yak = Reinforcements.Reinforce(soviets, { "yak" }, { YakEntryPoint.Location, YakAttackPoint.Location + CVec.New(0, -10) }, 0, YakAttack)[1]
	end)

	Trigger.AfterDelay(ParabombDelay, SendParabombs)
	Trigger.AfterDelay(ParatroopersDelay, SendParatroopers)
	Trigger.AfterDelay(ReinforcementsDelay, SpawnAlliedReinforcements)
end

SpawnTanya = function()
	Tanya = Actor.Create(TanyaType, true, { Owner = allies1, Location = TanyaLocation.Location })
	Tanya.Stance = TanyaStance

	if Map.LobbyOption("difficulty") ~= "easy" and allies1.IsLocalPlayer then
		Trigger.AfterDelay(DateTime.Seconds(2), function()
			Media.DisplayMessage("According to the rules of engagement I need your explicit orders to fire, Commander!", "Tanya")
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
	neutral = Player.GetPlayer("Neutral")

	-- Allies is the pre-set owner of units that get assigned to either the second player, if any, or the first player otherwise.
	allies = Player.GetPlayer("Allies")

	-- Allies1 is the player starting on the right, controlling Tanya
	allies1 = Player.GetPlayer("Allies1")

	-- Allies2 is the player starting on the left, defending the base
	allies2 = Player.GetPlayer("Allies2")

	soviets = Player.GetPlayer("Soviets")

	Utils.Do({ allies1, allies2 }, function(player)
		if player and player.IsLocalPlayer then
			Trigger.OnObjectiveAdded(player, function(p, id)
				Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
			end)

			Trigger.OnObjectiveCompleted(player, function(p, id)
				Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
			end)

			Trigger.OnObjectiveFailed(player, function(p, id)
				Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
			end)

			Trigger.OnPlayerWon(player, function()
				Media.PlaySpeechNotification(player, "MissionAccomplished")
			end)

			Trigger.OnPlayerLost(player, function()
				Media.PlaySpeechNotification(player, "MissionFailed")
			end)
		end
	end)

	if not allies2 or allies2.IsLocalPlayer then
		Camera.Position = Allies2BasePoint.CenterPosition
	else
		Camera.Position = ChinookHusk.CenterPosition
	end

	if not allies2 then
		allies2 = allies1
	end

	ReassignActors(Map.ActorsInWorld, allies, allies2)
	SpawnTanya()

	objTanyaMustSurvive = allies1.AddPrimaryObjective("Tanya must survive.")
	objFindEinstein = allies1.AddPrimaryObjective("Find Einstein's crashed helicopter.")
	objDestroySamSites = allies1.AddPrimaryObjective("Destroy the SAM sites.")

	objHoldPosition = allies2.AddPrimaryObjective("Hold your position and protect the base.")
	objLimitLosses = allies2.AddSecondaryObjective("Do not lose more than " .. DeathThreshold[Map.LobbyOption("difficulty")] .. " units.")
	objCutSovietPower = allies2.AddSecondaryObjective("Take out the Soviet power grid.")

	SetupTriggers()
	SetupSoviets()
end