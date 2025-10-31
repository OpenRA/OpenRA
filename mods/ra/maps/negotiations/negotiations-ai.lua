--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
BuildIntervals =
{
	easy = DateTime.Seconds(25),
	normal = DateTime.Seconds(15),
	hard = DateTime.Seconds(10)
}
StructureRebuildGrace = BuildIntervals[Difficulty]

InfantryTeams =
{
	{
		types = { "e1", "e1" }
	},
	{
		types = { "e2", "e2" }
	},
	{
		types = { "e4", "e4" },
		requirements = { "ftur" }
	},
	{
		types = { "e1", "e1", "e2", "e2" },
		onBuilt = AttackFromChurch
	},
	{
		types = { "shok", "shok" },
		requirements = { "ai.hard", "techcenter", "tsla" }
	}
}
VehicleTeams =
{
	{
		types = { "3tnk" },
		requirements = { "fix" }
	},
	{
		types = { "ttnk", "3tnk" },
		requirements = { "techcenter", "fix", "tsla" }
	}
}
HarvesterTeam =
{
	types = { "harv" },
	buildTime = Actor.BuildTime("harv"),
	onBuilt = function(actors)
		Utils.Do(actors, function(actor)
			actor.FindResources()
		end)
	end
}

PrepareBadGuy = function()
	BadGuy.Cash = 100000
	if Difficulty == "hard" then
		SpawnMiscActor("ai.hard", BadGuy)
	end
	PrepareTeamBuildTimes(InfantryTeams)
	PrepareTeamBuildTimes(VehicleTeams)

	SetBotStructures(
		{ type = "powr", exists = false, shape = "2x3", location = CPos.New(40, 51) },
		{ type = "proc", exists = false, shape = "3x4", location = CPos.New(33, 52) },
		{ type = "barr", exists = false, shape = "2x3", location = CPos.New(40, 55), onBuilt = BuildInfantry, onKilled = CheckNewBase, rebuildSkipped = true },
		{ type = "powr", exists = false, shape = "2x3", location = CPos.New(30, 50) },
		{ type = "ftur", exists = false, shape = "1x1", location = CPos.New(39, 58), onKilled = CheckNewBase },
		{ type = "ftur", exists = false, shape = "1x1", location = CPos.New(43, 57), onKilled = CheckNewBase },
		{ type = "powr", exists = false, shape = "2x3", location = CPos.New(30, 53) },
		{ type = "weap", exists = false, shape = "3x3", location = CPos.New(33, 56), onBuilt = BuildVehicles, onKilled = CheckNewBase, rebuildSkipped = true },
		-- Tesla coil is originally built before factory.
		-- For convention, coil is swapped (and service depot added).
		{ type = "tsla", exists = false, shape = "1x1", location = CPos.New(37, 57), onKilled = CheckNewBase },
		{ type = "fix", exists = false, shape = "3x3", location = CPos.New(30, 56) }
	)

	Trigger.OnEnteredFootprint( { BuilderRally.Location }, function(actor, id)
		if actor.Type ~= "fact" then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		ActivateBot()
		Trigger.OnKilled(actor, CheckNewBase)
	end)
end

CheckNewBase = function()
	if IsNewBaseOkay() then
		return
	end
	PrepareTechSale()

	if Difficulty == "hard" then
		StartFireSale(BadGuy)
	end
end

IsNewBaseOkay = function()
	local types = { "fact", "barr", "ftur", "weap", "tsla" }

	return Utils.Any(types, function(type)
		return BadGuy.HasPrerequisites({ type })
	end)
end

PrepareTechSale = function()
	if ForwardTech.IsDead or ForwardTech.Owner == USSR then
		return
	end

	ForwardTech.Owner = USSR
	if ForwardCommand.IsDead then
		StartFireSale(USSR)
	end
end

ActivateBot = function()
	CheckStructures(BadGuy, DateTime.Seconds(5))
	BadGuy.GrantCondition("ai-enabled")
end

PrepareTeamBuildTimes = function(teamCollection)
	Utils.Do(teamCollection, function(team)
		local time = 0

		Utils.Do(team.types, function(type)
			time = time + Actor.BuildTime(type)
		end)

		team.buildTime = time
	end)
end

SelectTeam = function(teamCollection)
	local teams = Utils.Shuffle(teamCollection)

	for i = 1, #teams do
		local team = teams[i]
		if AreTeamRequirementsMet(team.requirements) then
			return team
		end
	end

	return { }
end

AreTeamRequirementsMet = function(requirements)
	if not requirements then
		return true
	end

	return BadGuy.HasPrerequisites(requirements)
end

BuildTeam = function(player, team, nextBuild)
	if not team.types then
		-- No teams had their prerequisites.
		Trigger.AfterDelay(BuildIntervals[Difficulty], nextBuild)
		return
	end

	local onBuilt = team.onBuilt or GroupIdleHunt
	player.Build(team.types, onBuilt)
	Trigger.AfterDelay(team.buildTime, nextBuild)
