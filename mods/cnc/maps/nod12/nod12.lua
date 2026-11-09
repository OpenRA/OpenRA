--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- Port of the original Nod mission 12 ("Secure Ion Cannon Security Codes",
-- SCB12EA.INI).

StartingCashBonus = { easy = 2000, normal = 1000, hard = 0 }

-- Original [CellTriggers] for ion1, the two areas that hand the Ion Cannon
-- to GDI.
IonCannonCells = {
	CPos.New(44, 24), CPos.New(45, 24), CPos.New(46, 24),
	CPos.New(45, 23), CPos.New(46, 23), CPos.New(47, 23),
	CPos.New(46, 22), CPos.New(47, 22), CPos.New(48, 22),
	CPos.New(34, 6), CPos.New(35, 6), CPos.New(36, 6), CPos.New(37, 6),
	CPos.New(34, 7), CPos.New(35, 7), CPos.New(36, 7), CPos.New(37, 7),
	CPos.New(34, 8), CPos.New(35, 8), CPos.New(36, 8), CPos.New(37, 8),
}

InitPlayers = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
end

-- The original scenario sets every unit to Guard, Area Guard or Harvest.
-- Ground defenders open fire on what they see (the mission felt passive when
-- they had to be hit first); harvesters keep Defend so they do not walk off
-- their route.
SetInitialStances = function()
	Utils.Do(Nod.GetActors(), function(actor)
		if actor.HasProperty("Stance") then
			actor.Stance = "Defend"
		end
	end)

	Utils.Do(GDI.GetActors(), function(actor)
		if actor.HasProperty("Stance") then
			if actor.HasProperty("FindResources") then
				actor.Stance = "Defend"
			else
				actor.Stance = "AttackAnything"
			end
		end
	end)
end

-- The original `win` trigger (Cap=Win/Des=Lose) is attached to the Advanced
-- Communications Center: capturing it wins the mission, destroying it loses.
CenterCaptured = function(_, _, _, newOwner)
	if newOwner ~= Nod then
		return
	end

	IonCannonOnline = false
	Nod.MarkCompletedObjective(CaptureCenter)
end

CenterDestroyed = function()
	IonCannonOnline = false
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("advanced-communications-center-destroyed"))

	-- Capturing it already won the mission. A late hit during the game-over
	-- delay must not flip the completed objective to failed.
	if Nod.IsObjectiveCompleted(CaptureCenter) or Nod.IsObjectiveFailed(CaptureCenter) then
		return
	end

	Nod.MarkFailedObjective(CaptureCenter)
end

WorldLoaded = function()
	InitPlayers()

	-- The original mission starts with the camera on waypoint 26.
	Camera.Position = DefaultCameraPosition.CenterPosition

	InitObjectives(Nod)

	CaptureCenter = AddPrimaryObjective(Nod, "capture-advanced-communications-center")
	StopReinforcements = AddSecondaryObjective(Nod, "destroy-gdi-construction-yard")
	StopAirstrikes = AddSecondaryObjective(Nod, "destroy-gdi-communications-center")

	Nod.Cash = Nod.Cash + StartingCashBonus[Difficulty or "normal"]

	SetInitialStances()

	-- The GDI harvesters start harvesting as soon as Nod has deployed a
	-- Construction Yard. Until then they stay parked (see StartGDIHarvesting).
	Utils.Do(GDI.GetActorsByType("harv"), function(harvester)
		harvester.Stop()
	end)

	-- Optional objectives: the Construction Yard feeds the attack waves, the
	-- Communications Center calls in the A10 strikes.
	if IsLive(GDICYard) then
		Trigger.OnKilled(GDICYard, function()
			WavesStopped = true
			Nod.MarkCompletedObjective(StopReinforcements)
		end)
	end

	-- GDI calls in the A10 through a dedicated proxy actor. HQ must not
	-- provide AirstrikePower: Nod can build a Communications Center and must
	-- never gain the airstrike, regardless of capture logic.
	GDIAirstrikeProxy = Actor.Create("airstrike.proxy", true, { Owner = GDI })

	if IsLive(GDIHQ) then
		Trigger.OnKilled(GDIHQ, function()
			AirstrikeStopped = true
			Nod.MarkCompletedObjective(StopAirstrikes)
		end)
	end

	Trigger.OnCapture(GDIAdvComCenter, CenterCaptured)
	Trigger.OnKilled(GDIAdvComCenter, CenterDestroyed)

	-- ion2: the Mammoth Tank guarding the center carries the firing codes.
	Trigger.OnKilled(IonCannonMammoth, EnableIonCannon)

	-- ion1: entering one of the original cell areas hands GDI the Ion Cannon.
	Trigger.OnEnteredFootprint(IonCannonCells, function(actor, id)
		if actor.Owner ~= Nod or IonCannonOnline then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		EnableIonCannon()
	end)

	SetupAI()

	-- brdg: the two Mammoth Tanks guarding the bridge target the unit that
	-- attacked them and pursue it until it is destroyed. Registered after
	-- SetupAI, which also attaches a damage handler to these actors: a second
	-- Trigger.OnDamaged on the same actor silently replaces the first one.
	-- The liveness check keeps the actor reference out of Eluant, a "null
	-- actor" would abort the mission with a NullReferenceException.
	Utils.Do({ BridgeMammoth1, BridgeMammoth2 }, function(unit)
		if not IsLive(unit) then
			return
		end

		Trigger.OnDamaged(unit, function(_, attacker)
			SendBridgeResponse(attacker)
		end)
	end)
end

Tick = function()
	if not Nod.IsObjectiveCompleted(CaptureCenter) and not Nod.IsObjectiveFailed(CaptureCenter)
		and DateTime.GameTime > DateTime.Seconds(2) and Nod.HasNoRequiredUnits() then
		Nod.MarkFailedObjective(CaptureCenter)
	end

	if not GDIHarvestingStarted and DateTime.GameTime % DateTime.Seconds(1) == 0 and NodHasConstructionYard() then
		StartGDIHarvesting()
	end
end
