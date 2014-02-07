print = Internal.Debug

OpenRA = { }

OpenRA.New = function(className, args)
	if args == nil then
		args = { }
	end
	return Internal.New(className, args)
end

OpenRA.RunAfterDelay = function(delay, func)
	if func == nil then error("No function specified", 2) end
	Internal.RunAfterDelay(delay, func)
end

OpenRA.SetViewportCenterPosition = function(position)
	WorldRenderer.Viewport:Center(position)
end

OpenRA.GetViewportCenterPosition = function()
	return WorldRenderer.Viewport.CenterPosition
end

OpenRA.GetDifficulty = function()
	return World.LobbyInfo.GlobalSettings.Difficulty
end

OpenRA.IsSinglePlayer = function()
	return World.LobbyInfo:get_IsSinglePlayer()
end

OpenRA.GetPlayer = function(internalName)
	return Utils.EnumerableFirstOrNil(World.Players, function(p) return p.InternalName == internalName end)
end

OpenRA.GetPlayers = function(func)
	return Utils.EnumerableWhere(World.Players, func)
end

OpenRA.SetWinState = function(player, winState)
	Internal.SetWinState(player, winState)
end

OpenRA.GetRandomInteger = function(low, high)
	if high <= low then
		return low
	else
		return Internal.GetRandomInteger(low, high)
	end
end

OpenRA.TakeOre = function(player, amount)
	Actor.Trait(player.PlayerActor, "PlayerResources"):TakeOre(amount)
end

OpenRA.TakeCash = function(player, amount)
	Actor.Trait(player.PlayerActor, "PlayerResources"):TakeCash(amount)
end

OpenRA.GiveOre = function(player, amount)
	Actor.Trait(player.PlayerActor, "PlayerResources"):GiveOre(amount)
end

OpenRA.GiveCash = function(player, amount)
	Actor.Trait(player.PlayerActor, "PlayerResources"):GiveCash(amount)
end

OpenRA.CanGiveOre = function(player, amount)
	return Actor.Trait(player.PlayerActor, "PlayerResources"):CanGiveOre(amount)
end

OpenRA.GetOreCapacity = function(player)
	return Actor.Trait(player.PlayerActor, "PlayerResources").OreCapacity
end

OpenRA.GetOre = function(player)
	return Actor.Trait(player.PlayerActor, "PlayerResources").Ore
end

OpenRA.GetCash = function(player)
	return Actor.Trait(player.PlayerActor, "PlayerResources").Cash
end
