--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

CommandoReinforcements = { "rmbo" }
MCVReinforcements = { "mcv" }

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

SamSites = { SAM01, SAM02 }

WorldLoaded = function()
	GDI = Player.GetPlayer("GDI")
	Nod = Player.GetPlayer("Nod")

	InitObjectives(GDI)

	DestroySAMs = AddPrimaryObjective(GDI, "destroy-obelisk-sams")
	DestroyObelisk = AddPrimaryObjective(GDI, "destroy-obelisk")
	DestroyBiotechCenter = AddPrimaryObjective(GDI, "destroy-biotech")

	Trigger.OnAllKilled(SamSites, function()
		AirSupport = Actor.Create("airstrike.proxy", true, { Owner = GDI })
		AirSupportEnabled = true
		GDI.MarkCompletedObjective(DestroySAMs)
	end)

	Trigger.OnDamaged(Obelisk01, function()
		Trigger.AfterDelay(DateTime.Seconds(1), Obelisk01.Kill)
	end)

	Trigger.OnKilled(Obelisk01, function()
		GDI.MarkCompletedObjective(DestroyObelisk)

		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.PlaySpeechNotification(GDI, "Reinforce")
			ReinforceWithLandingCraft(GDI, MCVReinforcements, lstStart.Location, lstEnd.Location, UnitsRally.Location)
		end)

		ObeliskFlare.Destroy()
		if AirSupportEnabled then
			AirSupport.Destroy()
		end
	end)

	Trigger.OnKilled(Biolab, function()
		GDI.MarkCompletedObjective(DestroyBiotechCenter)
	end)

	Trigger.OnCapture(Biolab, function()
		Trigger.AfterDelay(DateTime.Seconds(1), Biolab.Kill)
	end)

	Trigger.OnDamaged(Biolab, function()
		Utils.Do(Nod.GetGroundAttackers(), IdleHunt)
	end)

	RepairNamedActors(Nod, 0.9)

	Trigger.AfterDelay(0, function()
		local toBuild = function() return { "harv" } end
		Utils.Do(Nod.GetActorsByType("harv"), function(harv)
			RebuildHarvesters(harv, toBuild)
		end)
	end)

	local vehicleToBuild = function() return Utils.Random(AutocreateSquads) end
	Utils.Do(AttackTriggers, function(a)
		Trigger.OnKilledOrCaptured(a, function()
			ProduceUnits(Nod, Airfield, nil, vehicleToBuild)
		end)
	end)
	Trigger.AfterDelay(DateTime.Seconds(150), function()
		ProduceUnits(Nod, Airfield, function() return DateTime.Seconds(150) end, vehicleToBuild)
	end)

	Trigger.AfterDelay(DateTime.Minutes(5), HeliHunt)

	local toBuild = function() return { "e4" } end
	local delay = function() return DateTime.Seconds(15) end
	ProduceUnits(Nod, HandOfNod, delay, toBuild)

	Camera.Position = UnitsRally.CenterPosition
	ObeliskFlare = Actor.Create("flare", true, { Owner = GDI, Location = Flare.Location })

	Media.PlaySpeechNotification(GDI, "Reinforce")
	ReinforceWithLandingCraft(GDI, CommandoReinforcements, lstStart.Location, lstEnd.Location, UnitsRally.Location)
end

Tick = function()
	if DateTime.GameTime > DateTime.Seconds(5) and GDI.HasNoRequiredUnits() then
		GDI.MarkFailedObjective(DestroyBiotechCenter)
	end
end

-- Overwrite the default to send the units to UnitsRally first
IdleHunt = function(unit)
	if not unit.IsDead then
		Trigger.OnIdle(unit, function()
			unit.AttackMove(UnitsRally.Location, 50)
			unit.Hunt()
		end)
	end
end

HeliHunt = function()
	local patrolpath = Utils.Random(HeliPatrolPaths)
	Utils.Do(Nod.GetActorsByType("heli"), function(actor)
		Trigger.OnIdle(actor, function()
			actor.Patrol(patrolpath)
		end)
	end)
end

RebuildHarvesters = function(harv, toBuild)
	Trigger.OnRemovedFromWorld(harv, function()
		ProduceUnits(Nod, Airfield, nil, toBuild, function(units)
			RebuildHarvesters(units[1], toBuild)
		end)
	end)
end