end

BuildInfantry = function()
	if #BadGuy.GetActorsByType("barr") < 1 then
		return
	end

	local team = SelectTeam(InfantryTeams)

	BuildTeam(BadGuy, team, function()
		Trigger.AfterDelay(BuildIntervals[Difficulty], BuildInfantry)
	end)
end

BuildVehicles = function()
	if #BadGuy.GetActorsByType("weap") < 1 then
		return
	end

	local team = nil
	if IsHarvesterNeeded(BadGuy) then
		team = HarvesterTeam
	end
	team = team or SelectTeam(VehicleTeams)

	BuildTeam(BadGuy, team, function()
		Trigger.AfterDelay(BuildIntervals[Difficulty], BuildVehicles)
	end)
end

IsHarvesterNeeded = function(player)
	return player.HasPrerequisites({ "proc" }) and #player.GetActorsByType("harv") < 1
end

AttackFromChurch = function(actors)
	local path =
	{
		BadGuyRally.Location,
		ForestPatrolStart.Location,
		GuideHouseReveal.Location,
		ChurchRally.Location,
		GuideHouseReveal.Location
	}
	GroupTightPatrol(actors, path, false, 0, GroupIdleHunt)
end

--[[
	These kind of work like the original [BASE] and [STRUCTURES] .INI sections.
	Check a list every so often and (re)build structures that are missing,
	in order, and if circumstances allow for it.
]]
SetBotStructures = function(...)
	local structures = { ... }

	Utils.Do(structures, function(structure)
		structure.cost = Actor.Cost(structure.type)
		structure.buildTime = Actor.BuildTime(structure.type)
		structure.position = WPos.New(structure.location.X * 1024, structure.location.Y * 1024, 0)
	end)

	BadGuyStructures = structures
end

CheckStructures = function(player, interval, rushed)
	if #player.GetActorsByType("fact") < 1 then
		return
	end

	local buildingScheduled = false

	for _, structure in pairs(BadGuyStructures) do
		if not structure.exists then
			buildingScheduled = true
			ScheduleStructure(player, structure, rushed or structure.buildTime, interval)
			break
		end
	end

	if buildingScheduled then
		return
	end

	Trigger.AfterDelay(interval, function()
		CheckStructures(player, interval)
	end)
end

ScheduleStructure = function(player, structure, buildTime, interval)
	Trigger.AfterDelay(buildTime, function()
		if #player.GetActorsByType("fact") < 1 then
			return
		end

		local success, blocked = BuildStructure(player, structure)
		if not success and blocked then
			CheckStructures(player, interval, DateTime.Seconds(2))
			return
		end

		Trigger.AfterDelay(interval, function()
			CheckStructures(player, interval)
		end)
	end)
end

AddRebuildTrigger = function(actor, structure)
	if structure.rebuildSkipped then
		return
	end

	Trigger.OnRemovedFromWorld(actor, function()
		-- Period before the bot can consider replacement.
		Trigger.AfterDelay(StructureRebuildGrace, function()
			structure.exists = false
		end)
	end)
end

BuildStructure = function(player, structure)
	if structure.cost > player.Cash then
		return false
	end

	local blocked = IsStructureAreaBlocked(player, structure.position, structure.shape)
	if blocked then
		return false, "blocked"
	end

	local actor = Actor.Create(structure.type, true, { Owner = player, Location = structure.location })
	structure.exists = true
	player.Cash = player.Cash - structure.cost
	AddRebuildTrigger(actor, structure)

	if structure.onKilled then
		Trigger.OnKilled(actor, structure.onKilled)
	end

	if structure.onBuilt then
		-- Build() will not work properly on producers if called immediately.
		Trigger.AfterDelay(1, function()
			structure.onBuilt(actor)
		end)
	end

	return actor
end

StructureFootprints =
{
	["1x1"] = WVec.New(1 * 1024, 1 * 1024, 0),
	["2x3"] = WVec.New(2 * 1024, 3 * 1024, 0),
	["3x3"] = WVec.New(3 * 1024, 3 * 1024, 0),
	["3x4"] = WVec.New(3 * 1024, 4 * 1024, 0),
}

IsStructureAreaBlocked = function(player, position, shape)
	local foot = StructureFootprints[shape]
	local blockers = Map.ActorsInBox(position, position + foot, function(actor)
		return actor.CenterPosition.Z == 0 and actor.HasProperty("Health")
	end)

	if #blockers == 0 then
		return false
	end

	ScatterBlockers(player, blockers)
	return true
end

ScatterBlockers = function(player, actors)
	Utils.Do(actors, function(actor)
		if actor.IsIdle and actor.Owner == player and actor.HasProperty("Scatter") then
			actor.Scatter()
		end
	end)
end
