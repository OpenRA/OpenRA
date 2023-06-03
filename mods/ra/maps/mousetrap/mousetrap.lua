--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
AlliedReinforcements =
{
	start =  { "e1", "e1", "e1" },
	southHall = { "e1", "e1", "e1", "e1", "e1" },
	northHall = { "e1", "e1", "e1" },
	northEnding = { "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1" },
	southEnding = { "e1", "e1", "e1", "e1", "medi", "medi" },
}
SovietReinforcements =
{
	constant = { "e1", "e1", "e1" },
	sphere = { "e1", "e1", "e1", "e1", "e1" },
	selfDestructFlamers = { "e4", "e4", "e4", "e4" },
	selfDestructRifles = { "e1", "e1", "e1", "e1" },
}
USSR = Player.GetPlayer("USSR")
Greece = Player.GetPlayer("Greece")
GoodGuy = Player.GetPlayer("GoodGuy")
England = Player.GetPlayer("England")
Neutral = Player.GetPlayer("Neutral")

ConsoleTouchDistance = WDist.New(512)
ShiftDelay = DateTime.Seconds(2)
AttackerShiftDelay = DateTime.Seconds(1)
GeneralType = "stavros"
GeneralTypeHard = "stavros.hard"

EscapeBlocked = false
SelfDestructStarted = false
EngineersKilled = false
EastHallEntered = false
CurrentGeneralSphere = StartSphere

WorldLoaded = function()
	Camera.Position = DefaultCameraPosition.CenterPosition
	if Difficulty == "hard" then
		GeneralType = GeneralTypeHard
		ShiftSouthEndingReinforcements()
		SpawnPillbox()

		Trigger.AfterDelay(DateTime.Minutes(1), function()
			ShiftNorthReinforcements(DateTime.Minutes(1))
		end)
	end

	Trigger.AfterDelay(5, GeneralStart)
	SetObjectives()
	CheckRequiredUnits()
	InitialTriggers()
end

SetObjectives = function()
	InitObjectives(USSR)
	DefendStavros = AddPrimaryObjective(Greece, "")
	KillStavros = AddPrimaryObjective(USSR, "kill-stavros")
	SabotageFacility = AddPrimaryObjective(USSR, "sabotage-facility")
end

AlliedWin = function()
	if AlliesHaveWon then
		return
	end

	AlliesHaveWon = true
	Media.PlaySpeechNotification(USSR, "ObjectiveNotMet")

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Greece.MarkCompletedObjective(DefendStavros)
	end)
end

CheckRequiredUnits = function()
	if not SovietsHaveWon and USSR.HasNoRequiredUnits() then
		AlliedWin()
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(2), CheckRequiredUnits)
end

OnStartingEngineersKilled = function()
	EngineersKilled = true
	if SelfDestructStarted then
		return
	end

	Media.PlaySoundNotification(USSR, "AlertBleep")
	Media.DisplayMessage(UserInterface.Translate("all-engineers-killed"))
	Trigger.AfterDelay(DateTime.Seconds(1), AlliedWin)
end

OnShiftedEngineerKilled = function()
	if not SelfDestructStarted then
		USSR.MarkFailedObjective(SabotageFacility)
	end
end

OnStavrosKilled = function()
	SovietsHaveWon = true
	Media.PlaySpeechNotification(USSR, "ObjectiveMet")

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		USSR.MarkCompletedObjective(KillStavros)
	end)
end

-- Sphere movement functions.
EnterSphere = function(actor, sphere, onArrived)
	actor = actor or GetGeneral()
	-- Nil should only be an issue if self-destruct or escape gas triggers while Stavros shifts.
	-- SelfDestructSequence, GeneralControlHiding, and GeneralSouthwest account for this.
	if not actor then
		return
	end

	CurrentGeneralSphere = sphere
	actor.Infiltrate(sphere)

	Trigger.OnInfiltrated(sphere, function()
		Trigger.Clear(sphere, "OnInfiltrated")

		Trigger.AfterDelay(ShiftDelay, function()
			ChronoEffect(sphere)
		end)

		Trigger.AfterDelay(ShiftDelay * 2, onArrived)
	end)
