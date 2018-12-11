--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

if Map.LobbyOption("difficulty") == "easy" then
	Rambo = "rmbo.easy"
elseif Map.LobbyOption("difficulty") == "hard" then
	Rambo = "rmbo.hard"
else
	Rambo = "rmbo"
end

GDIBuildings = {ConYard, PowerPlant1, PowerPlant2, PowerPlant3, PowerPlant4, PowerPlant5, Barracks,
Silo1, Silo2, WeaponsFactory, CommCenter, GuardTower1, GuardTower2}


function RepairBuilding(building, attacker)
	if not building.IsDead and building.Owner == enemy then
		building.StartBuildingRepairs(enemy)
	end
end


Mammoths = {Mammoth1, Mammoth2, Mammoth3}
Grenadiers = {Grenadier1, Grenadier2, Grenadier3, Grenadier4}
MediumTanks = {MediumTank1, MediumTank2}
Riflemen = {Rifleman1, Rifleman2, Rifleman3, Rifleman4}

MammothPatrolPath = {MammothWaypoint1.Location, MammothWaypoint2.Location}
RiflemenPatrolPath = {RiflemenWaypoint1.Location, RiflemenWaypoint2.Location}

DamageTrigger = false


function TankDamaged(tank, attacker)
	if not DamageTrigger then
		DamageTrigger = true
		Utils.Do(Grenadiers, function(grenadier)
			if not grenadier.IsDead then
				grenadier.AttackMove(tank.Location)
			end
		end)
	end
end


function GrenadierDamaged(grenadier, attacker)
	if not DamageTrigger then
		DamageTrigger = true
		Utils.Do(MediumTanks, function(tank)
			if not tank.IsDead then
				tank.AttackMove(grenadier.Location)
			end
		end)
	end
end


InfantrySquad = {"e1", "e1", "e1", "e1", "e1"}


function MoveToNorthEntrance(squad)
	Utils.Do(squad, function(unit)
		if not unit.IsDead then
			unit.AttackMove(NorthEntrance.Location)
		end
    end)
end


function EnteredFromNorth(actor, id)
	if actor.Owner == player then
		Trigger.RemoveFootprintTrigger(id)
		if not Barracks.IsDead and Barracks.Owner == enemy then
			Barracks.Build(InfantrySquad, MoveToNorthEntrance)
		end
	end
end


function DeliverCommando()
	Media.PlaySpeechNotification(player, "Reinforce")
	units = Reinforcements.ReinforceWithTransport(player, 'tran.in', {Rambo}, {ChinookEntry.Location, ChinookTarget.Location}, {ChinookEntry.Location})
	rambo = units[2][1]
	Trigger.OnKilled(rambo, function(a, k)
		player.MarkFailedObjective(keepRamboAliveObjective)
	end)
	Trigger.OnPlayerWon(player, function(player)
        if not rambo.IsDead then
            player.MarkCompletedObjective(keepRamboAliveObjective)
        end
	end)
end


function WorldLoaded()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")

	enemy.Cash = 10000

	Camera.Position = DefaultCameraPosition.CenterPosition
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
		Media.PlaySpeechNotification(player, "Win")
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	gdiObjective = enemy.AddPrimaryObjective("Eliminate all Nod forces in the area.")
	warFactoryObjective = player.AddPrimaryObjective("Destroy or capture the Weapons Factory.")
	destroyTanksObjective = player.AddPrimaryObjective("Destroy the Mammoth tanks in the R&D base.")
	keepRamboAliveObjective = player.AddSecondaryObjective("Keep your Commando alive.")

	Trigger.OnKilledOrCaptured(WeaponsFactory, function()
		player.MarkCompletedObjective(warFactoryObjective)
	end)
	Trigger.OnAllKilled(Mammoths, function()
		player.MarkCompletedObjective(destroyTanksObjective)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), DeliverCommando)

	Utils.Do(Mammoths, function(mammoth)
		mammoth.Stance = "HoldFire"
    end)

	Utils.Do(MediumTanks, function(tank)
		Trigger.OnDamaged(tank, TankDamaged)
    end)

	Utils.Do(Grenadiers, function(grenadier)
		Trigger.OnDamaged(grenadier, GrenadierDamaged)
    end)

	Utils.Do(GDIBuildings, function(building)
		Trigger.OnDamaged(building, RepairBuilding)
    end)

	Trigger.OnEnteredFootprint({NorthEntrance.Location}, EnteredFromNorth)

	Utils.Do(Riflemen, function(rifleman)
		rifleman.Patrol(RiflemenPatrolPath)
    end)

	PatrollingMammoth.Patrol(MammothPatrolPath)
end


function Tick()
	if DateTime.GameTime > 2 then
		if player.HasNoRequiredUnits() then
			enemy.MarkCompletedObjective(gdiObjective)
		end
	end
end
