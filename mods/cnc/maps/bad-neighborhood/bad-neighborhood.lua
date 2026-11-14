--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

-- Port of the original Nod Covert Operations mission "Bad Neighborhood"
-- (SCB20EA.INI). Nod starts with nothing but an MCV and has to build a base
-- while staying out of GDI's and the villagers' sight. The original street
-- fighting was driven by the house triggers `yyyy` and `zzzz` (both
-- "Autocreate", i.e. alert every house): walking into one of the two trip
-- areas north of the village alerts the whole GDI base. The `dely`/`delz`
-- areas disable those trips again, opening the two safe approaches.
--
-- The cell triggers used the Neutral house in the original, which means
-- only a civilian walking into them fired them. In the port every trip is
-- Nod-triggered instead: the villagers already expose the player through
-- the `civ1` trigger (they run north and report), so the footprint trips
-- are the player's own mistakes rather than random wandering.

-- Original [CellTriggers]
Civ2Cells = {
	CPos.New(30, 55), CPos.New(31, 55), CPos.New(31, 56), CPos.New(32, 55), CPos.New(32, 56), CPos.New(33, 56),
	CPos.New(45, 53), CPos.New(46, 52), CPos.New(46, 53), CPos.New(47, 52), CPos.New(47, 53), CPos.New(48, 52),
	CPos.New(59, 53), CPos.New(59, 54), CPos.New(60, 53), CPos.New(60, 54), CPos.New(61, 53), CPos.New(61, 54)
}

Ion1Cells = {
	CPos.New(16, 48), CPos.New(16, 49), CPos.New(16, 50), CPos.New(16, 51), CPos.New(17, 48), CPos.New(17, 49),
	CPos.New(17, 50), CPos.New(17, 51)
}

-- `yyyy`: the western approach and the diagonal in front of the GDI base.
YyyyCells = {
	CPos.New(29, 12), CPos.New(29, 13), CPos.New(29, 14), CPos.New(30, 12), CPos.New(30, 13), CPos.New(30, 14),
	CPos.New(33, 22), CPos.New(34, 22), CPos.New(34, 23), CPos.New(35, 22), CPos.New(35, 23), CPos.New(35, 24),
	CPos.New(36, 23), CPos.New(36, 24), CPos.New(36, 25), CPos.New(37, 24), CPos.New(37, 25), CPos.New(37, 26),
	CPos.New(38, 25), CPos.New(38, 26), CPos.New(39, 26), CPos.New(49, 26), CPos.New(49, 27), CPos.New(50, 26),
	CPos.New(50, 27), CPos.New(51, 26), CPos.New(51, 27), CPos.New(52, 26), CPos.New(52, 27), CPos.New(53, 26),
	CPos.New(53, 27), CPos.New(54, 26), CPos.New(54, 27), CPos.New(55, 26), CPos.New(55, 27), CPos.New(56, 26),
	CPos.New(56, 27), CPos.New(57, 26), CPos.New(57, 27), CPos.New(58, 26), CPos.New(58, 27)
}

-- `delz`: Nod entering here disables the `zzzz` trip (original `delz`).
DelzCells = {
	CPos.New(31, 12), CPos.New(31, 13), CPos.New(31, 14), CPos.New(32, 12), CPos.New(32, 13), CPos.New(32, 14),
	CPos.New(34, 20), CPos.New(35, 20), CPos.New(35, 21), CPos.New(36, 20), CPos.New(36, 21), CPos.New(36, 22),
	CPos.New(37, 20), CPos.New(37, 21), CPos.New(37, 22), CPos.New(37, 23), CPos.New(38, 21), CPos.New(38, 22),
	CPos.New(38, 23), CPos.New(38, 24), CPos.New(39, 22), CPos.New(39, 23), CPos.New(39, 24), CPos.New(40, 24),
	CPos.New(49, 24), CPos.New(49, 25), CPos.New(50, 24), CPos.New(50, 25), CPos.New(51, 24), CPos.New(51, 25),
	CPos.New(52, 24), CPos.New(52, 25), CPos.New(53, 24), CPos.New(53, 25), CPos.New(54, 24), CPos.New(54, 25),
	CPos.New(55, 24), CPos.New(55, 25), CPos.New(56, 24), CPos.New(56, 25), CPos.New(57, 24), CPos.New(57, 25),
	CPos.New(58, 24), CPos.New(58, 25)
}

-- `zzzz`: the second trip wire directly south of the GDI base.
ZzzzCells = {
	CPos.New(49, 20), CPos.New(49, 21), CPos.New(50, 20), CPos.New(50, 21), CPos.New(51, 20), CPos.New(51, 21),
	CPos.New(52, 20), CPos.New(52, 21), CPos.New(53, 20), CPos.New(53, 21), CPos.New(54, 20), CPos.New(54, 21),
	CPos.New(55, 20), CPos.New(55, 21)
}

