--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
Yaks = { "yak", "yak", "yak" }
Airfields = { Airfield1, Airfield2, Airfield3 }

InsertYaks = function()
	local i = 1
	Utils.Do(Yaks, function(yakType)
		local start = YakEntry.CenterPosition + WVec.New(0, (i - 1) * 1536, Actor.CruiseAltitude(yakType))
		local dest = StartJeep.Location + CVec.New(0, 2 * i)
		local yak = Actor.Create(yakType, true, { CenterPosition = start, Owner = player, Facing = (Map.CenterOfCell(dest) - start).Facing })
		yak.Move(dest)
		yak.ReturnToBase(Airfields[i])
		i = i + 1
	end)
end

JeepDemolishingBridge = function()
	StartJeep.Move(StartJeepMovePoint.Location)

	Trigger.OnEnteredFootprint({ StartJeepMovePoint.Location }, function(actor, id)
		if actor.Owner == france and not BridgeBarrel.IsDead then
			Trigger.RemoveFootprintTrigger(id)
			BridgeBarrel.Kill()
		end

		local bridge = Map.ActorsInBox(BridgeWaypoint.CenterPosition, Airfield1.CenterPosition,
			function(self) return self.Type == "bridge1" end)[1]

		if not bridge.IsDead then
			bridge.Kill()
		end
	end)
end

Paratroopers = function()
	Trigger.OnKilled(StartJeep, function()
		Media.PlaySpeechNotification(player, "ReinforcementsArrived")
		Paradrop.TargetParatroopers(StartJeepMovePoint.CenterPosition, Angle.East)
	end)

	Trigger.OnKilled(Church, function()
		Media.PlaySpeechNotification(player, "ReinforcementsArrived")
		Paradrop.TargetParatroopers(StartJeepMovePoint.CenterPosition, Angle.East)
	end)

	Trigger.OnKilled(ParaHut, function()
		Media.PlaySpeechNotification(player, "ReinforcementsArrived")
		Paradrop.TargetParatroopers(StartJeepMovePoint.CenterPosition, Angle.East)
	end)
end

PanicAttack = function()
	if not HouseDamaged then
		local panicTeam = Reinforcements.Reinforce(france, { "c3", "c6", "c9" }, { CivSpawn.Location }, 0)
		Utils.Do(panicTeam, function(a)
			a.Move(a.Location + CVec.New(-1,-1))
			a.Panic()
		end)
	end
	HouseDamaged = true
end

WorldLoaded = function()
	player = Player.GetPlayer("USSR")
	france = Player.GetPlayer("France")
	germany = Player.GetPlayer("Germany")

	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	VillageRaidObjective = player.AddObjective("Raze the village.")

	Trigger.OnAllRemovedFromWorld(Airfields, function()
		player.MarkFailedObjective(VillageRaidObjective)
	end)

	JeepDemolishingBridge()

	Trigger.OnPlayerWon(player, function()
		Media.PlaySpeechNotification(player, "MissionAccomplished")
	end)

	Trigger.OnPlayerLost(player, function()
		Media.PlaySpeechNotification(player, "MissionFailed")
	end)

	Paradrop = Actor.Create("powerproxy.paratroopers", false, { Owner = player })
	Trigger.AfterDelay(DateTime.Seconds(2), InsertYaks)
	Paratroopers()
	Trigger.OnDamaged(HayHouse, PanicAttack)
	Trigger.OnKilled(PillboxBarrel1, function()
		if not Pillbox1.IsDead then
			Pillbox1.Kill()
		end
	end)
	Trigger.OnKilled(PillboxBarrel2, function()
		if not Pillbox2.IsDead then
			Pillbox2.Kill()
		end
	end)
end

Tick = function()
	if france.HasNoRequiredUnits() and germany.HasNoRequiredUnits() then
		player.MarkCompletedObjective(VillageRaidObjective)
	end
end
