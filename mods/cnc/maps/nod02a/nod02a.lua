NodUnits = { "bggy", "e1", "e1", "e1", "e1", "e1", "bggy", "e1", "e1", "e1", "bggy" }
NodBaseBuildings = { "hand", "fact", "nuke" }

DfndActorTriggerActivator = { Refinery, Barracks, Powerplant, Yard }
Atk3ActorTriggerActivator = { Guard1, Guard2, Guard3, Guard4, Guard5, Guard6, Guard7 }

Atk1CellTriggerActivator = { CPos.New(45,37), CPos.New(44,37), CPos.New(45,36), CPos.New(44,36), CPos.New(45,35), CPos.New(44,35), CPos.New(45,34), CPos.New(44,34) }
Atk4CellTriggerActivator = { CPos.New(50,47), CPos.New(49,47), CPos.New(48,47), CPos.New(47,47), CPos.New(46,47), CPos.New(45,47), CPos.New(44,47), CPos.New(43,47), CPos.New(42,47), CPos.New(41,47), CPos.New(40,47), CPos.New(39,47), CPos.New(38,47), CPos.New(37,47), CPos.New(50,46), CPos.New(49,46), CPos.New(48,46), CPos.New(47,46), CPos.New(46,46), CPos.New(45,46), CPos.New(44,46), CPos.New(43,46), CPos.New(42,46), CPos.New(41,46), CPos.New(40,46), CPos.New(39,46), CPos.New(38,46) }

Atk2TriggerFunctionTime = DateTime.Seconds(40)
Atk5TriggerFunctionTime = DateTime.Minutes(1) + DateTime.Seconds(15)
Atk6TriggerFunctionTime = DateTime.Minutes(1) + DateTime.Seconds(20)
Atk7TriggerFunctionTime = DateTime.Seconds(50)
Pat1TriggerFunctionTime = DateTime.Seconds(30)

Atk1Waypoints = { waypoint2, waypoint4, waypoint5, waypoint6 }
Atk2Waypoints = { waypoint2, waypoint5, waypoint7, waypoint6 }
Atk3Waypoints = { waypoint2, waypoint4, waypoint5, waypoint9 }
Atk4Waypoints = { waypoint0, waypoint8, waypoint9 }
Pat1Waypoints = { waypoint0, waypoint1, waypoint2, waypoint3 }

UnitToRebuild = 'e1'
GDIStartUnits = 0

getActors = function(owner, units)
	local maxUnits = 0
	local actors = { }
	for type, count in pairs(units) do
		local globalActors = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)
			return actor.Owner == owner and actor.Type == type and not actor.IsDead
		end)
		if #globalActors < count then
			maxUnits = #globalActors
		else
			maxUnits = count
		end
		for i = 1, maxUnits, 1 do
			actors[#actors + 1] = globalActors[i]
		end
	end
	return actors
end

InsertNodUnits = function()
	Media.PlaySpeechNotification(Nod, "Reinforce")
	Reinforcements.Reinforce(Nod, NodUnits, { UnitsEntry.Location, UnitsRally.Location }, 15)
	Reinforcements.Reinforce(Nod, { "mcv" }, { McvEntry.Location, McvRally.Location })
end

OnAnyDamaged = function(actors, func)
	Utils.Do(actors, function(actor)
		Trigger.OnDamaged(actor, func)
	end)
end

CheckForBase = function(player, buildings)
	local checked = { }
	local baseBuildings = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)
		if actor.Owner ~= Nod or Utils.Any(checked, function(bldng) return bldng.Type == actor.Type end) then
			return false
		end

		local found = false
		for i = 1, #buildings, 1 do
			if actor.Type == buildings[i] then
				found = true
				checked[#checked + 1] = actor
			end
		end
		return found
	end)
	return #baseBuildings >= 3
end

DfndTriggerFunction = function()
	local list = GDI.GetGroundAttackers()
	Utils.Do(list, function(unit)
		IdleHunt(unit)
	end)
end

Atk2TriggerFunction = function()
	local MyActors = getActors(GDI, { ['e1'] = 3 })
	Utils.Do(MyActors, function(actor)
		Atk2Movement(actor)
	end)
end

Atk3TriggerFunction = function()
	if not Atk3TriggerSwitch then
		Atk3TriggerSwitch = true
		MyActors = getActors(GDI, { ['e1'] = 4 })
		Utils.Do(MyActors, function(actor)
			Atk3Movement(actor)
		end)
	end
end

Atk5TriggerFunction = function()
	local MyActors = getActors(GDI, { ['e1'] = 3 })
	Utils.Do(MyActors, function(actor)
		Atk2Movement(actor)
	end)
end

Atk6TriggerFunction = function()
	local MyActors = getActors(GDI, { ['e1'] = 4 })
	Utils.Do(MyActors, function(actor)
		Atk3Movement(actor)
	end)
end

Atk7TriggerFunction = function()
	local MyActors = getActors(GDI, { ['e1'] = 3 })
	Utils.Do(MyActors, function(actor)
		Atk4Movement(actor)
	end)