end

ReinforceAtSphere = function(sphere, owner, types, onArrived)
	if sphere.IsDead then
		return { }
	end

	local exit = sphere.Location + CVec.New(0, 1)
	local troops = Reinforcements.Reinforce(owner, types, { exit }, 10, function(actor)
		if onArrived then
			onArrived(actor)
		end
	end)

	return troops
end

ReinforceGeneralAtSphere = function(sphere, onArrived)
	CurrentGeneralSphere = sphere

	local stavros = ReinforceAtSphere(sphere, GoodGuy, { GeneralType }, onArrived)[1]
	if sphere == ControlSphere then
		-- It's still possible to barely hit him with flamethrower spread damage.
		stavros.GrantCondition("invulnerability")
	end
	Trigger.OnKilled(stavros, OnStavrosKilled)

	return stavros
end

--[[
	These control Stavros' actions near each sphere.
	Start -> Southwest -> Control -> Gas.
	From there, different routes depending on the player's actions:
		Gas -> Escape.
		Gas -> Control (until self-destruct) -> Ending.
		Gas -> Ending.
]]--
GeneralStart = function()
	Trigger.AfterDelay(DateTime.Seconds(3), SpawnEscapeVehicle)

	Trigger.AfterDelay(DateTime.Seconds(20), function()
		HideCameras("Start")
	end)

	Media.PlaySpeechNotification(USSR, "StavrosCommander")
	Stavros.Move(StartSphere.Location + CVec.New(0, 2))
	EnterSphere(Stavros, StartSphere, GeneralSouthwest)
end

GeneralSouthwest = function()
	ShowCameras("Southwest")
	ReinforceGeneralAtSphere(SouthwestSphere, function(stavros)
		stavros.Move(SouthwestSphereRally.Location)

		stavros.CallFunc(function()
			if EscapeBlocked then
				GeneralSouthwestToControlAndGas()
			end
			ShiftStartReinforcements()
		end)
	end)
end

GeneralSouthwestToControlAndGas = function()
	EnterSphere(GetGeneral(), SouthwestSphere, function()
		ShowCameras("Control")
		ReinforceGeneralAtSphere(ControlSphere, function(stavros)
			stavros.Move(ControlNortheast.Location)
			stavros.CallFunc(SpawnNorthwestGas)

			if not EscapeBlocked then
				stavros.CallFunc(SpawnStartGas)
			end

			stavros.Wait(DateTime.Seconds(3))
			stavros.Move(ControlNorthwest.Location)

			stavros.CallFunc(function()
				Media.PlaySoundNotification(USSR, "ConsoleBeep")
				ShiftCenterReinforcements()
				Trigger.AfterDelay(DateTime.Seconds(6), GeneralControlToGas)
			end)
		end)
	end)
end

GeneralControlToGas = function()
	EnterSphere(GetGeneral(), ControlSphere, function()
		HideCameras("Control")
		ShowCameras("Gas")

		ReinforceGeneralAtSphere(GasSphere, function(stavros)
			if EastHallEntered then
				GeneralGasToControl()
				return
			end
			stavros.Move(GasSphereReveal.Location)
			stavros.Move(GasSphereRally.Location)

			stavros.CallFunc(function()
				if EastHallEntered then
					GeneralGasToControl()
				end
			end)
		end)
	end)
end

GeneralGasToControl = function()
	EnterSphere(GetGeneral(), GasSphere, GeneralControlHiding)
end

GeneralControlHiding = function()
	HideCameras("Gas")
	ShowCameras("Control")
	ReinforceGeneralAtSphere(ControlSphere, function(stavros)
		stavros.Move(ControlWest.Location)

		stavros.CallFunc(function()
			if not SelfDestructStarted then
				return
			end

			GeneralControlToEnding()
		end)
	end)
end

GeneralGasToEscape = function()
	EnterSphere(GetGeneral(), GasSphere, GeneralEscape)
end

