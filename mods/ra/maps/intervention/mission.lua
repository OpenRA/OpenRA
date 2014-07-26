difficulty = OpenRA.GetDifficulty()

if difficulty == "Medium" then
	BaseRaidInterval	= Utils.Minutes(3)
	BaseFrontAttackInterval	= Utils.Minutes(3) + Utils.Seconds(30)
	BaseRearAttackInterval	= Utils.Minutes(8)
	UBoatPatrolDelay	= Utils.Minutes(2) + Utils.Seconds(30)
	BaseFrontAttackWpts	= { PatrolWpt1, BaseRaidWpt1 }
else
	BaseRaidInterval	= Utils.Minutes(2) + Utils.Seconds(30)
	BaseFrontAttackInterval	= Utils.Minutes(2)
	BaseRearAttackInterval	= Utils.Minutes(5)
	UBoatPatrolDelay	= Utils.Minutes(2)
	BaseFrontAttackWpts	= { PatrolWpt1 }
end

Village			= { FarmHouse1, FarmHouse2, FarmHouse3, FarmHouse4, FarmHouse5, FarmHouse6, FarmHouse7, FarmHouse8, FarmHouse9, Church }
VillageRaidInterval 	= Utils.Minutes(3)
VillageRaidAircraft	= { "mig", "mig" }
VillageRaidWpts		= { VillageRaidWpt1, VillageRaidWpt2 }

BaseRaidAircraft	= { "mig", "mig" }
BaseRaidWpts		= { UboatPatrolWpt1, BaseRaidWpt2 }

BaseFrontAttackUnits	= {
				{ Barracks, {"e3", "e3", "e1", "e1", "e1"} },
				{ WarFactory, {"3tnk", "3tnk", "apc"} }
			  }

BaseRearAttackUnits	= {
				{ Barracks, {"e3", "e3", "e1", "e1"} },
				{ WarFactory, {"3tnk", "3tnk", "v2rl"} }
			  }
BaseRearAttackWpts	= { GroundAttackWpt1, BaseRearAttackWpt1, BaseRearAttackWpt2, BaseRearAttackWpt3 }

SovietHarvesters	= { Harvester1, Harvester2, Harvester3 }
HarvesterGuard		= { HarvGuard1, HarvGuard2, HarvGuard3 }

UBoats			= { Uboat1, Uboat2, Uboat3, Uboat4, Uboat5, Uboat6 }
UboatPatrolWpts1	= { UboatPatrolWpt1, UboatPatrolWpt2, UboatPatrolWpt3, UboatPatrolWpt4 }
UboatPatrolWpts2	= { UboatPatrolWpt4, UboatPatrolWpt2, UboatPatrolWpt1 }
UBoatPatrolUnits	= { { SubPen, {"ss"} } }

HunterSubs		= { { SubPen, {"ss", "ss"} } }

GroundPatrolWpts	= { PatrolWpt1, PatrolWpt2 }
GroundPatrolUnits	= {
				{ { Barracks, {"e1", "e1", "e1", "e3", "e3"} }, { Kennel, {"dog"} } },
				{ { WarFactory, {"apc", "apc", "ftrk"} } },
				{ { WarFactory, {"3tnk", "3tnk"} } }
			  }

Reinforcements.ReinforceAir = function(owner, planeNames, entrypoint, rallypoint, interval, onCreateFunc)
	local facing = { Map.GetFacing(CPos.op_Subtraction(rallypoint.Location, entrypoint.Location), 0), "Int32" }
	local flight = { }

	for i, planeName in ipairs(planeNames) do
		local enterPosition = WPos.op_Addition(entrypoint.CenterPosition, WVec.New(0, 0, Rules.InitialAltitude(planeName)))
		local plane = Actor.Create(planeName, { AddToWorld = false, Location = entrypoint.Location, CenterPosition = enterPosition, Owner = owner, Facing = facing })
		flight[i] = plane
		OpenRA.RunAfterDelay((i - 1) * interval, function()
			World:Add(plane)
			Actor.Fly(plane, rallypoint.CenterPosition)
			if onCreateFunc ~= nil then
				onCreateFunc(plane)
			end
		end)
	end
	return flight
