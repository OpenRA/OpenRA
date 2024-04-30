--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
ReinforceAllies = function()
	Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
	local proxy = SpawnMiscActor("paradrop.allies", GoodGuy, TanyaDrop.Location, DateTime.Seconds(1))
	local planes = proxy.TargetParatroopers(TanyaDrop.CenterPosition, Angle.SouthEast)

	Utils.Do(planes, function(plane)
		Trigger.OnPassengerExited(plane, function(_, passenger)
			passenger.Owner = Greece
		end)

		Trigger.OnAllKilled(plane.Passengers, CheckAlliedDestruction)
	end)

	local demoTypes = { "2tnk", "2tnk", "2tnk", "dtrk" }
	local artilleryTypes = {"2tnk", "2tnk", "arty", "arty", "mech", "mech", "mech" }
	local groundPath = { WestRoadEntry.Location, WestRoadRally.Location }

	local demoTeam = Reinforcements.Reinforce(Greece, demoTypes, groundPath)
	Trigger.OnAllKilled(demoTeam, CheckAlliedDestruction)

	Trigger.AfterDelay(DateTime.Seconds(30), function()
		if AlliesDefeated then
			return
		end

		Media.PlaySpeechNotification(Greece, "ReinforcementsArrived")
		local artilleryTeam = Reinforcements.Reinforce(Greece, artilleryTypes, groundPath)
		Trigger.OnAllKilled(artilleryTeam, CheckAlliedDestruction)
	end)
end

ReinforceChronoTanks = function(locations)
	if ChronoTanksReinforced then
		return
	end

	ChronoTanksReinforced = true
	Media.PlaySoundNotification(Greece, "Chronoshift")

	Utils.Do(locations, function(location)
		local proxy = SpawnMiscActor("powerproxy.chronoshift", GoodGuy, location, DateTime.Seconds(1))
		local tank = Actor.Create("ctnk", true, { Owner = Greece, Facing = Angle.NorthWest })
		local payload = { [tank] = location }
		proxy.Chronoshift(payload)
	end)
end

ReinforceTanya = function()
	local path = { WestRoadEntry.Location, TanyaDrop.Location }
	local plane = Reinforcements.Reinforce(Greece, { "badr.tanya" }, path)[1]
	local passengers = { TanyaType }

	Trigger.OnAddedToWorld(plane, function()
		Utils.Do(passengers, function(type)
			local passenger = Actor.Create(type, false, { Owner = Greece })
			plane.LoadPassenger(passenger)
			Trigger.OnKilled(passenger, OnTanyaKilled)
			Tanya = passenger
			AnnounceTanyaRules(passenger)
		end)

		plane.Paradrop(path[2])
	end)
end

AnnounceTanyaRules = function(tanya)
	if Difficulty == "easy" then
		return
	end

	Trigger.OnAddedToWorld(tanya, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.DisplayMessage(UserInterface.Translate("tanya-rules-of-engagement"), tanya.TooltipName)
			Media.PlaySoundNotification(Greece, "AlertBleep")
		end)
	end)
end

ReinforceLongbows = function()
	if SoutheastTurret.IsDead then
		return
	end

	local goals = { LongbowGoal1, LongbowGoal2, LongbowGoal3 }
	local spawnOffset = 0

	Utils.Do(goals, function(goal)
		local entry = { LongbowEntry.Location + CVec.New(spawnOffset, 15) }

		Reinforcements.Reinforce(GoodGuy, { "heli" }, entry, 0, function(helicopter)
			helicopter.Wait(DateTime.Seconds(1))
			helicopter.AttackMove(goal.Location)

			-- Original behavior was to fire at the ground until out of ammo.
			Trigger.OnKilled(SoutheastTurret, function()
				helicopter.Stop()
				helicopter.Wait(10)
				helicopter.Move(LongbowExit.Location, 1)
				helicopter.Destroy()
			end)
		end)

		spawnOffset = spawnOffset + 5
	end)
end