GeneralEscape = function()
	HideCameras("Gas")
	ReinforceGeneralAtSphere(SouthwestSphere, function(stavros)
		local carrier = GoodGuy.GetActorsByType("apc")[1]

		Trigger.AfterDelay(DateTime.Seconds(1), function()
			EscapeGuy.EnterTransport(carrier)
		end)

		stavros.CallFunc(function()
			Media.PlaySoundNotification(USSR, "AlertBleep")
			Camera.Position = EscapeRally.CenterPosition
		end)

		stavros.Move(SouthwestSphereRally.Location)
		stavros.Move(EscapeGas1.Location)

		stavros.CallFunc(function()
			Media.PlaySpeechNotification(USSR, "StavrosMoveOut")
		end)

		stavros.EnterTransport(carrier)
	end)
end

GeneralGasToEnding = function()
	Trigger.AfterDelay(DateTime.Seconds(2), function()
		HideCameras("Gas")
	end)

	local stavros = GetGeneral()
	stavros.Move(EastGas2.Location)
	stavros.AttackMove(EndingIntersection.Location)
	stavros.AttackMove(EndingConsole.Location)
end

GeneralControlToEnding = function()
	EnterSphere(GetGeneral(), ControlSphere, function()
		HideCameras("Control")
		SpawnPlayerCamera(EndingSphereLeft.Location, nil, 0, "camera.small")
		ReinforceGeneralAtSphere(EndingSphereLeft, function(stavros)
			stavros.AttackMove(EndingConsole.Location)
		end)
	end)
end

GeneralEnding = function()
	ShiftNorthEndingReinforcements()
	Media.PlaySoundNotification(USSR, "ConsoleBeep")

	local stavros = GetGeneral()
	SpawnPlayerCamera(stavros.Location)

	stavros.Wait(DateTime.Seconds(1))
	stavros.AttackMove(EndingConsole.Location + CVec.New(0, 1))
	stavros.Stance = "AttackAnything"
end

-- Reinforcements.
InsertSovietReinforcements = function(team, entry, rally, delay)
	Trigger.AfterDelay(delay or 0, function()
		Reinforcements.Reinforce(USSR, SovietReinforcements[team], { entry, rally } )
	end)

	if team ~= "constant" then
		return
	end

	Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
	Trigger.AfterDelay(DateTime.Minutes(4), function()
		InsertSovietReinforcements(team, entry, rally)
	end)
end

ShiftSovietReinforcements = function()
	if SovietReinforcementsShifted or CenterSphere.IsDead then
		return
	end

	SovietReinforcementsShifted = true
	Media.PlaySoundNotification(USSR, "ConsoleBeep")
	ChronoEffect()

	Trigger.AfterDelay(AttackerShiftDelay, function()
		ReinforceAtSphere(CenterSphere, USSR, SovietReinforcements.sphere, function(actor)
			actor.AttackMove(ShiftedSovietRally.Location)
		end)
		SpawnPlayerCamera(CenterSphere.Location, DateTime.Seconds(2), 0, "camera.small")
	end)
end

ShiftCenterReinforcements = function()
	SpawnPlayerCamera(CenterSphere.Location, nil, 0, "camera.small")
	ChronoEffect()

	Trigger.AfterDelay(AttackerShiftDelay, function()
		local southHallRifles = ReinforceAtSphere(CenterSphere, Greece, AlliedReinforcements.southHall)
		GroupHuntFromRally(southHallRifles, CenterSphereRally.Location, SovietEntry.Location)
	end)
end

ShiftNorthReinforcements = function(interval)
	if NorthSphere.IsDead then
		return
	end

	SpawnPlayerCamera(NorthSphereReveal.Location, nil, 0, "camera.small")
	ChronoEffect(NorthSphere)

	Trigger.AfterDelay(AttackerShiftDelay, function()
		local northRifles = ReinforceAtSphere(NorthSphere, Greece, AlliedReinforcements.northHall)
		GroupHuntFromRally(northRifles, NorthSphereRally.Location)
	end)

	Trigger.AfterDelay(interval, function()
		ShiftNorthReinforcements(interval)
	end)
end