-- `dzne`: a civilian entering here sets the drop-zone flare (original `dzne`,
-- Neutral/Player Enters). The late villager walks through it.
DzneCells = {
	CPos.New(18, 48), CPos.New(18, 49), CPos.New(18, 50), CPos.New(18, 51),
	CPos.New(19, 48), CPos.New(19, 49), CPos.New(19, 50), CPos.New(19, 51)
}

-- `dely`: entering here disables the `yyyy` trip (original `dely`).
DelyCells = {
	CPos.New(50, 18), CPos.New(50, 19), CPos.New(51, 18), CPos.New(51, 19), CPos.New(52, 18), CPos.New(52, 19),
	CPos.New(53, 18), CPos.New(53, 19), CPos.New(54, 18), CPos.New(54, 19)
}

-- `orc2`: Nod entering here calls in a second Orca flight.
Orc2Cells = {
	CPos.New(46, 5), CPos.New(46, 6), CPos.New(46, 7), CPos.New(46, 8), CPos.New(47, 5), CPos.New(47, 6),
	CPos.New(47, 7), CPos.New(47, 8)
}

-- `civ1` runs north to the GDI base and reports, `civ2` joins the fight.
RunForHelpCivilians = { Civilian5, Civilian6, Civilian8 }
VillageAttackCivilians = { Civilian3, Civilian4, Civilian7 }

-- `civ3` spawns one more villager in the original after 20 minutes, which
-- then walks towards the Nod base. It only marks the drop zone if it actually
-- discovers Nod; walking into the old dzne area is not enough. The spawn time
-- is the difficulty knob: easy 15 min, normal 10 min, hard 7 min.
Civ3Delay = { easy = 900, normal = 600, hard = 420 }

MissionFinished = false
CiviliansReported = false
VillageAlerted = false
DropZoneMarked = false
LateVillager = nil

WorldLoaded = function()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	Civilians = Player.GetPlayer("Civilians")

	Camera.Position = DefaultCameraPosition.CenterPosition

	InitObjectives(Nod)
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")

	-- Original [BadGuy] credits: 50 * 100. GDI starts with 20 * 100.
	Nod.Cash = 5000

	SetupGDI()
	SetupCivilians()
	SetupDetection()

	Trigger.AfterDelay(DateTime.Seconds(Civ3Delay[Difficulty or "normal"]), SpawnLateVillager)
end

Tick = function()
	if MissionFinished or DateTime.GameTime <= DateTime.Seconds(2) then
		return
	end

	-- `win`/`lose`: All Destroyed for GDI and Nod respectively.
	if GDI.HasNoRequiredUnits() then
		MissionFinished = true
		Nod.MarkCompletedObjective(EliminateGDI)
	elseif Nod.HasNoRequiredUnits() then
		MissionFinished = true
		Nod.MarkFailedObjective(EliminateGDI)
	end
end

SetupCivilians = function()
	Utils.Do({ Civilian1, Civilian2, Civilian3, Civilian4, Civilian5, Civilian6, Civilian7, Civilian8 },
		function(civilian)
			if not IsLive(civilian) then
				return
			end

			-- The armed villagers must not open fire on their own; they only
			-- join the fight after the civ1/civ2 triggers fire.
			if civilian.HasProperty("Stance") then
				civilian.Stance = "Defend"
			end

			Trigger.OnDiscovered(civilian, function(_, discoverer)
				if discoverer == Nod then
					CiviliansDiscovered()
				end
			end)

			Trigger.OnDamaged(civilian, function(self)
				if self.Owner == Civilians then
					CiviliansDiscovered()
				end
			end)
		end)
end

-- civ1: a villager has seen Nod. They run north towards the GDI base and
-- report, which is enough to alert GDI.
CiviliansDiscovered = function()
	if CiviliansReported or MissionFinished then
		return
	end

	CiviliansReported = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("civilians-alerted"))
	AlertGDI()

	Utils.Do(RunForHelpCivilians, function(civilian)
		if not IsLive(civilian) then
			return
		end

		-- Run for help: they only defend themselves while running north.
		if civilian.HasProperty("Stance") then
			civilian.Stance = "ReturnFire"
		end

		Utils.Do({ waypoint12, waypoint8, waypoint5, waypoint4, waypoint15 }, function(point)
			civilian.Move(point.Location)
		end)
	end)
end

