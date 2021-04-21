--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
Jeeps = { Jeep1, Jeep2 }
JeepWaypoints = { JeepWaypoint1.Location, JeepWaypoint2.Location }
OreAttackers = { OreAttack1, OreAttack2, OreAttack3, OreAttack4, OreAttack5, OreAttack6, OreAttack7, OreAttack8 }
RadarSites = { Radar1, Radar2, Radar3, Radar4 }
StartAttack = { StartAttack1, StartAttack2, StartAttack3, StartAttack4, StartAttack5 }
ChronoDemolitionTrigger = { CPos.New(35,96), CPos.New(36,96), CPos.New(37,96), CPos.New(37,97), CPos.New(38,97), CPos.New(39,97) }
OreAttackTrigger = { CPos.New(105,67), CPos.New(106,67), CPos.New(107,67), CPos.New(108,67), CPos.New(109,67), CPos.New(110,67) }

Start = function()
	Reinforcements.Reinforce(USSR, { "mcv" }, { MCVEntry.Location, DefaultCameraPosition.Location }, 5)

	Utils.Do(Jeeps, function(jeep)
		jeep.Patrol(JeepWaypoints, true, 125)
	end)

	Utils.Do(StartAttack, function(a)
		IdleHunt(a)
	end)

	ChronoCam = Actor.Create("camera", true, { Owner = USSR, Location = Chronosphere.Location})
end

MissionTriggers = function()
	Trigger.OnAllKilledOrCaptured(RadarSites, function()
		USSR.MarkCompletedObjective(TakeDownRadar)
		ChronoshiftAlliedUnits()
	end)

	Trigger.OnCapture(Chronosphere, function()
		if not USSR.IsObjectiveCompleted(TakeDownRadar) then
			Media.DisplayMessage(UserInterface.Translate("chrono-trap-triggered"), UserInterface.Translate("headquarters"))
			Chronosphere.Kill()
		else
			USSR.MarkCompletedObjective(CaptureChronosphere)
		end
	end)

	Trigger.OnKilled(Chronosphere, function()
		USSR.MarkFailedObjective(CaptureChronosphere)
	end)

	local chronoTriggered
	Trigger.OnEnteredFootprint(ChronoDemolitionTrigger, function(actor, id)
		if actor.Owner == USSR and not chronoTriggered and not USSR.IsObjectiveCompleted(TakeDownRadar) then
			Trigger.RemoveFootprintTrigger(id)
			chronoTriggered = true
			Media.DisplayMessage(UserInterface.Translate("chrono-trap-triggered"), UserInterface.Translate("headquarters"))
			Chronosphere.Kill()
		end
	end)

	Trigger.OnEnteredProximityTrigger(ChinookLZ.CenterPosition, WDist.FromCells(5), function(actor, id)
		if actor.Owner == USSR and actor.Type == "harv" then
			Trigger.RemoveProximityTrigger(id)
			SendChinook()
		end
	end)

	local oreAttackTriggered
	Trigger.OnEnteredFootprint(OreAttackTrigger, function(actor, id)
		if actor.Owner == USSR and not oreAttackTriggered then
			Trigger.RemoveFootprintTrigger(id)
			oreAttackTriggered = true

			Utils.Do(OreAttackers, function(a)
				if not a.IsDead then
					IdleHunt(a)
				end
			end)
		end
	end)
end

ChronoshiftAlliedUnits = function()
	if Chronosphere.IsDead then
		return
	end

	local cells = Utils.ExpandFootprint({ ChronoshiftPoint.Location }, false)
	local units = { }
	for i = 1, #cells do
		local unit = Actor.Create("2tnk", true, { Owner = Greece, Facing = Angle.North })
		units[unit] = cells[i]
		IdleHunt(unit)
	end
	Chronosphere.Chronoshift(units)
end

Tick = function()
	Greece.Cash = 20000

	if USSR.HasNoRequiredUnits() then
		Greece.MarkCompletedObjective(AlliesObjective)
	end
end

WorldLoaded = function()
	USSR = Player.GetPlayer("USSR")
	Greece = Player.GetPlayer("Greece")
	GoodGuy = Player.GetPlayer("GoodGuy")

	InitObjectives(USSR)

	AlliesObjective = AddPrimaryObjective(Greece, "")
	TakeDownRadar = AddPrimaryObjective(USSR, "destroy-allied-radar-sites")
	CaptureChronosphere = AddPrimaryObjective(USSR, "capture-the-chronosphere")

	Camera.Position = DefaultCameraPosition.CenterPosition
	Start()
	MissionTriggers()
	ActivateAI()
	GreeceWarFactory.RallyPoint = WarFactoryRally.Location
	if Difficulty == "hard" then
		V2A.Destroy()
		V2B.Destroy()
	end
end