ShiftStartReinforcements = function()
	ChronoEffect()

	Trigger.AfterDelay(AttackerShiftDelay, function()
		local startRifles = ReinforceAtSphere(StartSphere, Greece, AlliedReinforcements.start)
		GroupHuntFromRally(startRifles, StartSphereRally.Location)
	end)
end

ShiftNorthEndingReinforcements = function()
	ChronoEffect()

	Trigger.AfterDelay(AttackerShiftDelay, function()
		local norts = ReinforceAtSphere(EndingSphereRight, Greece, AlliedReinforcements.northEnding, function(actor)
			actor.AddTag("Ending Group")
			actor.AttackMove(EndingSphereRally.Location)
		end)
		PrepareEndingTeam(norts)
	end)

	Trigger.AfterDelay(AttackerShiftDelay + DateTime.Seconds(4), function()
		DestroyBuilding(EndingSphereRight, nil, 0, "hidden")
	end)
end

ShiftSouthEndingReinforcements = function()
	local southers = ReinforceAtSphere(SoutheastSphere, Greece, AlliedReinforcements.southEnding, function(actor)
		actor.AddTag("Ending Group")
		actor.Move(SoutheastSphereRally.Location)
		if actor.Type == "medi" then
			actor.Move(SoutheastSphereRally.Location + CVec.New(0, 1))
		end
	end)
	PrepareEndingTeam(southers)
end

-- Supporting actors.
SpawnPlayerCamera = function(location, duration, delay, cameraType)
	duration = duration or DateTime.Seconds(6)
	cameraType = cameraType or "camera"

	Trigger.AfterDelay(delay or 0, function()
		local camera = Actor.Create(cameraType, true, { Owner = USSR, Location = location } )
		if duration < 0 then
			return
		end

		Trigger.AfterDelay(duration, camera.Destroy)
	end)
end

SetCameraGroupOwner = function(tag, owner)
	local cameras = Map.ActorsWithTag(tag)
	Utils.Do(cameras, function(camera)
		camera.Owner = owner
	end)
end

ShowCameras = function(tag)
	SetCameraGroupOwner(tag, USSR)
end

HideCameras = function(tag)
	SetCameraGroupOwner(tag, Neutral)
end

SpawnGas = function(waypoint, delay, sound)
	Trigger.AfterDelay(delay or 0, function()
		Actor.Create("flare", true, { Owner = England, Location = waypoint.Location } )
	end)

	if sound then
		Media.PlaySoundNotification(USSR, sound)
	end
end

SpawnEastGas = function()
	SpawnGas(EastGas1, 15, "GasHiss")
	SpawnGas(EastGas2)
end

SpawnNorthwestGas = function()
	SpawnPlayerCamera(SelfDestructReveal.Location, DateTime.Seconds(20))
	SpawnGas(NorthwestGas1, 15, "GasHiss")
	SpawnGas(NorthwestGas2)
end

SpawnStartGas = function()
	SpawnGas(StartConsole)
end

SpawnEscapeGas = function()
	EscapeBlocked = true

	SpawnGas(EscapeGas1, 0, "GasHiss")
	SpawnGas(EscapeGas2, 15)
	EscapeGuy.Scatter()

	if IsStavrosIdle() and CurrentGeneralSphere == SouthwestSphere then
		GeneralSouthwestToControlAndGas()
	end
end

SpawnEscapeVehicle = function()
	local vehiclePath = { EscapeEntry.Location, EscapeRally.Location }
	Reinforcements.Reinforce(GoodGuy, { "apc" }, vehiclePath, 0, function(vehicle)
		Trigger.OnKilled(EscapeGuy, function()
			ExitEscapeVehicle(vehicle)
		end)

		Trigger.OnPassengerEntered(vehicle, function(_, passenger)
			if not IsStavros(passenger) then
				return
			end

			ExitEscapeVehicle(vehicle, "stavros")
		end)
	end)
end

ExitEscapeVehicle = function(vehicle, stavros)
	vehicle.Move(EscapeEntry.Location)

	vehicle.CallFunc(function()
		if stavros then
			AlliedWin()
		end

		Trigger.AfterDelay(DateTime.Seconds(2), function()
			HideCameras("Southwest")
		end)
	end)

	vehicle.Destroy()
