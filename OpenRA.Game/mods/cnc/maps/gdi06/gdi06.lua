--[[
   Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

IslandSamSites = { SAM01, SAM02 }
NodBase = { PowerPlant1, PowerPlant2, PowerPlant3, PowerPlant4, PowerPlant5, Refinery, HandOfNod, Silo1, Silo2, Silo3, Silo4, ConYard, CommCenter }

FlameSquad = { FlameGuy1, FlameGuy2, FlameGuy3 }
FlameSquadRoute = { waypoint4.Location, waypoint12.Location, waypoint4.Location, waypoint6.Location }

FootPatrol1Squad = { MiniGunner1, MiniGunner2, RocketSoldier1 }
FootPatrol1Route =
{
	waypoint4.Location,
	waypoint12.Location,
	waypoint13.Location,
	waypoint3.Location,
	waypoint2.Location,
	waypoint7.Location,
	waypoint6.Location
}

FootPatrol2Squad = { MiniGunner3, MiniGunner4 }
FootPatrol2Route =
{
	waypoint14.Location,
	waypoint16.Location
}

FootPatrol3Squad = { MiniGunner5, MiniGunner6 }
FootPatrol3Route =
{
	waypoint15.Location,
	waypoint17.Location
}

FootPatrol4Route =
{
	waypoint4.Location,
	waypoint5.Location
}

FootPatrol5Squad = { RocketSoldier2, RocketSoldier3, RocketSoldier4 }
FootPatrol5Route =
{
	waypoint4.Location,
	waypoint12.Location,
	waypoint13.Location,
	waypoint8.Location,
	waypoint9.Location,
}

Buggy1Route =
{
	waypoint6.Location,
	waypoint7.Location,
	waypoint2.Location,
	waypoint8.Location,
	waypoint9.Location,
	waypoint8.Location,
	waypoint2.Location,
	waypoint7.Location
}

Buggy2Route =
{
	waypoint6.Location,
	waypoint10.Location,
	waypoint11.Location,
	waypoint10.Location
}

HuntTriggerActivator = { SAM03, SAM04, SAM05, SAM06, LightTank1, LightTank2, LightTank3, Buggy1, Buggy2, Turret1, Turret2 }

AttackCellTriggerActivator = { CPos.New(57,26), CPos.New(56,26), CPos.New(57,25), CPos.New(56,25), CPos.New(57,24), CPos.New(56,24), CPos.New(57,23), CPos.New(56,23), CPos.New(57,22), CPos.New(56,22), CPos.New(57,21), CPos.New(56,21) }
AttackUnits = { LightTank2, LightTank3 }

KillCounter = 0

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	InitObjectives(GDI)

	if Difficulty == "easy" then
		CommandoType = "rmbo.easy"
		KillCounterHuntThreshold = 30
	elseif Difficulty == "hard" then
		CommandoType = "rmbo.hard"
		KillCounterHuntThreshold = 15
	else
		CommandoType = "rmbo"
		KillCounterHuntThreshold = 20
	end

	DestroyObjective = GDI.AddObjective("Destroy the Nod ********.")

	Trigger.OnKilled(Airfield, function()
		GDI.MarkCompletedObjective(DestroyObjective)
	end)

	Utils.Do(NodBase, function(structure)
		Trigger.OnKilled(structure, function()
			GDI.MarkCompletedObjective(DestroyObjective)
		end)
	end)

	Trigger.OnAllKilled(IslandSamSites, function()
		TransportFlare = Actor.Create('flare', true, { Owner = GDI, Location = Flare.Location })
		Reinforcements.ReinforceWithTransport(GDI, 'tran', nil, { lstStart.Location, TransportRally.Location })
	end)

	Trigger.OnKilled(CivFleeTrigger, function()
		if not Civilian.IsDead then
			Civilian.Move(CivHideOut.Location)
		end
	end)

	Trigger.OnKilled(AttackTrigger2, function()
		Utils.Do(FlameSquad, function(unit)
			if not unit.IsDead then
				unit.Patrol(FlameSquadRoute, false)
			end
		end)
	end)

	Trigger.OnEnteredFootprint(AttackCellTriggerActivator, function(a, id)
		if a.Owner == GDI then
			Utils.Do(AttackUnits, function(unit)
				if not unit.IsDead then
					unit.AttackMove(waypoint10.Location)
				end
			end)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Utils.Do(HuntTriggerActivator, function(unit)
		Trigger.OnDamaged(unit, function()
			Utils.Do(Nod.GetGroundAttackers(), IdleHunt)
		end)
	end)

	Trigger.AfterDelay(5, function()
		Utils.Do(Nod.GetGroundAttackers(), function(unit)
			Trigger.OnKilled(unit, function()
				KillCounter = KillCounter + 1
				if KillCounter >= KillCounterHuntThreshold then
					Utils.Do(Nod.GetGroundAttackers(), IdleHunt)
				end
			end)
		end)
	end)

	Utils.Do(FootPatrol1Squad, function(unit)
		unit.Patrol(FootPatrol1Route, true)
	end)

	Utils.Do(FootPatrol2Squad, function(unit)
		unit.Patrol(FootPatrol2Route, true, 50)
	end)

	Utils.Do(FootPatrol3Squad, function(unit)
		unit.Patrol(FootPatrol3Route, true, 50)
	end)

	Utils.Do(FootPatrol5Squad, function(unit)
		unit.Patrol(FootPatrol5Route, true, 50)
	end)

	AttackTrigger2.Patrol(FootPatrol4Route, true, 25)
	LightTank1.Move(waypoint6.Location)
	Buggy1.Patrol(Buggy1Route, true, 25)
	Buggy2.Patrol(Buggy2Route, true, 25)

	Camera.Position = UnitsRally.CenterPosition
	Media.PlaySpeechNotification(GDI, "Reinforce")
	ReinforceWithLandingCraft(GDI, { CommandoType }, lstStart.Location, lstEnd.Location, UnitsRally.Location)
end

Tick = function()
	if DateTime.GameTime > DateTime.Seconds(5) and GDI.HasNoRequiredUnits() then
		GDI.MarkFailedObjective(DestroyObjective)
	end
end