ReinforceBadGuy = function()
	-- If ForwardCommand dies early, the General will be busy hunting.
	if General.IsDead or ForwardCommand.IsDead then
		return
	end

	BadGuyReinforced = true
	Media.PlaySpeechNotification(Greece, "SignalFlareWest")
	local flare = SpawnFlare(NorthFlare.Location)

	Trigger.OnKilled(General, function()
		RemoveActor(flare, DateTime.Seconds(30))
	end)

	local waterCamera = SpawnPlayerCamera(NorthWaterEntry.Location, -1)
	local boatPath = { NorthWaterEntry.Location, BuilderBeach.Location }
	local cargo = Reinforcements.ReinforceWithTransport(BadGuy, "lst", { "mcv" }, boatPath, { boatPath[1] })[2]

	Utils.Do(cargo, function(builder)
		Trigger.OnKilled(builder, CheckNewBase)

		Trigger.OnAddedToWorld(builder, function()
			local builderCamera = SpawnPlayerCamera(builder.Location, -1, "camera.small")
			builder.Move(BuilderRally.Location)

			builder.CallFunc(function()
				RemoveActor(waterCamera)
				RemoveActor(builderCamera)
			end)

			builder.Deploy()
		end)
	end)
end

ReinforceNorthChinook = function()
	local delay = DateTime.Seconds(4)
	local camera = SpawnPlayerCamera(NorthChinookUnload.Location, -1, "camera.small", delay + DateTime.Seconds(2))

	Trigger.AfterDelay(delay, function()
		local transport = Actor.Create("tran.north", true, { Owner = USSR, Location = NorthChinookEntry.Location, Facing = Angle.South })
		local passengers = transport.Passengers

		transport.UnloadPassengers(NorthChinookUnload.Location)
		transport.Move(SouthChinookExit.Location)
		transport.Destroy()

		Trigger.OnPassengerExited(transport, function()
			if transport.PassengerCount > 0 then
				return
			end

			-- Original unload is slow. Mimic the timing with a short pause.
			Trigger.AfterDelay(DateTime.Seconds(3), function()
				OrderNorthChinookSoldiers(passengers, camera)
			end)
		end)

		Trigger.OnAnyKilled(passengers, function()
			RemoveActor(camera, DateTime.Seconds(1))
		end)
	end)
end

OrderNorthChinookSoldiers = function(soldiers, camera)
	local patrolPath =
	{
		VillageNortheast.Location,
		VillageNorthwest.Location,
		VillageSouthwest.Location,
		VillageSoutheast.Location
	}
	local liveSoldiers = Utils.Where(soldiers, IsAlive)

	Utils.Do(liveSoldiers, function(soldier)
		soldier.AttackMove(NorthChinookRally.Location)

		soldier.CallFunc(function()
			Trigger.AfterDelay(1, function()
				if not AreAllIdleOrDead(liveSoldiers) then
					return
				end
				RemoveActor(camera, DateTime.Seconds(1))
				GroupAttackMove(liveSoldiers, VillageBridgeCenter.Location, 2)
				GroupTightPatrol(liveSoldiers, patrolPath, true)
				GroupHuntOnDamaged(liveSoldiers, Greece)
			end)
		end)
	end)
end

ReinforceVillagePatrol = function()
	if VillagePatrolSent then
		return
	end

	VillagePatrolSent = true

	local path =
	{
		VillageSoutheast.Location,
		VillageSouthwest.Location,
		VillageNorthwest.Location,
		VillageNortheast.Location
	}
	local types = { "e1", "e1", "e2", "dog" }
	local origin = { VillagePatrolEntry.Location }
	local group = Reinforcements.Reinforce(USSR, types, origin, 0)
	GroupTightPatrol(group, path, true)
end

ReinforceGuardHouse = function(intruderType)
	if GuardHouse.IsDead then
		return
	end

	local goal = GuideBarrelGoal.Location
	if intruderType == TanyaType then
		goal = PrisonReveal.Location
	end

	for i = 1, 4 do
		Trigger.AfterDelay(i * 5, function()
			local guard = Actor.Create("e1", true, { Owner = USSR, Location = GuardHouseSpawn.Location, SubCell = 4, Facing = Angle.SouthWest })
			guard.Move(GuardHouseExit.Location)
			guard.AttackMove(goal)
			IdleHunt(guard)
		end)
	end
end

ReinforceHardTeams = function()
	if Difficulty ~= "hard" then
		return
	end

	ReinforceBaseDefenders()
	ReinforceNorthBeachGuards()
	ReinforceV2Team()
	ReinforceNorthEdgeGuards()
	PrepareSouthChinook()
	ReinforceHardDogs()
end

ReinforceBaseDefenders = function()
	local origin = BadGuyRally.Location + CVec.New(2, 2)
	local types = { "3tnk", "3tnk" }
	local defenders = Reinforcements.Reinforce(USSR, types, { origin }, 0, function(actor)
		actor.Wait(1)
		actor.Scatter()
	end)
	GroupHuntOnDamaged(defenders, Greece)

	local structures = { RoadTurretWest, RoadTurretEast, ForwardPower, ForwardCommand, ForwardTech }
	Trigger.OnAnyKilled(structures, function()
		GroupIdleHunt(defenders)
	end)
