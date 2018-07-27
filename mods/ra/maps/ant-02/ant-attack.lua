CanAttackFromSouth = true
CanAttackFromIsland = true
DifficultySetting = Map.LobbyOption("difficulty")

StrikeForceList = { "warriorant", "warriorant", "warriorant", "warriorant", "warriorant", "fireant", "fireant", "fireant", "fireant" }

AntLists = {
	easy = {
		"fireant", "fireant",
		"scoutant", "scoutant",
		"warriorant", "warriorant", "warriorant",
	},

	normal = {
		"fireant", "fireant", "fireant",
		"scoutant", "scoutant", "scoutant", "scoutant" ,
		"warriorant", "warriorant", "warriorant", "warriorant", "warriorant" 
	},

	hard = {
		"fireant", "fireant", "fireant", "fireant", "fireant",
		"scoutant", "scoutant", "scoutant", "scoutant", "scoutant", 
		"warriorant", "warriorant", "warriorant", "warriorant", "warriorant", "warriorant", "warriorant", "warriorant" 
	}
}

AttackDirection = { "north", "middle", "south" }

Pathing = {
	north = {
		{ waypoint0.Location, waypoint12.Location }, 
		waypoint18.Location
	},

	middle = {
		{ waypoint1.Location, waypoint11.Location },
		waypoint35.Location
	},

	south = {
		{ waypoint2.Location, waypoint95.Location }, 
		waypoint20.Location
	}
}

StartAttack = function()
	local direction = Utils.Random(AttackDirection)
	local shouldLaunchAttack = true

	if direction ~= "south" and not CanAttackFromIsland then
		shouldLaunchAttack = false
	elseif not CanAttackFromSouth then
		shouldLaunchAttack = false
	end

	if shouldLaunchAttack then
		local pathAndPoint = Pathing[direction]
		Reinforcements.Reinforce(PatrolAnts, AntLists[DifficultySetting], pathAndPoint[1], DateTime.Seconds(5), function(ant)
			ant.AttackMove(pathAndPoint[2])
			Trigger.OnIdle(ant, function(actor)
				actor.Hunt()
			end)
		end)
	end
end

StopIslandAttacks = function()
	CanAttackFromIsland = false
end

StopSouthernAttacks = function()
	CanAttackFromSouth = false
end

StartNorthAttack = function()
	if CanAttackFromNorth then
		local attackPath = { waypoint0.Location, waypoint12.Location }
		return SpawnVillageAttack(attackPath, Actor192.Location)
	end
end

StartSouthAttack = function()
	if CanAttackFromSouth then
		local attackPath = { waypoint2.Location, waypoint8.Location }
		return SpawnVillageAttack(attackPath, Church.Location)
	end
end

SpawnVillageAttack  = function(attackPath, attackPoint) 
	return Reinforcements.Reinforce(AttackAnts, StrikeForceList, attackPath, DateTime.Seconds(5), function(ant)
		Trigger.OnIdle(ant, function(actor)
			actor.Hunt()
		end)
	end)

end

InitAntPlayers = function()
	PatrolAnts = Player.GetPlayer("PatrolAnts")
	AttackAnts = Player.GetPlayer("AttackAnts")
end