end

-- Hard's spy behavior and pillboxes.
SpawnSpy = function(sphere)
	sphere = sphere or StartSphere
	if Difficulty ~= "hard" or sphere.IsDead then
		return
	end
	ChronoEffect()

	Trigger.AfterDelay(ShiftDelay, function()
		local spy = ReinforceAtSphere(sphere, Greece, { "spy" } )[1]

		Trigger.OnAddedToWorld(spy, function()
			spy.DisguiseAsType("e1", USSR)
		end)

		Trigger.OnIdle(spy, function()
			SpyIdle(spy)
		end)
	end)
end

SpyIdle = function(spy)
	if SelfDestructStarted then
		SpyEscape(spy)
		return
	end

	local targets = Map.ActorsInCircle(spy.CenterPosition, WDist.FromCells(3), function(nearby)
		return nearby.Type == "e6"
	end)

	if targets and #targets > 0 then
		SpyAttack(spy, targets[1])
		return
	end

	SpySearch(spy)
end

SpyAttack = function(spy, target)
	spy.AttackMove(target.Location)

	spy.CallFunc(function()
		if EngineersKilled and not SelfDestructStarted then
			SpyPostMortem(spy)
		end
	end)

	spy.Wait(DateTime.Seconds(1))

	spy.CallFunc(function()
		spy.DisguiseAsType(target.Type, target.Owner)
	end)

	spy.Wait(DateTime.Seconds(1))
end

SpySearch = function(spy)
	local targets = Utils.Where(USSR.GetActorsByType("e6"), function(actor)
		return not actor.HasTag("shifted")
	end)

	if targets and #targets > 0 then
		local goal = Utils.Random(targets)
		spy.Move(goal.Location, 2)
		return
	end

	SpyEscape(spy)
end

SpyEscape = function(spy)
	spy.Move(SovietRally.Location, 1)
	spy.Move(SovietEntry.Location)
	spy.Destroy()
end

-- Avoid the spy killing both engineers, ending the mission, and being left in fog.
SpyPostMortem = function(spy)
	Media.PlaySpeechNotification(USSR, "SpyCommander")
	SpawnPlayerCamera(spy.Location, -1, 0, "camera.small")
	spy.Stop()
	spy.Wait(DateTime.Minutes(1))
end

SpawnPillbox = function()
	Actor.Create("pbox", true, { Owner = GoodGuy, Location = PillboxSpawn.Location } )

	CreateProximityOneShot(SouthHallProximity, WDist.FromCells(3), IsSoviet, function()
		SpawnPlayerCamera(PillboxSpawn.Location, nil, 0 ,"camera.tiny")
	end)
end

--  Group activities.
IsGroupIdle = function(actors)
	return Utils.All(actors, function(actor)
		return actor.IsIdle
	end)
end

GroupHunt = function(actors, goal, delay)
	Utils.Do(actors, function(actor)
		if actor.IsDead then
			return
		end

		Trigger.Clear(actor, "OnIdle")
		actor.Wait(delay)
		actor.AttackMove(goal or actor.Location, 1)
		actor.Hunt()
	end)
end

GroupHuntFromRally = function(actors, rally, goal, delay)
	if not actors or #actors < 1 then
		return
	end

	delay = delay or 10
	Utils.Do(actors, function(actor)
		Trigger.OnIdle(actor, function()

			if actor.Location == rally then
				if IsGroupIdle(actors) then
					GroupHunt(actors, goal, delay)
				end
			else
				actor.AttackMove(rally)
			end

		end)
	end)
end

PrepareEndingTeam = function(actors)
	OnAnyDamaged(actors, function()
		local defenders = Map.ActorsWithTag("Ending Group")
		local path = { EndingIntersection.Location, EndingSphereRally.Location }

		Utils.Do(defenders, function(actor)
			if actor.IsDead then
				return
			end

			Trigger.ClearAll(actor)
			actor.Patrol(path, true, DateTime.Seconds(1))
		end)
	end)
end

