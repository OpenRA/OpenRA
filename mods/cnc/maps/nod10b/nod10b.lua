--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

if Difficulty == "easy" then
	Rambo = "rmbo.easy"
elseif Difficulty == "hard" then
	Rambo = "rmbo.hard"
else
	Rambo = "rmbo"
end

GDIBuildings = { ConYard, PowerPlant1, PowerPlant2, PowerPlant3, PowerPlant4, PowerPlant5, Barracks, Silo1, Silo2, WeaponsFactory, CommCenter, GuardTower1, GuardTower2 }

Mammoths = { Mammoth1, Mammoth2, Mammoth3 }
Grenadiers = { Grenadier1, Grenadier2, Grenadier3, Grenadier4 }
MediumTanks = { MediumTank1, MediumTank2 }
Riflemen = { Rifleman1, Rifleman2, Rifleman3, Rifleman4 }

MammothPatrolPath = { MammothWaypoint1.Location, MammothWaypoint2.Location }
RiflemenPatrolPath = { RiflemenWaypoint1.Location, RiflemenWaypoint2.Location }

InfantrySquad = { "e1", "e1", "e1", "e1", "e1" }

DeliverCommando = function()
	Media.PlaySpeechNotification(Nod, "Reinforce")
	local rambo = Reinforcements.ReinforceWithTransport(Nod, "tran.in", { Rambo }, { ChinookEntry.Location, ChinookTarget.Location }, { ChinookEntry.Location })[2][1]

	Trigger.OnKilled(rambo, function()
		Nod.MarkFailedObjective(KeepRamboAliveObjective)
	end)

	Trigger.OnPlayerWon(Nod, function(Nod)
        if not rambo.IsDead then
            Nod.MarkCompletedObjective(KeepRamboAliveObjective)
        end
	end)
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")

	GDI.Cash = 10000

	Camera.Position = DefaultCameraPosition.CenterPosition

	InitObjectives(Nod)

	GDIObjective = GDI.AddPrimaryObjective("Eliminate all Nod forces in the area.")
	WarFactoryObjective = Nod.AddPrimaryObjective("Destroy or capture the Weapons Factory.")
	DestroyTanksObjective = Nod.AddPrimaryObjective("Destroy the Mammoth tanks in the R&D base.")
	KeepRamboAliveObjective = Nod.AddObjective("Keep your Commando alive.", "Secondary", false)

	Trigger.OnKilledOrCaptured(WeaponsFactory, function()
		Nod.MarkCompletedObjective(WarFactoryObjective)
	end)

	Trigger.OnAllKilled(Mammoths, function()
		Nod.MarkCompletedObjective(DestroyTanksObjective)
	end)

	Trigger.AfterDelay(DateTime.Seconds(1), DeliverCommando)

	Utils.Do(Mammoths, function(mammoth)
		mammoth.Stance = "HoldFire"
    end)

	Utils.Do(MediumTanks, function(tank)
		Trigger.OnDamaged(tank, function()
			if DamageTrigger then
				return
			end

			DamageTrigger = true
			Utils.Do(Grenadiers, function(grenadier)
				if not grenadier.IsDead then
					grenadier.AttackMove(tank.Location)
				end
			end)
		end)
    end)

	Utils.Do(Grenadiers, function(grenadier)
		Trigger.OnDamaged(grenadier, function()
			if DamageTrigger then
				return
			end

			DamageTrigger = true
			Utils.Do(MediumTanks, function(tank)
				if not tank.IsDead then
					tank.AttackMove(grenadier.Location)
				end
			end)
		end)
    end)

	Utils.Do(GDIBuildings, function(building)
		RepairBuilding(GDI, building, 0.75)
    end)

	Trigger.OnEnteredFootprint({ NorthEntrance.Location }, function(a, id)
		if a.Owner == Nod then
			Trigger.RemoveFootprintTrigger(id)

			if Barracks.IsDead or Barracks.Owner ~= GDI then
				return
			end

			Barracks.Build(InfantrySquad, function(squad)
				Utils.Do(squad, function(unit)
					if not unit.IsDead then
						unit.AttackMove(NorthEntrance.Location)
					end
				end)
			end)
		end
	end)

	Utils.Do(Riflemen, function(rifleman)
		rifleman.Patrol(RiflemenPatrolPath)
    end)

	PatrollingMammoth.Patrol(MammothPatrolPath)
end

Tick = function()
	if DateTime.GameTime > 2 and Nod.HasNoRequiredUnits() then
		GDI.MarkCompletedObjective(GDIObjective)
	end
end
