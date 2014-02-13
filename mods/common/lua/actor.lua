Actor = { }

Actor.Create = function(name, init)
	if name == nil then error("No actor name specified", 2) end
	if init.Owner == nil then error("No actor owner specified", 2) end
	local td = OpenRA.New("TypeDictionary")
	local addToWorld = true
	for key, value in pairs(init) do
		if key == "AddToWorld" then
			addToWorld = value
		else
			td:Add(OpenRA.New(key .. "Init", { value }))
		end
	end
	return World:CreateActor(addToWorld, name, td)
end

Actor.Turn = function(actor, facing)
	actor:QueueActivity(OpenRA.New("Turn", { { facing, "Int32" } }))
end

Actor.Move = function(actor, location)
	Actor.MoveNear(actor, location, 0)
end

Actor.MoveNear = function(actor, location, nearEnough)
	actor:QueueActivity(OpenRA.New("Move", { location, WRange.FromCells(nearEnough) }))
end

Actor.ScriptedMove = function(actor, location)
	if Actor.HasTrait(actor, "Helicopter") then
		Internal.HeliFlyToPos(actor, location.CenterPosition)
	else
		actor:QueueActivity(OpenRA.New("Move", { location }))
	end
end

Actor.AfterMove = function(actor)
	local heli = Actor.TraitOrDefault(actor, "Helicopter")
	if heli ~= nil then
		Actor.Turn(actor, heli.Info.InitialFacing)
		Actor.HeliLand(actor, true)
	end
end

Actor.Teleport = function(actor, location)
	actor:QueueActivity(OpenRA.New("SimpleTeleport", { location }))
end

Actor.AttackMove = function(actor, location, nearEnough)
	Internal.AttackMove(actor, location, nearEnough or 0)
end

Actor.HeliFly = function(actor, position)
	Internal.HeliFlyToPos(actor, position)
end

Actor.HeliLand = function(actor, requireSpace)
	actor:QueueActivity(OpenRA.New("HeliLand", { requireSpace }))
end

Actor.Fly = function(actor, position)
	Internal.FlyToPos(actor, position)
end

Actor.FlyAttackActor = function(actor, targetActor)
	Internal.FlyAttackActor(actor, targetActor)
end

Actor.FlyAttackCell = function(actor, location)
	Internal.FlyAttackCell(actor, location)
end

Actor.FlyOffMap = function(actor)
	actor:QueueActivity(OpenRA.New("FlyOffMap"))
end

Actor.Hunt = function(actor)
	actor:QueueActivity(OpenRA.New("Hunt", { actor }))
end

Actor.UnloadCargo = function(actor, unloadAll)
	actor:QueueActivity(OpenRA.New("UnloadCargo", { actor, unloadAll }))
end

Actor.Harvest = function(actor)
	actor:QueueActivity(OpenRA.New("FindResources"))
end

Actor.Scatter = function(actor)
	local mobile = Actor.Trait(actor, "Mobile")
	mobile:Nudge(actor, actor, true)
end

Actor.Wait = function(actor, period)
	actor:QueueActivity(OpenRA.New("Wait", { { period, "Int32" } }))
end

Actor.WaitFor = function(actor, func)
	Internal.WaitFor(actor, func)
end

Actor.CallFunc = function(actor, func)
	Internal.CallFunc(actor, func)
end

Actor.DeployTransform = function(actor)
	Actor.CallFunc(actor, function()
		-- Queue the transform order
		Actor.Trait(actor, "Transforms"):DeployTransform(true)
	end)
end

Actor.RemoveSelf = function(actor)
	actor:QueueActivity(OpenRA.New("RemoveSelf"))
end

Actor.Stop = function(actor)
	actor:CancelActivity()
end

Actor.IsDead = function(actor)
	return Internal.IsDead(actor)
end

Actor.IsInWorld = function(actor)
	return actor.IsInWorld
end

Actor.Owner = function(actor)
	return actor.Owner
end

Actor.Facing = function(actor)
	return Actor.Trait(actor, "IFacing"):get_Facing()
end

Actor.IsIdle = function(actor)
	return actor.IsIdle
end

Actor.SetStance = function(actor, stance)
	Internal.SetUnitStance(actor, stance)
end

Actor.OnKilled = function(actor, eh)
	Actor.Trait(actor, "LuaScriptEvents").OnKilled:Add(eh)
end

Actor.OnAddedToWorld = function(actor, eh)
	Actor.Trait(actor, "LuaScriptEvents").OnAddedToWorld:Add(eh)
end

Actor.OnRemovedFromWorld = function(actor, eh)
	Actor.Trait(actor, "LuaScriptEvents").OnRemovedFromWorld:Add(eh)
end

Actor.OnCaptured = function(actor, eh)
	Actor.Trait(actor, "LuaScriptEvents").OnCaptured:Add(eh)
end

Actor.ActorsWithTrait = function(className)
	local ret = { }
	for item in Utils.Enumerate(Internal.ActorsWithTrait(className)) do
		table.insert(ret, item.Actor)
	end
	return ret
end

Actor.HasTrait = function(actor, className)
	return Internal.HasTrait(actor, className)
end

Actor.TraitOrDefault = function(actor, className)
	return Internal.TraitOrDefault(actor, className)
end

Actor.Trait = function(actor, className)
	return Internal.Trait(actor, className)
end

Actor.ReturnToBase = function(actor, airfield)
	actor:QueueActivity(OpenRA.New("ReturnToBase", { actor, airfield }))
end

Actor.Patrol = function(actor, waypoints, wait, loop)
	Utils.Do(waypoints, function(wpt)
		Actor.AttackMove(actor, wpt.Location, 3)
		Actor.Wait(actor, wait or 0)
	end)
	if loop or loop == nil then
		Actor.CallFunc(actor, function() Actor.Patrol(actor, waypoints, wait, loop) end)
	end
end

Actor.PatrolUntil = function(actor, waypoints, wait, func)
	if func == nil then error("No function specified", 2) end
	Actor.Patrol(actor, waypoints, wait, false)
	if not func(actor) then
		Actor.CallFunc(actor, function() Actor.PatrolUntil(actor, waypoints, wait, func) end)
	end
end