-- Starting trigger setups.
CreateProximityOneShot = function(waypoint, range, filter, func)
	Trigger.OnEnteredProximityTrigger(waypoint.CenterPosition, range, function(actor, id)
		if not filter(actor) then
			return
		end

		Trigger.RemoveProximityTrigger(id)
		func(actor)
		func = function()
			print("Duplicate event at " .. tostring(waypoint) .. " skipped.")
		end
	end)
end

InitialTriggers = function()
	Trigger.OnAllKilled(USSR.GetActorsByType("e6"), OnStartingEngineersKilled)

	Trigger.AfterDelay(DateTime.Minutes(4), function()
		InsertSovietReinforcements("constant", SovietEntry.Location, SovietRally.Location)
	end)

	Trigger.OnKilled(NorthSphere, function()
		SpawnPlayerCamera(NorthSphereReveal.Location, nil, 0, "camera.small")
	end)

	PrepareConsoles()
	PrepareOtherProximities()
	PrepareDamagedChronosphere()
end

PrepareConsole = function(waypoint, filter, func)
	CreateProximityOneShot(waypoint, ConsoleTouchDistance, filter, func)
end

PrepareConsoles = function()
	PrepareConsole(CenterSphereConsole, IsSovietHuman, function()
		DestroyBuilding(CenterSphere, "ConsoleBeep", 10)
	end)

	PrepareConsole(NorthSphereConsole, IsSovietHuman, function()
		DestroyBuilding(NorthSphere, "ConsoleBeep", 10, "hidden")
	end)

	PrepareConsole(StartConsole, IsSovietHuman, SpawnEscapeGas)
	PrepareConsole(ReinforcementConsole1, IsSovietHuman, ShiftSovietReinforcements)
	PrepareConsole(ReinforcementConsole2, IsSovietHuman, ShiftSovietReinforcements)
	PrepareConsole(SelfDestructConsole1, IsSovietHuman, BeginSelfDestruct)
	PrepareConsole(SelfDestructConsole2, IsSovietHuman, BeginSelfDestruct)
	PrepareConsole(EndingConsole, IsStavros, GeneralEnding)
end

PrepareOtherProximities= function()
	CreateProximityOneShot(DamagedSphereProximity, WDist.FromCells(3), IsSoviet, function()
		SpawnPlayerCamera(DamagedSphere.Location)
	end)

	CreateProximityOneShot(SoutheastSphereProximity, WDist.FromCells(3), IsSoviet, function()
		SpawnPlayerCamera(SoutheastSphereRally.Location)
	end)

	CreateProximityOneShot(SouthHallProximity, WDist.FromCells(2), IsSoviet, function()

		if not EscapeBlocked and CurrentGeneralSphere == SouthwestSphere then
			GeneralSouthwestToControlAndGas()
		end

		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(60, 91)), SpawnSpy)
	end)

	CreateProximityOneShot(EastHallProximity, WDist.FromCells(2), IsSoviet, function()
		EastHallEntered = true

		Trigger.AfterDelay(DateTime.Seconds(3), SpawnEastGas)
		local stavros = GetGeneral()
		if not (IsStavrosIdle(stavros) and stavros.Location == GasSphereRally.Location) then
			return
		end
		if SelfDestructStarted then
			GeneralGasToEnding()
			return
		end
		GeneralGasToControl()
	end)
end

PrepareDamagedChronosphere = function()
	Trigger.OnCapture(DamagedSphere, function(sphere)

		Trigger.AfterDelay(ShiftDelay, function()
			if sphere.IsInWorld then
				ChronoEffect(sphere)
			end
		end)

		Trigger.AfterDelay(ShiftDelay * 2, function()
			ReinforceAtSphere(NorthwestSphere, USSR, { "e6" }, function(newEngineer)
				Trigger.OnKilled(newEngineer, OnShiftedEngineerKilled)
				newEngineer.AddTag("shifted")

				newEngineer.CallFunc(function()
					SpawnPlayerCamera(NorthwestSphere.Location)
				end)

				newEngineer.Move(NorthwestSphereRally.Location)
			end)
		end)

		Trigger.AfterDelay(ShiftDelay * 3, function()
			if sphere.IsInWorld then
				ChronoEffect(sphere)
				DestroyBuilding(sphere, nil, DateTime.Seconds(1))
			end
		end)
	end)