-- civ2: Nod entered the GDI-friendly village. The armed villagers join the
-- fight and GDI is alerted.
CiviliansAttack = function()
	if VillageAlerted or MissionFinished then
		return
	end

	VillageAlerted = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("civilians-alerted"))
	AlertGDI()

	Utils.Do(VillageAttackCivilians, function(civilian)
		if not IsLive(civilian) then
			return
		end

		if civilian.HasProperty("Stance") then
			civilian.Stance = "AttackAnything"
		end

		Utils.Do({ waypoint13, waypoint9, waypoint14 }, function(point)
			civilian.AttackMove(point.Location)
		end)
		IdleHunt(civilian)
	end)
end

-- civ3: the original creates one more civilian team after 20 minutes. The
-- villager walks Waypoint 13 -> 9 -> 18. The drop-zone flare is set by the
-- original `dzne` trigger when a civilian enters the DzneCells (see below),
-- not by discovery and not by the Chinook.
SpawnLateVillager = function()
	if MissionFinished then
		return
	end

	LateVillager = Actor.Create("c2", true, { Owner = Civilians, Location = waypoint13.Location })
	-- Same Area Guard stance as the placed civilians: the bot default is
	-- AttackAnything, which would make it fight instead of walking.
	if LateVillager.HasProperty("Stance") then
		LateVillager.Stance = "Defend"
	end

	Trigger.OnDiscovered(LateVillager, function(_, discoverer)
		if discoverer == Nod then
			CiviliansDiscovered()
		end
	end)

	Trigger.OnDamaged(LateVillager, function(self)
		if self.Owner == Civilians then
			CiviliansDiscovered()
		end
	end)

	Trigger.AfterDelay(DateTime.Seconds(5), GuideLateVillager)
end

-- Re-issues the walk if the villager was interrupted. Arriving in DzneCells
-- sets the flare (see SetupDetection).
GuideLateVillager = function()
	if MissionFinished or not IsLive(LateVillager) then
		return
	end

	if DropZoneMarked then
		return
	end

	if LateVillager.IsIdle then
		LateVillager.Move(waypoint9.Location)
		LateVillager.Move(waypoint18.Location)
	end

	Trigger.AfterDelay(DateTime.Seconds(15), GuideLateVillager)
end

MarkDropZone = function()
	if DropZoneMarked or MissionFinished then
		return
	end

	DropZoneMarked = true
	Actor.Create("flare", true, { Owner = Neutral, Location = DefaultFlareLocation.Location })
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("drop-zone-marked"))
end

SetupDetection = function()
	YyyyTrigger = Trigger.OnEnteredFootprint(YyyyCells, function(actor, id)
		if actor.Owner ~= Nod then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		YyyyTrigger = nil
		AlertGDI()
	end)

	ZzzzTrigger = Trigger.OnEnteredFootprint(ZzzzCells, function(actor, id)
		if actor.Owner ~= Nod and actor.Owner ~= Civilians then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		ZzzzTrigger = nil
		AlertGDI()
	end)

	-- delz (original: Nod): Nod walked the hidden path, the southern trip is disarmed.
	Trigger.OnEnteredFootprint(DelzCells, function(actor, id)
		if actor.Owner ~= Nod then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		if ZzzzTrigger then
			Trigger.RemoveFootprintTrigger(ZzzzTrigger)
			ZzzzTrigger = nil
		end
	end)

	-- dely (original: Neutral): a villager or Nod disarms the western trip.
	Trigger.OnEnteredFootprint(DelyCells, function(actor, id)
		if actor.Owner ~= Nod and actor.Owner ~= Civilians then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		if YyyyTrigger then
			Trigger.RemoveFootprintTrigger(YyyyTrigger)
			YyyyTrigger = nil
		end
	end)

	-- dzne (original: Neutral): a civilian entering this area sets the
	-- drop-zone flare. The late villager walks through here on its way.
	DzneTrigger = Trigger.OnEnteredFootprint(DzneCells, function(actor, id)
		if actor.Owner ~= Civilians then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		MarkDropZone()
	end)

	-- ion1: the original fires this on the Neutral house, i.e. when a
	-- wandering villager walks into the area. Random villager wandering is
	-- not reproducible here, so Nod entering counts as well.
	Trigger.OnEnteredFootprint(Ion1Cells, function(actor, id)
		if actor.Owner ~= Nod and actor.Owner ~= Civilians then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		UnlockIonCannon()
	end)

	-- civ2: entering the village draws the armed villagers out.
	Trigger.OnEnteredFootprint(Civ2Cells, function(actor, id)
		if actor.Owner ~= Nod then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		CiviliansAttack()
	end)

	-- orc2: a second Orca flight is called in.
	Trigger.OnEnteredFootprint(Orc2Cells, function(actor, id)
		if actor.Owner ~= Nod then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		AlertGDI()
		SendOrcas()
	end)
end