NodUnits = { "bggy", "e1", "e1", "e1", "e1", "e1", "bggy", "e1", "e1", "e1", "bggy" }
NodBaseBuildings = { "hand", "fact", "nuke" }

Grd2ActorTriggerActivator = { Refinery, Yard }
Atk4ActorTriggerActivator = { Guard1 }
Atk3ActorTriggerActivator = { Guard4 }
Atk6ActorTriggerActivator = { Guard2, Guard3 }
HuntActorTriggerActivator = { Refinery, Yard, Barracks, Plant, Silo1, Silo2 }

Atk8TriggerFunctionTime = DateTime.Minutes(1) + DateTime.Seconds(25)
Atk7TriggerFunctionTime = DateTime.Minutes(1) + DateTime.Seconds(20)

Gdi1Waypoints = { waypoint0, waypoint1, waypoint2, waypoint3 }
Gdi3Waypoints = { waypoint0, waypoint1, waypoint4, waypoint5, waypoint6, waypoint7, waypoint9 }

UnitToRebuild = 'e1'
GDIStartUnits = 0

Grd2TriggerFunction = function()
	if not Grd2TriggerSwitch then
		Grd2TriggerSwitch = true
		MyActors = getActors(enemy, { ['e1'] = 5 })
		Utils.Do(MyActors, function(actor)
			Gdi5Movement(actor)
		end)
	end
end

Atk8TriggerFunction = function()
	MyActors = getActors(enemy, { ['e1'] = 2 })
	Utils.Do(MyActors, function(actor)
		Gdi1Movement(actor)
	end)
end

Atk7TriggerFunction = function()
	MyActors = getActors(enemy, { ['e1'] = 3 })
	Utils.Do(MyActors, function(actor)
		Gdi3Movement(actor)
	end)
end

Atk4TriggerFunction = function()
	MyActors = getActors(enemy, { ['e1'] = 3 })
	Utils.Do(MyActors, function(actor)
		Gdi3Movement(actor)
	end)
end

Atk3TriggerFunction = function()
	MyActors = getActors(enemy, { ['e1'] = 2 })
	Utils.Do(MyActors, function(actor)
		Gdi1Movement(actor)
	end)
end

Atk6TriggerFunction = function()
	MyActors = getActors(enemy, { ['e1'] = 2 })
	Utils.Do(MyActors, function(actor)
		Gdi1Movement(actor)
	end)
end

Atk5TriggerFunction = function()
	if not Atk5TriggerSwitch then
		Atk5TriggerSwitch = true
		MyActors = getActors(enemy, { ['e1'] = 3 })
		Utils.Do(MyActors, function(actor)
			Gdi3Movement(actor)
		end)
	end
end

HuntTriggerFunction = function()
	local list = enemy.GetGroundAttackers()
	Utils.Do(list, function(unit)
		IdleHunt(unit)
	end)
end

Gdi5Movement = function(unit)
	IdleHunt(unit)
end

Gdi1Movement = function(unit)
	Utils.Do(Gdi1Waypoints, function(waypoint)
		unit.AttackMove(waypoint.Location)
	end)
	IdleHunt(unit)
end

Gdi3Movement = function(unit)
	Utils.Do(Gdi3Waypoints, function(waypoint)
		unit.AttackMove(waypoint.Location)
	end)
	IdleHunt(unit)
end

WorldLoaded = function()
	player = Player.GetPlayer("Nod")
	enemy = Player.GetPlayer("GDI")

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

	GDIObjective = enemy.AddPrimaryObjective("Kill all enemies.")
	NodObjective1 = player.AddPrimaryObjective("Build a base.")
	NodObjective2 = player.AddPrimaryObjective("Destroy all GDI units.")

	OnAnyDamaged(Grd2ActorTriggerActivator, Grd2TriggerFunction)
	Trigger.AfterDelay(Atk8TriggerFunctionTime, Atk8TriggerFunction)
	Trigger.AfterDelay(Atk7TriggerFunctionTime, Atk7TriggerFunction)
	Trigger.OnAllRemovedFromWorld(Atk4ActorTriggerActivator, Atk4TriggerFunction)
	Trigger.OnAllRemovedFromWorld(Atk3ActorTriggerActivator, Atk3TriggerFunction)
	Trigger.OnDamaged(Harvester, Atk5TriggerFunction)
	Trigger.OnAllRemovedFromWorld(HuntActorTriggerActivator, HuntTriggerFunction)

	Trigger.AfterDelay(0, getStartUnits)
	Harvester.FindResources()

	InsertNodUnits()
end

Tick = function()
	if player.HasNoRequiredUnits() then
		if DateTime.GameTime > 2 then
			enemy.MarkCompletedObjective(GDIObjective)
		end
	end

	if enemy.HasNoRequiredUnits() then
		player.MarkCompletedObjective(NodObjective2)
	end

	if DateTime.GameTime % DateTime.Seconds(1) == 0 and not player.IsObjectiveCompleted(NodObjective1) and CheckForBase(player) then
		player.MarkCompletedObjective(NodObjective1)
	end

	if DateTime.GameTime % DateTime.Seconds(3) == 0 and Barracks.IsInWorld and Barracks.Owner == enemy then
		checkProduction(enemy)
	end
end

CheckForBase = function(player)
	local buildings = 0

	Utils.Do(NodBaseBuildings, function(name)
		if #player.GetActorsByType(name) > 0 then
			buildings = buildings + 1
		end
	end)

	return buildings == #NodBaseBuildings
end

OnAnyDamaged = function(actors, func)
	Utils.Do(actors, function(actor)
		Trigger.OnDamaged(actor, func)
	end)
end

getActors = function(owner, units)
	local maxUnits = 0
	local actors = { }
	for type, count in pairs(units) do
		local globalActors = Utils.Where(Map.ActorsInWorld, function(actor)
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

checkProduction = function(player)
	local Units = Utils.Where(Map.ActorsInWorld, function(actor)
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
	local Units = Utils.Where(Map.ActorsInWorld, function(actor)
		return actor.Owner == enemy
	end)
	Utils.Do(Units, function(unit)
		if unit.Type == UnitToRebuild then
			GDIStartUnits = GDIStartUnits + 1
		end
	end)
end

InsertNodUnits = function()
	Media.PlaySpeechNotification(player, "Reinforce")
	Reinforcements.Reinforce(player, NodUnits, { UnitsEntry.Location, UnitsRally.Location }, 15)
	Reinforcements.Reinforce(player, { "mcv" }, { McvEntry.Location, McvRally.Location })
end

IdleHunt = function(unit)
	if not unit.IsDead then
		Trigger.OnIdle(unit, unit.Hunt)
	end
end