end

-- Filters, fetchers, and such.
IsSoviet = function(actor)
	return actor.Owner == USSR
end

IsSovietHuman = function(actor)
	return actor.Owner == USSR and actor.Type ~= "dog"
end

IsStavros = function(actor)
	return actor.Type == GeneralType
end

IsStavrosIdle = function(stavros)
	stavros = stavros or GetGeneral()
	return stavros and stavros.IsIdle
end

IsEscapePossible = function()
	return not EscapeBlocked and CurrentGeneralSphere == GasSphere
end

GetGeneral = function()
	local generals = GoodGuy.GetActorsByType(GeneralType)
	if generals and #generals > 0 then
		return generals[1]
	end
end

-- Self-destruct and building effects.
BeginSelfDestruct = function()
	if SelfDestructStarted then
		return
	end

	SelfDestructStarted = true
	USSR.MarkCompletedObjective(SabotageFacility)
	Media.PlaySoundNotification(USSR, "ConsoleBuzz")

	Trigger.AfterDelay(DateTime.Seconds(1), function()
		SelfDestructAlarm()

		if IsEscapePossible() then
			GeneralGasToEscape()
		end

		if IsStavrosIdle() and CurrentGeneralSphere == ControlSphere then
			GeneralControlToEnding()
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(2), SelfDestructSequence)
end

SelfDestructSequence = function()
	DestroyBuilding(NorthwestPower)
	DestroyBuilding(SoutheastPower)
	local pillboxes = GoodGuy.GetActorsByType("pbox")
	Utils.Do(pillboxes, function(pillbox)
		DestroyBuilding(pillbox)
	end)

	Trigger.AfterDelay(DateTime.Seconds(3), function()
		if IsEscapePossible() then
			return
		end

		Media.PlaySpeechNotification(USSR, "ReinforcementsArrived")
		InsertSovietReinforcements("selfDestructFlamers", FlamerEntry.Location, FlamerRally.Location)
		InsertSovietReinforcements("selfDestructRifles", SovietEntry.Location, SovietRally.Location, DateTime.Seconds(1))
	end)

	Trigger.AfterDelay(DateTime.Seconds(9), function()
		DestroyBuilding(NortheastPower)
		DestroyBuilding(SouthwestPower)
	end)

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		DestroyBuilding(CenterSphere)
		DestroyBuilding(NorthSphere, nil, nil, "hidden")
	end)

	Trigger.AfterDelay(DateTime.Seconds(21), function()
		DestroyBuilding(NorthwestSphere)
		DestroyBuilding(GasSphere)
		DestroyBuilding(EndingSphereLeft)
		DestroyBuilding(EastSphere)
		DestroyBuilding(SoutheastSphere)
		DestroyBuilding(ControlSphere)
	end)
end

SelfDestructAlarm = function()
	Media.PlaySoundNotification(USSR, "AlertBuzzer")
	Trigger.AfterDelay(DateTime.Seconds(6), SelfDestructAlarm)
end

DestroyBuilding = function(actor, sound, delay, hidden)
	if actor.IsDead then
		return
	end

	delay = delay or Utils.RandomInteger(0, DateTime.Seconds(1))

	Trigger.AfterDelay(delay, function()
		if actor.IsDead then
			return
		end

		actor.Kill()
		if not hidden then
			SpawnPlayerCamera(actor.Location, nil, nil, "camera.small")
		end
	end)

	if sound then
		Media.PlaySoundNotification(USSR, sound)
	end
end

ChronoEffect = function(sphere)
	Media.PlaySoundNotification(USSR, "Chronoshift")
	if not sphere then
		return
	end

	local units = { }
	local target = ChronoDummyTarget.Location
	local dummy = Reinforcements.Reinforce(Neutral, { "1tnk" }, { ChronoDummyEntry.Location } )[1]
	units[dummy] = target

	sphere.Chronoshift(units, 0)
	Trigger.AfterDelay(1, dummy.Destroy)
end