end

Pat1TriggerFunction = function()
	local MyActors = getActors(GDI, { ['e1'] = 3 })
	Utils.Do(MyActors, function(actor)
		Pat1Movement(actor)
	end)
end

Atk1Movement = function(unit)
	Utils.Do(Atk1Waypoints, function(waypoint)
		unit.AttackMove(waypoint.Location)
	end)
	IdleHunt(unit)
end

Atk2Movement = function(unit)
	Utils.Do(Atk2Waypoints, function(waypoint)
		unit.AttackMove(waypoint.Location)
	end)
	IdleHunt(unit)
end

Atk3Movement = function(unit)
	Utils.Do(Atk3Waypoints, function(waypoint)
		unit.AttackMove(waypoint.Location)
	end)
	IdleHunt(unit)
end

Atk4Movement = function(unit)
	Utils.Do(Atk4Waypoints, function(waypoint)
		unit.AttackMove(waypoint.Location)
	end)
	IdleHunt(unit)
end

Pat1Movement = function(unit)
	Utils.Do(Pat1Waypoints, function(waypoint)
		unit.Move(waypoint.Location)
	end)
	IdleHunt(unit)
end

initialSong = "ind2"
PlayMusic = function()
	Media.PlayMusic(initialSong, PlayMusic)
	initialSong = nil
end

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	Trigger.OnObjectiveAdded(Nod, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(Nod, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)

	Trigger.OnObjectiveFailed(Nod, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerWon(Nod, function()
		Media.PlaySpeechNotification(Nod, "Win")
	end)

	Trigger.OnPlayerLost(Nod, function()
		Media.PlaySpeechNotification(Nod, "Lose")
	end)

	NodObjective1 = Nod.AddPrimaryObjective("Build a base.")
	NodObjective2 = Nod.AddPrimaryObjective("Destroy the GDI base.")
	GDIObjective = GDI.AddPrimaryObjective("Kill all enemies.")

	OnAnyDamaged(Atk3ActorTriggerActivator, Atk3TriggerFunction)
	Trigger.OnAllRemovedFromWorld(DfndActorTriggerActivator, DfndTriggerFunction)

	Trigger.AfterDelay(Atk2TriggerFunctionTime, Atk2TriggerFunction)
	Trigger.AfterDelay(Atk5TriggerFunctionTime, Atk5TriggerFunction)
	Trigger.AfterDelay(Atk6TriggerFunctionTime, Atk6TriggerFunction)
	Trigger.AfterDelay(Atk7TriggerFunctionTime, Atk7TriggerFunction)
	Trigger.AfterDelay(Pat1TriggerFunctionTime, Pat1TriggerFunction)

	Trigger.OnEnteredFootprint(Atk1CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			MyActors = getActors(GDI, { ['e1'] = 5 })
			Utils.Do(MyActors, function(actor)
				Atk1Movement(actor)
			end)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	Trigger.OnEnteredFootprint(Atk4CellTriggerActivator, function(a, id)
		if a.Owner == Nod then
			MyActors = getActors(GDI, { ['e1'] = 3 } )
			Utils.Do(MyActors, function(actor)
				Atk2Movement(actor)
			end)
			Trigger.RemoveFootprintTrigger(id)
		end
	end)

	PlayMusic()

	Trigger.AfterDelay(0, getStartUnits)
	InsertNodUnits()
end

Tick = function()
	if GDI.HasNoRequiredUnits() then
		Nod.MarkCompletedObjective(NodObjective2)
	end

	if Nod.HasNoRequiredUnits() then
		if DateTime.GameTime > 2 then
			GDI.MarkCompletedObjective(GDIObjective)
		end
	end

	if DateTime.GameTime % DateTime.Seconds(1) == 0 and not Nod.IsObjectiveCompleted(NodObjective1) and CheckForBase(Nod, NodBaseBuildings) then
		Nod.MarkCompletedObjective(NodObjective1)
	end

	if DateTime.GameTime % DateTime.Seconds(3) == 0 and Barracks.IsInWorld and Barracks.Owner == gdi then
		checkProduction(GDI)
	end
end

checkProduction = function(player)
	local Units = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)
		return actor.Owner == player and actor.Type == UnitToRebuild
	end)

	if #Units < GDIStartUnits then
		local unitsToProduce = GDIStartUnits - #Units
		if Barracks.IsInWorld and unitsToProduce > 0 then
			local UnitsType = { }
			for i = 1, unitsToProduce, 1 do
				UnitsType[i] = UnitToRebuild
			end
			Barracks.Build(UnitsType)
		end
	end
end

getStartUnits = function()
	local Units = Map.ActorsInBox(Map.TopLeft, Map.BottomRight, function(actor)
		return actor.Owner == GDI
	end)
	Utils.Do(Units, function(unit)
		if unit.Type == UnitToRebuild then
			GDIStartUnits = GDIStartUnits + 1
		end
	end)
end

IdleHunt = function(unit)
	if not unit.IsDead then
		Trigger.OnIdle(unit, unit.Hunt)
	end
end
