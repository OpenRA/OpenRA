BeachheadTrigger =
{
	CPos.New(120, 90), CPos.New(120, 89), CPos.New(120, 88), CPos.New(121, 88), CPos.New(122, 88), CPos.New(123, 88), CPos.New(124, 88),
	CPos.New(125, 88), CPos.New(126, 88), CPos.New(126, 89), CPos.New(127, 89), CPos.New(128, 89), CPos.New(128, 90), CPos.New(129, 90),
	CPos.New(130, 90), CPos.New(130, 91), CPos.New(131, 91), CPos.New(132, 91), CPos.New(133, 91), CPos.New(134, 91), CPos.New(134, 92),
	CPos.New(135, 92), CPos.New(136, 92), CPos.New(137, 92), CPos.New(137, 93), CPos.New(138, 93), CPos.New(139, 93), CPos.New(140, 93),
	CPos.New(140, 94), CPos.New(140, 95), CPos.New(140, 96), CPos.New(140, 97), CPos.New(140, 98), CPos.New(140, 99), CPos.New(140, 100),
	CPos.New(139, 100), CPos.New(139, 101), CPos.New(139, 102), CPos.New(138, 102), CPos.New(138, 103), CPos.New(138, 104),
	CPos.New(137, 104), CPos.New(137, 105), CPos.New(137, 106), CPos.New(136, 106), CPos.New(136, 107)
}

Difficulty = Map.LobbyOption("difficulty")

if Difficulty == "normal" then
	BaseRaidInterval = DateTime.Minutes(3)
	BaseFrontAttackInterval = DateTime.Minutes(3) + DateTime.Seconds(30)
	BaseRearAttackInterval = DateTime.Minutes(8)
	UBoatPatrolDelay = DateTime.Minutes(2) + DateTime.Seconds(30)
	BaseFrontAttackWpts = { PatrolWpt1.Location, BaseRaidWpt1.Location }
else
	BaseRaidInterval = DateTime.Minutes(2)
	BaseFrontAttackInterval = DateTime.Minutes(2) + DateTime.Seconds(30)
	BaseRearAttackInterval = DateTime.Minutes(5)
	UBoatPatrolDelay = DateTime.Minutes(2)
	BaseFrontAttackWpts = { PatrolWpt1.Location }
end

Village = { FarmHouse1, FarmHouse2, FarmHouse3, FarmHouse4, FarmHouse5, FarmHouse6, FarmHouse7, FarmHouse8, FarmHouse9, Church }
VillageRaidInterval = DateTime.Minutes(3)
VillageRaidAircraft = { "mig", "mig" }
VillageRaidWpts = { VillageRaidEntrypoint.Location, VillageRaidWpt1.Location, VillageRaidWpt2.Location }

BaseRaidAircraft = { "mig", "mig" }
BaseRaidWpts = { BaseRaidEntrypoint.Location, UboatPatrolWpt1.Location, BaseRaidWpt2.Location }

BaseFrontAttackUnits = { "e3", "e3", "e1", "e1", "e1", "3tnk", "3tnk", "apc" }
BaseRearAttackUnits = { "e3", "e3", "e1", "e1", "3tnk", "3tnk", "v2rl" }
BaseRearAttackWpts = { GroundAttackWpt1.Location, BaseRearAttackWpt1.Location, BaseRearAttackWpt2.Location, BaseRearAttackWpt3.Location }

SovietHarvesters = { Harvester1, Harvester2, Harvester3 }
HarvesterGuard = { HarvGuard1, HarvGuard2, HarvGuard3 }

UboatPatrolWpts1 = { UboatPatrolWpt1.Location, UboatPatrolWpt2.Location, UboatPatrolWpt3.Location, UboatPatrolWpt4.Location }
UboatPatrolWpts2 = { UboatPatrolWpt4.Location, UboatPatrolWpt2.Location, UboatPatrolWpt1.Location }
UBoatPatrolUnits = { "ss" }

HunterSubs = { "ss", "ss" }

GroundPatrolWpts = { PatrolWpt1.Location, PatrolWpt2.Location }
GroundPatrolUnits =
{
	{ "e1", "e1", "e1", "e3", "e3", "dog" },
	{ "apc", "apc", "ftrk" },
	{ "3tnk", "3tnk" }
}

