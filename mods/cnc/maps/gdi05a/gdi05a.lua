RepairThreshold = { Easy = 0.3, Normal = 0.6, Hard = 0.9 }

ActorRemovals =
{
	Easy = { Actor167, Actor168, Actor190, Actor191, Actor193, Actor194, Actor196, Actor198, Actor200 },
	Normal = { Actor167, Actor194, Actor196, Actor197 },
	Hard = { },
}

GdiTanks = { "mtnk", "mtnk" }
GdiApc = { "apc" }
GdiInfantry = { "e1", "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e2", "e2" }
GdiBase = { GdiNuke1, GdiNuke2, GdiProc, GdiSilo1, GdiSilo2, GdiPyle, GdiWeap, GdiHarv }
NodSams = { Sam1, Sam2, Sam3, Sam4 }
CoreNodBase = { NodConYard, NodRefinery, HandOfNod, Airfield }

Grd1UnitTypes = { "bggy" }
Grd1Path = { waypoint4.Location, waypoint5.Location, waypoint10.Location }
Grd1Delay = { Easy = DateTime.Minutes(2), Normal = DateTime.Minutes(1), Hard = DateTime.Seconds(30) }
Grd2UnitTypes = { "bggy" }
Grd2Path = { waypoint0.Location, waypoint1.Location, waypoint2.Location }
Grd3Units = { GuardTank1, GuardTank2 }
Grd3Path = { waypoint4.Location, waypoint5.Location, waypoint9.Location }

AttackDelayMin = { Easy = DateTime.Minutes(1), Normal = DateTime.Seconds(45), Hard = DateTime.Seconds(30) }
AttackDelayMax = { Easy = DateTime.Minutes(2), Normal = DateTime.Seconds(90), Hard = DateTime.Minutes(1) }
AttackUnitTypes =
{
	Easy =
	{
		{ HandOfNod, { "e1", "e1" } },
		{ HandOfNod, { "e1", "e3" } },
		{ HandOfNod, { "e1", "e1", "e3" } },
		{ HandOfNod, { "e1", "e3", "e3" } },
	},
	Normal =
	{
		{ HandOfNod, { "e1", "e1", "e3" } },
		{ HandOfNod, { "e1", "e3", "e3" } },
		{ HandOfNod, { "e1", "e1", "e3", "e3" } },
		{ Airfield, { "bggy" } },
	},
	Hard =
	{
		{ HandOfNod, { "e1", "e1", "e3", "e3" } },
		{ HandOfNod, { "e1", "e1", "e1", "e3", "e3" } },
		{ HandOfNod, { "e1", "e1", "e3", "e3", "e3" } },
		{ Airfield, { "bggy" } },
		{ Airfield, { "ltnk" } },
	}
}
AttackPaths =
{
	{ waypoint0.Location, waypoint1.Location, waypoint2.Location, waypoint3.Location },
	{ waypoint4.Location, waypoint9.Location, waypoint7.Location, waypoint8.Location },
}

Build = function(factory, units, action)
	if factory.IsDead or factory.Owner ~= nod then
		return
	end

	if not factory.Build(units, action) then
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Build(factory, units, action)
		end)
	end
end

Attack = function()
	local types = Utils.Random(AttackUnitTypes[Map.Difficulty])
	local path = Utils.Random(AttackPaths)
	Build(types[1], types[2], function(units)
		Utils.Do(units, function(unit)
			if unit.Owner ~= nod then return end
			unit.Patrol(path, false)
			Trigger.OnIdle(unit, unit.Hunt)
		end)
	end)

	Trigger.AfterDelay(Utils.RandomInteger(AttackDelayMin[Map.Difficulty], AttackDelayMax[Map.Difficulty]), Attack)
end

Grd1Action = function()
	Build(Airfield, Grd1UnitTypes, function(units)
		Utils.Do(units, function(unit)
			if unit.Owner ~= nod then return end
			Trigger.OnKilled(unit, function()
				Trigger.AfterDelay(Grd1Delay[Map.Difficulty], Grd1Action)
			end)
			unit.Patrol(Grd1Path, true, DateTime.Seconds(7))
		end)
	end)
end

