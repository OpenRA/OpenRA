--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- SCB13EA: the original victory and defeat triggers require the destruction
-- of the opposing house. The briefing requests a Temple, but no original
-- trigger makes building or preserving it a condition for victory.
StartingCashBonus = { easy = 2000, normal = 1000, hard = 0 }
GDIReinforcementPassengers = {}
MissionFinished = false
OutcomeCheckPending = false

LandingZoneCells = {
	CPos.New(55, 47), CPos.New(56, 46), CPos.New(55, 46),
	CPos.New(54, 46), CPos.New(57, 45), CPos.New(56, 45), CPos.New(55, 45),
}

SendEmptyChinook = function(destination)
	if MissionFinished then return end
	Reinforcements.ReinforceWithTransport(GDISouth, "tran.capture", nil,
		{ CPos.New(61, 35), destination.Location })
end

TrackGDIReinforcements = function(passengers)
	for _, passenger in ipairs(passengers) do
		table.insert(GDIReinforcementPassengers, passenger)
	end
end

-- Empty aircraft are ignored by All Destr., but their passengers still count.
HouseEliminated = function(player)
	if not player.HasNoRequiredUnits() then
		return false
	end

	-- Reinforcement passengers exist before their transport enters the world.
	for _, passenger in ipairs(GDIReinforcementPassengers) do
		if passenger.Owner == player and not passenger.IsDead then
			return false
		end
	end

	for _, actorType in ipairs({ "tran", "tran.capture", "tran.in" }) do
		for _, transport in ipairs(player.GetActorsByType(actorType)) do
			if not transport.IsDead and transport.IsInWorld then
				for _, passenger in ipairs(transport.Passengers) do
					if not passenger.IsDead and passenger.Owner == player then
						return false
					end
				end
			end
		end
	end

	return true
end

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	GDIWest = Player.GetPlayer("GDIWest")
	GDISouth = Player.GetPlayer("GDISouth")

	Camera.Position = DefaultCameraPosition.CenterPosition
	InitObjectives(Nod)
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")
	BuildTemple = AddSecondaryObjective(Nod, "build-temple")
	Nod.Cash = Nod.Cash + StartingCashBonus[Difficulty or "normal"]

	-- dzne marks the northern landing zone when Nod reaches the south outpost.
	Trigger.OnEnteredFootprint(LandingZoneCells, function(actor, id)
		if actor.Owner ~= Nod or not actor.HasProperty("Move") then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		Actor.Create("flare", true, { Owner = Nod, Location = DefaultFlareLocation.Location })
	end)

	-- chn1/chn2: two empty GDI transport helicopters, at 1 and 3 original
	-- tenths of a minute, land near the south outpost and can be captured.
	Trigger.AfterDelay(DateTime.Seconds(6), function() SendEmptyChinook(waypoint0) end)
	Trigger.AfterDelay(DateTime.Seconds(18), function() SendEmptyChinook(waypoint1) end)

	SetupAI()
end

CheckOutcome = function()
	OutcomeCheckPending = false
	if MissionFinished then
		return
	end

	if DateTime.GameTime > DateTime.Seconds(2) and HouseEliminated(Nod) then
		MissionFinished = true
		Nod.MarkFailedObjective(EliminateGDI)
	elseif HouseEliminated(GDI) and HouseEliminated(GDIWest) and HouseEliminated(GDISouth) then
		MissionFinished = true
		Nod.MarkCompletedObjective(EliminateGDI)
	end
end

Tick = function()
	if MissionFinished then
		return
	end
	CheckArmorEscalation()

	-- Cargo is removed from its transport before it is added back to the world
	-- at frame end. Confirm elimination on the next tick to avoid a false result.
	if not OutcomeCheckPending and ((DateTime.GameTime > DateTime.Seconds(2) and HouseEliminated(Nod))
		or (HouseEliminated(GDI) and HouseEliminated(GDIWest) and HouseEliminated(GDISouth))) then
		OutcomeCheckPending = true
		Trigger.AfterDelay(1, CheckOutcome)
	end

	if DateTime.GameTime % DateTime.Seconds(1) == 0 and not Nod.IsObjectiveCompleted(BuildTemple)
		and #Nod.GetActorsByType("tmpl") > 0 then
		Nod.MarkCompletedObjective(BuildTemple)
	end
end