ParadropSovietUnits = function()
	local powerproxy = Actor.Create("powerproxy.paratroopers", false, { Owner = soviets })
	local units = powerproxy.SendParatroopers(MCVDeployLocation.CenterPosition, false, 256 - 53)

	Utils.Do(units, function(a)
		Trigger.OnIdle(a, function(actor)
			if actor.IsInWorld then
				actor.Hunt()
			end
		end)
	end)

	powerproxy.Destroy()
end

AirRaid = function(planeTypes, ingress, egress, target)
	if target == nil then
		return
	end

	for i = 1, #planeTypes do
		Trigger.AfterDelay((i - 1) * DateTime.Seconds(1), function()
			local start = Map.CenterOfCell(ingress[1]) + WVec.New(0, 0, Actor.CruiseAltitude(planeTypes[i]))
			local plane = Actor.Create(planeTypes[i], true, { CenterPosition = start, Owner = soviets, Facing = (Map.CenterOfCell(ingress[2]) - start).Facing })

			Utils.Do(ingress, function(wpt) plane.Move(wpt) end)
			plane.Attack(target)
			Utils.Do(egress, function(wpt) plane.Move(wpt) end)
			plane.Destroy()
		end)
	end
end

BaseRaid = function()
	local targets = Map.ActorsInBox(AlliedAreaTopLeft.CenterPosition, AlliedAreaBottomRight.CenterPosition, function(actor)
		return actor.Owner == player and actor.HasProperty("StartBuildingRepairs")
	end)

	if #targets == 0 then
		return
	end

	local target = Utils.Random(targets)

	AirRaid(BaseRaidAircraft, BaseRaidWpts, { VillageRaidEntrypoint.Location }, target)

	Trigger.AfterDelay(BaseRaidInterval, BaseRaid)
end

VillageRaid = function()
	local target = nil
	Utils.Do(Village, function(tgt)
		if target == nil and not tgt.IsDead then
			target = tgt
			return
		end
	end)

	if target == nil then
		return
	end

	AirRaid(VillageRaidAircraft, VillageRaidWpts, { BaseRaidEntrypoint.Location }, target)

	Trigger.AfterDelay(VillageRaidInterval, VillageRaid)
end

SendUboatPatrol = function(team)
	Trigger.AfterDelay(UBoatPatrolDelay, function()
		Utils.Do(team, function(uboat)
			if not uboat.IsDead then
				uboat.PatrolUntil(UboatPatrolWpts1, function()
					return DateTime.GameTime > DateTime.Minutes(2) + UBoatPatrolDelay
				end)
				uboat.Patrol(UboatPatrolWpts2)
			end
		end)
	end)
end

SendGroundPatrol = function(team)
	Utils.Do(team, function(unit) unit.Patrol(GroundPatrolWpts, true, DateTime.Seconds(3)) end)
	Utils.Do(team, function(unit)
		Trigger.OnIdle(unit, function(actor) actor.Hunt() end)
	end)
	Trigger.OnAllKilled(team, function()
		Build(Utils.Random(GroundPatrolUnits), SendGroundPatrol)
	end)
end

BaseFrontAttack = function(team)
	Utils.Do(team, function(unit) unit.Patrol(BaseFrontAttackWpts, false) end)
	Utils.Do(team, function(unit)
		Trigger.OnIdle(unit, function(actor) actor.Hunt() end)
	end)
	Trigger.AfterDelay(BaseFrontAttackInterval, function() Build(BaseFrontAttackUnits, BaseFrontAttack) end)
end

BaseRearAttack = function(team)
	Utils.Do(team, function(unit) unit.Patrol(BaseRearAttackWpts, false) end)
	Utils.Do(team, function(unit)
		Trigger.OnIdle(unit, function(actor) actor.Hunt() end)
	end)
	Trigger.AfterDelay(BaseRearAttackInterval, function() Build(BaseRearAttackUnits, BaseRearAttack) end)
end

Build = function(units, action)
	if not soviets.Build(units, action) then
		Trigger.AfterDelay(DateTime.Seconds(15), function()
			Build(units, action)
		end)
	end
end

SetupWorld = function()
	Utils.Do(SovietHarvesters, function(a) a.FindResources() end)

	Utils.Do(SovietHarvesters, function(harvester)
		Trigger.OnDamaged(harvester, function(h)
			Utils.Do(HarvesterGuard, function(g)
				if not g.IsDead then
					g.Stop()
					g.AttackMove(h.Location, 3)
				end
			end)
		end)
	end)

	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == soviets and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == soviets then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)

	WarFactory.RallyPoint = Rallypoint.Location
	WarFactory.IsPrimaryBuilding = true
	Barracks.IsPrimaryBuilding = true
	SubPen.IsPrimaryBuilding = true