end

ReinforceNorthBeachGuards = function()
	local types = { "e1", "e1", "e2", "e1" }
	local patrolPath = { VillageNortheast.Location, VillageSouthwest.Location }
	local soldiers = Reinforcements.Reinforce(USSR, types, { NorthFlare.Location }, 0, function(actor)
		actor.Scatter()
	end)

	Trigger.OnEnteredFootprint({ GeneralRally.Location }, function(actor, id)
		if actor.Type ~= "gnrl" then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		GroupHuntOnDamaged(soldiers, Greece)

		Utils.Do(soldiers, function(soldier)
			if soldier.IsDead then
				return
			end

			soldier.AttackMove(BadGuyRally.Location)
			soldier.AttackMove(GuideBarrelGoal.Location)
			soldier.AttackMove(VillageNorthwest.Location)
			soldier.Patrol(patrolPath, true)
		end)
	end)
end

ReinforceV2Team = function()
	local rocket = Actor.Create("v2rl", true, { Owner = USSR, Facing = Angle.West, Location = SamRocketEntry.Location })
	Reinforcements.Reinforce(USSR, { "e1" }, { SamFenceReveal.Location }, 0, function(actor)
		actor.Guard(rocket)
	end)
end

PrepareSouthChinook = function()
	local startPosition = SouthChinookEntry.CenterPosition + WVec.New(0, 0, Actor.CruiseAltitude("tran"))

	Trigger.OnEnteredProximityTrigger(SouthChinookProximity.CenterPosition, WDist.FromCells(8), function(actor, id)
		if actor.Type ~= TanyaType or not EscortFinished then
			return
		end

		Trigger.RemoveProximityTrigger(id)

		local transport = Actor.Create("tran.south", true, { Owner = USSR, CenterPosition = startPosition, Facing = Angle.East })
		transport.UnloadPassengers(SouthChinookEntry.Location)
		transport.Move(SouthChinookExit.Location)
		transport.Destroy()

		Utils.Do(transport.Passengers, function(passenger)
			Trigger.OnAddedToWorld(passenger, function()
				passenger.Wait(DateTime.Seconds(1))
				IdleHunt(passenger)
			end)
		end)
	end)
end

ReinforceNorthEdgeGuards = function()
	local types = { "e1", "e1", "e2" }
	local guards = Reinforcements.Reinforce(USSR, types, { NorthEdgeGuardEntry.Location }, 0, function(actor)
		actor.Scatter()
	end)
	GroupHuntOnDamaged(guards, Greece)
end

ReinforceStartSoldiers = function()
	local teams =
	{
		{
			entry = { WestRoadEntry.Location },
			types = { "e2", "e2", "e1" }
		},
		{
			entry = { RoadTurretRifle2.Location },
			types = { "e1", "e2", "e4" }
		}
	}

	Utils.Do(teams, function(team)
		local soldiers = Reinforcements.Reinforce(USSR, team.types, team.entry, 15, function(actor)
			actor.AttackMove(TanyaDrop.Location)
			actor.Hunt()
		end)

		Trigger.OnAnyKilled(soldiers, function()
			IdleHunt(StartSoldier)
		end)
	end)
end

ReinforceHardDogs = function()
	local startDog = Actor.Create("dog", true, { Owner = USSR, Location = WestRoadEntry.Location, Facing = Angle.South })
	startDog.Move(WestRoadRally.Location + CVec.New(-1, 0))
	PrepareStartDogAttack(startDog)

	local turretDog = Actor.Create("dog.areaguard", true, { Owner = USSR, Location = LongbowGoal2.Location, Facing = Angle.SouthWest })
	turretDog.AddTag("TurretDog")

	Trigger.OnKilled(turretDog, function()
		GroupIdleHunt(SoutheastTurretGuards)
	end)
end

PrepareStartDogAttack = function(dog)
	-- Tanya will take about 100 ticks/4 seconds to touch the ground.
	local delay = DateTime.Seconds(5) + 5

	Trigger.OnEnteredFootprint({ TanyaDrop.Location }, function(actor, id)
		if actor.Type ~= TanyaType then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		Trigger.AfterDelay(delay, function()
			if dog.IsDead or actor.IsDead then
				return
			end

			dog.Attack(actor)
		end)
	end)
end