end

FollowWaypoints = function(team, waypoints)
	Utils.Do(waypoints, function(wpt)
		Team.Do(team, function(a) Actor.Fly(a, wpt.CenterPosition) end)
	end)
end

PlaneExitMap = function(actor, exitPoint)
	Actor.Fly(actor, exitPoint.CenterPosition)
	Actor.FlyOffMap(actor)
	Actor.RemoveSelf(actor)
end

BaseRaid = function()
	local base = Map.FindStructuresInBox(player, AlliedAreaTopLeft, AlliedAreaBottomRight)
	if #base == 0 then
		return
	end

	local target = base[OpenRA.GetRandomInteger(1, #base + 1)]

	local flight = Team.New(Reinforcements.ReinforceAir(soviets, BaseRaidAircraft, BaseRaidEntrypoint, BaseRaidWpts[1], Utils.Seconds(1)))
	FollowWaypoints(flight, BaseRaidWpts)

	Team.Do(flight, function(plane)
		Actor.FlyAttackActor(plane, target)
		PlaneExitMap(plane, VillageRaidEntrypoint)
	end)

	OpenRA.RunAfterDelay(BaseRaidInterval, BaseRaid)
end

VillageRaid = function()
	local target = nil
	Utils.Do(Village, function(tgt)
		if target == nil and not Actor.IsDead(tgt) then
			target = tgt
			return
		end
	end)

	if target == nil then
		return
	end

	local flight = Team.New(Reinforcements.ReinforceAir(soviets, VillageRaidAircraft, VillageRaidEntrypoint, VillageRaidWpts[1], Utils.Seconds(1)))
	FollowWaypoints(flight, VillageRaidWpts)

	Team.Do(flight, function(plane)
		Actor.FlyAttackActor(plane, target)
		PlaneExitMap(plane, BaseRaidEntrypoint)
	end)

	OpenRA.RunAfterDelay(VillageRaidInterval, VillageRaid)
end

SendUboatPatrol = function(team)
	OpenRA.RunAfterDelay(UBoatPatrolDelay, function()
		if difficulty == "Medium" then
			Team.Patrol(team, UboatPatrolWpts1, 0, false)
		else
			Team.Do(team, Actor.Hunt)
		end
		OpenRA.RunAfterDelay(Utils.Minutes(2), function()
			Team.Do(team, Actor.Stop)
			Team.Patrol(team, UboatPatrolWpts2)
		end)
	end)
end

SendGroundPatrol = function(team)
	Team.Patrol(team, GroundPatrolWpts, Utils.Seconds(3))
	Team.Do(team, function(actor) Actor.OnIdle(actor, Actor.Hunt) end)

	Team.AddEventHandler(team.OnAllKilled, function()
		Production.BuildTeamFromTemplate(soviets, GroundPatrolUnits[OpenRA.GetRandomInteger(1, #GroundPatrolUnits + 1)], SendGroundPatrol)
	end)
end

BaseFrontAttack = function(team)
	Team.Patrol(team, BaseFrontAttackWpts, 0, false)
	Team.Do(team, function(actor) Actor.OnIdle(actor, Actor.Hunt) end)
	OpenRA.RunAfterDelay(BaseFrontAttackInterval, function() Production.BuildTeamFromTemplate(soviets, BaseFrontAttackUnits, BaseFrontAttack) end)
end

BaseRearAttack = function(team)
	Team.Patrol(team, BaseRearAttackWpts, 0, false)
	Team.Do(team, function(actor) Actor.OnIdle(actor, Actor.Hunt) end)
	OpenRA.RunAfterDelay(BaseRearAttackInterval, function() Production.BuildTeamFromTemplate(soviets, BaseRearAttackUnits, BaseRearAttack) end)
end

InsertMCV = function ()
	local mcv = Actor.Create("mcv", { Owner = player, Location = MCVInsertLocation.Location, Facing = Facing.North })
	Actor.Move(mcv, MCVDeployLocation.Location)
	Actor.DeployTransform(mcv)
end

SetupWorld = function()
	if difficulty ~= "Medium" then
		Actor.RemoveSelf(EasyMine)
	end

	Utils.Do(SovietHarvesters, Actor.Harvest)

	harvesterGuard = Team.New(HarvesterGuard)
	Utils.Do(SovietHarvesters, function(harvester)
		Actor.OnDamaged(harvester, function(h)
			Team.Do(harvesterGuard, function(g)
				Actor.Stop(g)
				Actor.AttackMove(g, h.Location, 3)
			end)
		end)
	end)

	Utils.Do(UBoats, function(a) Actor.SetStance(a, "Defend") end)

	Utils.Do(Actor.ActorsWithTrait("RepairableBuilding"), function(building)
		if Actor.Owner(building) == soviets then
			Actor.OnDamaged(building, function(b)
				if Actor.Owner(b) == soviets then
					Actor.RepairBuilding(b)
				end
			end)
		end
	end)

	Production.SetRallyPoint(WarFactory, Rallypoint)
	Production.EventHandlers.Setup(soviets)

	-- RunAfterDelay is used so that the 'Building captured' and 'Mission accomplished' sounds don't play at the same time
	Actor.OnCaptured(AirForceHQ, function() OpenRA.RunAfterDelay(Utils.Seconds(3), MissionAccomplished) end)
	Actor.OnKilled(AirForceHQ, MissionFailed)

	village = Team.New(Village)
	Team.AddEventHandler(village.OnAllKilled, MissionFailed)
end

tick = 0
alliedBaseEstablished = false
Tick = function()
	tick = tick + 1

	if OpenRA.GetOre(soviets) > (OpenRA.GetOreCapacity(soviets) * 0.75) then
		Mission.TickTakeOre(soviets)
	end

	if Mission.RequiredUnitsAreDestroyed(player) then
		OpenRA.RunAfterDelay(Utils.Seconds(1), MissionFailed)
	end

	if not alliedBaseEstablished and tick > Utils.Minutes(5) and tick % Utils.Seconds(10) == 0 then
		-- FIXME: replace with cell trigger when available
		local base = Map.FindStructuresInBox(player, AlliedAreaTopLeft, AlliedAreaBottomRight)
		if #base > 0 then
			alliedBaseEstablished = true

			OpenRA.RunAfterDelay(BaseFrontAttackInterval, function()
				Production.BuildTeamFromTemplate(soviets, BaseFrontAttackUnits, BaseFrontAttack)

				local plane, paratroopers = SupportPowers.Paradrop(soviets, "badr", {"e1", "e1", "e1", "e3", "e3"}, BaseRaidEntrypoint.Location, MCVDeployLocation.Location)
				Utils.Do(paratroopers, function(actor) Actor.OnIdle(actor, Actor.Hunt) end)
			end)

			OpenRA.RunAfterDelay(BaseRearAttackInterval, function()
				Production.BuildTeamFromTemplate(soviets, BaseRearAttackUnits, BaseRearAttack)
			end)

			Production.BuildTeamFromTemplate(soviets, HunterSubs, function(team)
				Team.Do(team, function(actor) Actor.OnIdle(actor, Actor.Hunt) end)
			end)

			OpenRA.RunAfterDelay(BaseRaidInterval, BaseRaid)
		end
	end
end

WorldLoaded = function()
	player	= OpenRA.GetPlayer("Allies")
	soviets	= OpenRA.GetPlayer("Soviets")
	civvies	= OpenRA.GetPlayer("Civilians")

	SetupWorld()

	OpenRA.RunAfterDelay(1, function()
		Production.BuildTeamFromTemplate(soviets, UBoatPatrolUnits, SendUboatPatrol)
		Production.BuildTeamFromTemplate(soviets, GroundPatrolUnits[OpenRA.GetRandomInteger(1, #GroundPatrolUnits + 1)], SendGroundPatrol)
	end)
	OpenRA.RunAfterDelay(VillageRaidInterval, VillageRaid)

	InsertMCV()

	OpenRA.SetViewportCenterPosition(Camera.CenterPosition)
	OpenRA.RunAfterDelay(Utils.Seconds(5), function() Actor.RemoveSelf(Camera) end)
end

MissionFailed = function()
	Mission.MissionOver(nil, { player }, false)
end

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil, false)
end