end

SetupMissionText = function()
	TextColorNormal = HSLColor.White
	TextColorDamaged = HSLColor.Yellow
	TextColorCritical = HSLColor.Red

	CurrentColor = TextColorNormal
	local villageHousesLeft = #Village
	VillagePercentage = 100 - villageHousesLeft * 10

	Utils.Do(Village, function(house)
		Trigger.OnKilled(house, function()
			villageHousesLeft = villageHousesLeft - 1
			VillagePercentage = 100 - villageHousesLeft * 10

			if VillagePercentage > 69 then
				CurrentColor = TextColorCritical
			elseif VillagePercentage > 49 then
				CurrentColor = TextColorDamaged
			else
				CurrentColor = TextColorNormal
			end
		end)
	end)
end

Tick = function()
	if DateTime.GameTime > 2 then
		if soviets.Resources > soviets.ResourceCapacity * 0.75 then
			soviets.Resources = soviets.Resources - ((soviets.ResourceCapacity * 0.01) / 25)
		end

		if player.HasNoRequiredUnits() then
			player.MarkFailedObjective(villageObjective)
		end

		UserInterface.SetMissionText(VillagePercentage .. "% of the village destroyed.", CurrentColor)
	end
end

WorldLoaded = function()
	player	= Player.GetPlayer("Allies")
	soviets	= Player.GetPlayer("Soviets")

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

	sovietObjective = soviets.AddPrimaryObjective("Destroy the village.")
	villageObjective = player.AddPrimaryObjective("Save the village.")
	beachheadObjective = player.AddPrimaryObjective("Get your MCV to the main island.")

	beachheadTrigger = false
	Trigger.OnExitedFootprint(BeachheadTrigger, function(a, id)
		if not beachheadTrigger and a.Owner == player and a.Type == "mcv" then
			beachheadTrigger = true
			Trigger.RemoveFootprintTrigger(id)
			player.MarkCompletedObjective(beachheadObjective)

			captureObjective = player.AddPrimaryObjective("Locate and capture the enemy's Air Force HQ.")

			if AirForceHQ.IsDead then
				player.MarkFailedObjective(captureObjective)
				return
			end
			if AirForceHQ.Owner == player then
				player.MarkCompletedObjective(captureObjective)
				player.MarkCompletedObjective(villageObjective)
				return
			end

			Trigger.OnCapture(AirForceHQ, function()
				Trigger.AfterDelay(DateTime.Seconds(3), function()
					player.MarkCompletedObjective(captureObjective)
					player.MarkCompletedObjective(villageObjective)
				end)
			end)
			Trigger.OnKilled(AirForceHQ, function() player.MarkFailedObjective(captureObjective) end)

			Actor.Create("mainland", true, { Owner = player })

			Trigger.AfterDelay(BaseFrontAttackInterval, function()
				Build(BaseFrontAttackUnits, BaseFrontAttack)
				ParadropSovietUnits()
			end)
			Trigger.AfterDelay(BaseRearAttackInterval, function()
				Build(BaseRearAttackUnits, BaseRearAttack)
			end)
			Trigger.AfterDelay(BaseRaidInterval, BaseRaid)

			Trigger.AfterDelay(UBoatPatrolDelay, function()
				Build(HunterSubs, function(subs)
					Utils.Do(subs, function(sub)
						Trigger.OnIdle(sub, function(s) s.Hunt() end)
					end)
				end)
			end)
		end
	end)

	Trigger.OnAllKilled(Village, function() player.MarkFailedObjective(villageObjective) end)

	SetupWorld()
	SetupMissionText()

	Trigger.AfterDelay(VillageRaidInterval, VillageRaid)

	Trigger.AfterDelay(1, function() Build(UBoatPatrolUnits, SendUboatPatrol) end)
	Trigger.AfterDelay(1, function() Build(Utils.Random(GroundPatrolUnits), SendGroundPatrol) end)

	Reinforcements.Reinforce(player, { "mcv" }, { MCVInsertLocation.Location, MCVDeployLocation.Location }, 0, function(mcv)
		mcv.Deploy()
	end)

	Camera.Position = CameraSpot.CenterPosition
	Trigger.AfterDelay(DateTime.Seconds(5), function() CameraSpot.Destroy() end)
end