Grd2Action = function()
	Build(Airfield, Grd2UnitTypes, function(units)
		Utils.Do(units, function(unit)
			if unit.Owner ~= nod then return end
			unit.Patrol(Grd2Path, true, DateTime.Seconds(5))
		end)
	end)
end

Grd3Action = function()
	local unit
	for i, u in ipairs(Grd3Units) do
		if not u.IsDead then
			unit = u
			break
		end
	end

	if unit ~= nil then
		Trigger.OnKilled(unit, function()
			Grd3Action()
		end)

		unit.Patrol(Grd3Path, true, DateTime.Seconds(11))
	end
end

DiscoverGdiBase = function(actor, discoverer)
	if baseDiscovered or not discoverer == gdi then
		return
	end

	Utils.Do(GdiBase, function(actor)
		actor.Owner = gdi
	end)
	GdiHarv.FindResources()

	baseDiscovered = true

	gdiObjective3 = gdi.AddPrimaryObjective("Eliminate all Nod forces in the area")
	gdi.MarkCompletedObjective(gdiObjective1)
	
	Attack()
end

SetupWorld = function()
	Utils.Do(ActorRemovals[Map.Difficulty], function(unit)
		unit.Destroy()
	end)

	Reinforcements.Reinforce(gdi, GdiTanks, { GdiTankEntry.Location, GdiTankRallyPoint.Location }, DateTime.Seconds(1), function(actor) actor.Stance = "Defend" end)
	Reinforcements.Reinforce(gdi, GdiApc, { GdiApcEntry.Location, GdiApcRallyPoint.Location }, DateTime.Seconds(1), function(actor) actor.Stance = "Defend" end)
	Reinforcements.Reinforce(gdi, GdiInfantry, { GdiInfantryEntry.Location, GdiInfantryRallyPoint.Location }, 15, function(actor) actor.Stance = "Defend" end)

	Trigger.OnPlayerDiscovered(gdiBase, DiscoverGdiBase)

	Utils.Do(Map.NamedActors, function(actor)
		if actor.Owner == nod and actor.HasProperty("StartBuildingRepairs") then
			Trigger.OnDamaged(actor, function(building)
				if building.Owner == nod and building.Health < RepairThreshold[Map.Difficulty] * building.MaxHealth then
					building.StartBuildingRepairs()
				end
			end)
		end
	end)

	Trigger.OnAllKilled(NodSams, function()
		gdi.MarkCompletedObjective(gdiObjective2)
		Actor.Create("airstrike.proxy", true, { Owner = gdi })
	end)

	GdiHarv.Stop()
	NodHarv.FindResources()
	if Map.Difficulty ~= "Easy" then
		Trigger.OnDamaged(NodHarv, function()
			Utils.Do(nod.GetGroundAttackers(), function(unit)
				unit.AttackMove(NodHarv.Location)
				if Map.Difficulty == "Hard" then
					unit.Hunt()
				end
			end)
		end)
	end

	Trigger.AfterDelay(DateTime.Seconds(45), Grd1Action)
	Trigger.AfterDelay(DateTime.Minutes(3), Grd2Action)
	Grd3Action()
end

WorldLoaded = function()
	gdiBase = Player.GetPlayer("AbandonedBase")
	gdi = Player.GetPlayer("GDI")
	nod = Player.GetPlayer("Nod")

	Trigger.OnObjectiveAdded(gdi, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)
	Trigger.OnObjectiveCompleted(gdi, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(gdi, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(gdi, function()
		Media.PlaySpeechNotification(player, "Lose")
	end)

	Trigger.OnPlayerWon(gdi, function()
		Media.PlaySpeechNotification(player, "Win")
	end)

	nodObjective = nod.AddPrimaryObjective("Destroy all GDI troops")
	gdiObjective1 = gdi.AddPrimaryObjective("Find the GDI base")
	gdiObjective2 = gdi.AddSecondaryObjective("Destroy all SAM sites to receive air support")

	SetupWorld()

	Camera.Position = GdiTankRallyPoint.CenterPosition

	Media.PlayMusic()
end

Tick = function()
	if gdi.HasNoRequiredUnits() then
		if DateTime.GameTime > 2 then
			nod.MarkCompletedObjective(nodObjective)
		end
	end
	if baseDiscovered and nod.HasNoRequiredUnits() then
		gdi.MarkCompletedObjective(gdiObjective3)
	end
end