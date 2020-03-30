--[[
   Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
CommandoReinforcements = { "rmbo" }
MCVReinforcements = { "mcv" }

inf1 = { "e4" }

AutocreateSquads =
{
	{ "stnk", "stnk" },
	{ "ftnk", "ftnk" },
	{ "ltnk", "ltnk", "bike" },
	{ "arty", "arty", "bike", "bike" },
	{ "ltnk", "ltnk" },
	{ "stnk", "stnk" },
	{ "ltnk", "ltnk" },
	{ "arty", "arty" }
}

HeliPatrolPaths =
{
	{ HeliPatrol1.Location, HeliPatrol2.Location, HeliPatrol3.Location, HeliPatrol4.Location, HeliPatrol5.Location, HeliPatrol6.Location },
	{ HeliPatrol5.Location, HeliPatrol4.Location, HeliPatrol3.Location, HeliPatrol2.Location, HeliPatrol1.Location, HeliPatrol6.Location }
}

AttackTriggers = { AttackTrigger1, AttackTrigger2, AttackTrigger3, AttackTrigger4 }

harvester = { "harv" }

SamSites = { SAM01, SAM02 }

WorldLoaded = function()
	player = Player.GetPlayer("GDI")
	enemy = Player.GetPlayer("Nod")

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

	destroySAMsCenterObjective = player.AddPrimaryObjective("Destroy the SAM sites protecting the Obelisk.")
	destroyObeliskObjective = player.AddPrimaryObjective("Destroy the Obelisk.")
	destroyBiotechCenterObjective = player.AddPrimaryObjective("Destroy the biotech facility.")

	Trigger.OnAllKilled(SamSites, function()
		AirSupport = Actor.Create("airstrike.proxy", true, { Owner = player })
		AirSupportEnabled = true
		player.MarkCompletedObjective(destroySAMsCenterObjective)
	end)

	Trigger.OnDamaged(Obelisk01, function()
		Trigger.AfterDelay(DateTime.Seconds(1), Obelisk01.Kill)
	end)

	Trigger.OnKilled(Obelisk01, function()
		player.MarkCompletedObjective(destroyObeliskObjective)
		Trigger.AfterDelay(DateTime.Seconds(5), function() Reinforce(MCVReinforcements) end)
		ObeliskFlare.Destroy()
		if AirSupportEnabled then AirSupport.Destroy() end
	end)

	Trigger.OnKilled(Biolab, function()
		player.MarkCompletedObjective(destroyBiotechCenterObjective)
	end)

	Trigger.OnCapture(Biolab, function()
		Biolab.Kill()
	end)

	Trigger.OnDamaged(Biolab, HuntTriggerFunction)

	AIRepairBuildings(enemy)
	AIRebuildHarvesters(enemy)

	Utils.Do(AttackTriggers, function(a)
		Trigger.OnKilledOrCaptured(a, function()
			NodVehicleProduction(Utils.Random(AutocreateSquads))
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(150), AutoCreateTeam)
	Trigger.AfterDelay(DateTime.Minutes(5), HeliHunt)

	NodInfantryProduction()

	Camera.Position = UnitsRally.CenterPosition
	ObeliskFlare = Actor.Create('flare', true, { Owner = player, Location = Flare.Location })
	Reinforce(CommandoReinforcements)
end

Tick = function()
	if DateTime.GameTime > DateTime.Seconds(5) and player.HasNoRequiredUnits() then
		player.MarkFailedObjective(destroyBiotechCenterObjective)
	end
end

Reinforce = function(units)
	Media.PlaySpeechNotification(player, "Reinforce")
	ReinforceWithLandingCraft(units, lstStart.Location, lstEnd.Location, UnitsRally.Location)
end

ReinforceWithLandingCraft = function(units, transportStart, transportUnload, rallypoint)
	local transport = Actor.Create("oldlst", true, { Owner = player, Facing = 0, Location = transportStart })
	local subcell = 0
	Utils.Do(units, function(a)
		transport.LoadPassenger(Actor.Create(a, false, { Owner = transport.Owner, Facing = transport.Facing, Location = transportUnload, SubCell = subcell }))
		subcell = subcell + 1
	end)

	transport.ScriptedMove(transportUnload)

	transport.CallFunc(function()
		Utils.Do(units, function()
			local a = transport.UnloadPassenger()
			a.IsInWorld = true
			a.MoveIntoWorld(transport.Location - CVec.New(0, 1))

			if rallypoint ~= nil then
				a.Move(rallypoint)
			end
		end)
	end)

	transport.Wait(5)
	transport.ScriptedMove(transportStart)
	transport.Destroy()
end

HuntTriggerFunction = function()
	local list = enemy.GetGroundAttackers()
	Utils.Do(list, function(unit)
		IdleHunt(unit)
	end)
end

IdleHunt = function(unit)
	if not unit.IsDead then
		Trigger.OnIdle(unit, function()
			unit.AttackMove(UnitsRally.Location, 50)
			unit.Hunt()
		end)
	end
end

NodInfantryProduction = function()
	if HandOfNod.IsDead or HandOfNod.Owner == player then
		return
	end
	HandOfNod.Build(inf1, SquadHunt)
	Trigger.AfterDelay(DateTime.Seconds(15), NodInfantryProduction)
end

NodVehicleProduction = function(Squad)
	if Airfield.IsDead or not Airfield.Owner == enemy then
		return
	end
	Airfield.Build(Squad, SquadHunt)
end

AIRepairBuildings = function(ai)
	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == ai and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == ai and building.Health < 0.9 * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)
end

HeliHunt = function()
	local helicopters = enemy.GetActorsByType("heli")
	local patrolpath = Utils.Random(HeliPatrolPaths)

	Utils.Do(helicopters, function(actor)
		Trigger.OnIdle(actor, function()
			actor.Patrol(patrolpath)
		end)
	end)
end

SquadHunt = function(actors)
	Utils.Do(actors, function(actor)
		Trigger.OnIdle(actor, function()
			actor.AttackMove(UnitsRally.Location, 50)
			actor.Hunt()
		end)
	end)
end

AIRebuildHarvesters = function(ai)
	if AIHarvesterCount == NIL or AIHarvesterCount == 0 then
		AIHarvesterCount = #ai.GetActorsByType("harv")
		IsBuildingHarvester = false
	end

	local CurrentHarvesterCount = #ai.GetActorsByType("harv")

	if CurrentHarvesterCount < AIHarvesterCount and Airfield.Owner == enemy and not IsBuildingHarvester and not Airfield.IsDead then
		IsBuildingHarvester = true
		Airfield.Build(harvester, function()
			IsBuildingHarvester = false
		end)
	end
	Trigger.AfterDelay(DateTime.Seconds(5), function() AIRebuildHarvesters(ai) end)
end

AutoCreateTeam = function()
	NodVehicleProduction(Utils.Random(AutocreateSquads))
	Trigger.AfterDelay(DateTime.Seconds(150), AutoCreateTeam)
end
