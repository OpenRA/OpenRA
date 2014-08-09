MobileConstructionVehicle = { "mcv" }
EngineerReinforcements = { "e6", "e6", "e6" }
VehicleReinforcements = { "jeep" }

AttackerSquadSize = 3

MissionAccomplished = function()
	Mission.MissionOver({ player }, nil, true)
	Media.PlayMovieFullscreen("flag.vqa")
end

MissionFailed = function()
	Mission.MissionOver(nil, { player }, true)
	Media.PlayMovieFullscreen("gameover.vqa")
end

ReinforceFromSea = function(passengers)
	local hovercraft, troops = Reinforcements.Insert(player, "oldlst", passengers, { lstStart.Location, lstEnd.Location  }, { lstStart.Location })
	Media.PlaySpeechNotification("Reinforce")
end

BridgeheadSecured = function()
	ReinforceFromSea(MobileConstructionVehicle)
	OpenRA.RunAfterDelay(25 * 15, NodAttack)
	OpenRA.RunAfterDelay(25 * 30, function() ReinforceFromSea(EngineerReinforcements) end)
	OpenRA.RunAfterDelay(25 * 60, function() ReinforceFromSea(VehicleReinforcements) end)
end

NodAttack = function()
	local nodUnits = Mission.GetGroundAttackersOf(enemy)
	if #nodUnits > AttackerSquadSize * 2 then
		attackers = Utils.Skip(nodUnits, #nodUnits - AttackerSquadSize)
		local attackSquad = Team.New(attackers)
		Team.Do(attackSquad, function(unit)
			Actor.AttackMove(unit, waypoint2.location)
			Actor.Hunt(unit)
		end)
		Team.AddEventHandler(attackSquad.OnAllKilled, OpenRA.RunAfterDelay(25 * 15, NodAttack))
	end
end

WorldLoaded = function()
	player = OpenRA.GetPlayer("GDI")
	enemy = OpenRA.GetPlayer("Nod")

	Media.PlayMovieFullscreen("gdi2.vqa")

	nodInBaseTeam = Team.New({ RushBuggy, RushRifle1, RushRifle2, RushRifle3 })
	Team.AddEventHandler(nodInBaseTeam.OnAllKilled, BridgeheadSecured)
end

Tick = function()
	if Mission.RequiredUnitsAreDestroyed(player) then
		MissionFailed()
	end
	if Mission.RequiredUnitsAreDestroyed(enemy) then
		MissionAccomplished()
	end
end