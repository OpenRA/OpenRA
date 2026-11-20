--[[
   Hostile Takeover (SCB33EA) - mission script.

   The original is a Nod recovery mission. The player starts with a Flame
   Tank and four Rocket Infantry in the south-east, finds an abandoned Hand
   of Nod and Communications Center, sells the Communications Center to
   train an Engineer, captures one of the three empty GDI Chinooks in the
   cyclone pen and flies to the damaged Nod base in the north-west. The
   yellow flare at waypoint 25 marks that base.

   The three GDI Chinooks are `tran.capture`: only the empty, landed
   transport is capturable by an engineer, matching TRAN capture in TD.
   Everything else is fixed original data.

-- Shared with hostile-takeover-AI.lua: CurrentDifficulty, GDI, GDI2Cells,
-- GDI3Cells, GDI9Cells, GDIBuildingsHunt, GDIBuildingsLost, GDIChinooks,
-- GDIHome, GIContext, Grd1Cells, Grd2Cells, InitGDI, IsLive, MissionFinished,
-- Nod, OnNodBaseReclaimed, OnTransportCaptured.
]]

local AI = CampaignAI

-- Cross-file state stays global (see header).
MissionFinished = false
GDIChinooks = {}
GDIBuildingsLost = 0
GDIBuildingsHunt = false
GIContext = nil
GDIHome = nil

---@type boolean
local OutpostMarked = false
---@type boolean
local OutpostReached = false
---@type boolean
local OutpostCaptured = false
---@type boolean
local NodBaseReclaimed = false
---@type boolean
local TransportObjectiveDone = false
---@type boolean
local ChurchCrateSpawned = false

-- Delegating wrapper; the shared AI owns the IsInWorld-aware implementation.
IsLive = function(actor)
	return AI.IsLive(actor)
end

-- The original gives Nod 0 credits and expects the player to sell the
-- damaged Communications Center for its full half value. OpenRA scales
-- sell refunds by health, so the damaged HQ only yields ~50 $. Nod starts
-- with 500 $ instead, enough for the first engineer; the HQ sale is a
-- small bonus and captured GDI structures fund the rest.
local NodStartingCash = 500

local DifficultySettings = {
	easy = { gdiCash = 2000, prodDelay = 480, autoDelay = 600, waveInterval = 300, waveCount = 1, prodInterval = 45, maxProducedUnits = 5, assaultSize = 4, pressureSquadSize = 2,
		vehiclePool = { "jeep", "mtnk", "mtnk" }, prodReserve = 800, rebuildReserve = 0, baseAttackDelay = 240 },
	normal = { gdiCash = 2000, prodDelay = 600, autoDelay = 780, waveInterval = 300, waveCount = 2, prodInterval = 32, maxProducedUnits = 7, assaultSize = 6, pressureSquadSize = 3,
		vehiclePool = { "jeep", "mtnk", "msam", "mtnk" }, prodReserve = 600, rebuildReserve = 0, baseAttackDelay = 300 },
	hard = { gdiCash = 3000, prodDelay = 720, autoDelay = 900, waveInterval = 180, waveCount = 3, prodInterval = 22, maxProducedUnits = 9, assaultSize = 8, pressureSquadSize = 4,
		vehiclePool = { "mtnk", "msam", "htnk", "jeep" }, prodReserve = 400, rebuildReserve = 0, baseAttackDelay = 360 },
}

-- CurrentDifficulty is defined below and shared with hostile-takeover-AI.lua.
function CurrentDifficulty()
	return DifficultySettings[Difficulty] or DifficultySettings.normal
end

-- Cell lists from [CellTriggers]. Every cell is y*64 + x in the original;
-- the lists below are already converted to CPos.
local DzneCells = { -- 16 cells, x16-17/y48-55 (cyclone pen)
	CPos.New(16, 48), CPos.New(16, 49), CPos.New(16, 50), CPos.New(16, 51), CPos.New(16, 52), CPos.New(16, 53),
	CPos.New(16, 54), CPos.New(16, 55), CPos.New(17, 48), CPos.New(17, 49), CPos.New(17, 50), CPos.New(17, 51),
	CPos.New(17, 52), CPos.New(17, 53), CPos.New(17, 54), CPos.New(17, 55),
}

GDI9Cells = { -- 27 cells, GDI base west and south-east approaches
	CPos.New(47, 31), CPos.New(48, 29), CPos.New(48, 30), CPos.New(48, 31), CPos.New(48, 32), CPos.New(49, 25),
	CPos.New(49, 26), CPos.New(49, 27), CPos.New(49, 28), CPos.New(49, 29), CPos.New(49, 30), CPos.New(49, 31),
	CPos.New(49, 32), CPos.New(50, 25), CPos.New(50, 26), CPos.New(50, 27), CPos.New(50, 28), CPos.New(50, 29),
	CPos.New(56, 39), CPos.New(57, 38), CPos.New(57, 39), CPos.New(58, 37), CPos.New(58, 38), CPos.New(58, 39),
	CPos.New(59, 37), CPos.New(59, 38), CPos.New(60, 37),
}

GDI2Cells = { -- 42 cells, ring around the damaged Nod base
	CPos.New(9, 20), CPos.New(9, 21), CPos.New(9, 22), CPos.New(9, 23), CPos.New(9, 24), CPos.New(9, 25),
	CPos.New(10, 20), CPos.New(10, 21), CPos.New(10, 22), CPos.New(10, 23), CPos.New(10, 24), CPos.New(10, 25),
	CPos.New(10, 26), CPos.New(10, 27), CPos.New(11, 20), CPos.New(11, 21), CPos.New(11, 26), CPos.New(11, 27),
	CPos.New(12, 20), CPos.New(12, 21), CPos.New(12, 26), CPos.New(12, 27), CPos.New(13, 20), CPos.New(13, 21),
	CPos.New(13, 26), CPos.New(13, 27), CPos.New(14, 20), CPos.New(14, 21), CPos.New(14, 22), CPos.New(14, 23),
	CPos.New(14, 24), CPos.New(14, 25), CPos.New(14, 26), CPos.New(14, 27), CPos.New(15, 20), CPos.New(15, 21),
	CPos.New(15, 22), CPos.New(15, 23), CPos.New(15, 24), CPos.New(15, 25), CPos.New(15, 26), CPos.New(15, 27),
}

GDI3Cells = { -- 26 cells, band north of the damaged Nod base
	CPos.New(2, 18), CPos.New(2, 19), CPos.New(3, 18), CPos.New(3, 19), CPos.New(4, 18), CPos.New(4, 19),
	CPos.New(5, 18), CPos.New(5, 19), CPos.New(6, 18), CPos.New(6, 19), CPos.New(7, 18), CPos.New(7, 19),
	CPos.New(8, 18), CPos.New(8, 19), CPos.New(9, 18), CPos.New(9, 19), CPos.New(10, 18), CPos.New(10, 19),
	CPos.New(11, 18), CPos.New(11, 19), CPos.New(12, 18), CPos.New(12, 19), CPos.New(13, 18), CPos.New(13, 19),
	CPos.New(14, 18), CPos.New(14, 19),
}

Grd1Cells = { -- 24 cells, northern passage
	CPos.New(20, 1), CPos.New(20, 2), CPos.New(20, 3), CPos.New(20, 4), CPos.New(20, 5), CPos.New(21, 1),
	CPos.New(21, 2), CPos.New(21, 3), CPos.New(21, 4), CPos.New(21, 5), CPos.New(23, 7), CPos.New(23, 8),
	CPos.New(24, 7), CPos.New(24, 8), CPos.New(25, 7), CPos.New(25, 8), CPos.New(26, 7), CPos.New(26, 8),
	CPos.New(27, 7), CPos.New(27, 8), CPos.New(28, 7), CPos.New(28, 8), CPos.New(29, 7), CPos.New(29, 8),
}

Grd2Cells = { -- 27 cells, three chokepoints between the Nod and GDI bases
	CPos.New(12, 35), CPos.New(13, 35), CPos.New(13, 36), CPos.New(14, 35), CPos.New(14, 36), CPos.New(14, 37),
	CPos.New(15, 36), CPos.New(15, 37), CPos.New(16, 37), CPos.New(23, 25), CPos.New(23, 26), CPos.New(23, 27),
	CPos.New(24, 25), CPos.New(24, 26), CPos.New(24, 27), CPos.New(25, 25), CPos.New(25, 26), CPos.New(25, 27),
	CPos.New(32, 22), CPos.New(33, 21), CPos.New(33, 22), CPos.New(34, 20), CPos.New(34, 21), CPos.New(34, 22),
	CPos.New(35, 20), CPos.New(35, 21), CPos.New(36, 20),
}

---@type player
local Neutral
---@type player
local NodBase
---@type integer?
local EliminateGDI
---@type integer?
local CaptureTransport
---@type integer?
local ReachOutpost
local CheckObjectives

local function WinMission()
	if MissionFinished then return end
	MissionFinished = true
	Nod.MarkCompletedObjective(EliminateGDI)
end

local function FailMission()
	if MissionFinished then return end
	MissionFinished = true
	Nod.MarkFailedObjective(EliminateGDI)
end

-- Called by the AI file from Trigger.OnCapture on the three empty Chinooks.
function OnTransportCaptured()
	if MissionFinished or TransportObjectiveDone then return end
	TransportObjectiveDone = true
	Nod.MarkCompletedObjective(CaptureTransport)
end

local function MarkDropZone()
	if OutpostMarked or MissionFinished then return end
	OutpostMarked = true
	Actor.Create("flare", true, { Owner = Nod, Location = DefaultFlareLocation.Location })
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("outpost-marked"))
end

local function OnOutpostReached()
	if MissionFinished or OutpostReached then return end
	OutpostReached = true
	Nod.MarkCompletedObjective(ReachOutpost)
end

local function CheckTransportObjective()
	if TransportObjectiveDone or MissionFinished then return end
	for _, tran in ipairs(GDIChinooks) do
		if IsLive(tran) and tran.Owner == Nod then
			OnTransportCaptured()
			return
		end
	end

	for _, actor in ipairs(AI.GetActorsByType(Nod, "tran.capture")) do
		if IsLive(actor) then
			OnTransportCaptured()
			return
		end
	end
end

-- The old base starts Neutral: own buildings are always visible to their
-- owner, so a Nod-owned construction yard at (7,26) would reveal the whole
-- base (and its parked harvester) on mission start. The base and its
-- harvester become Nod only when a Nod ground unit reaches the flare.
local function FindNodBaseHarvester()
	if not IsLive(NodProc) then return nil end
	for _, harv in ipairs(AI.GetActorsByType(NodBase, "harv")) do
		if IsLive(harv) and harv.Owner == NodBase and harv.HasProperty("Location") then
			local dx = harv.Location.X - NodProc.Location.X
			local dy = harv.Location.Y - NodProc.Location.Y
			if dx * dx + dy * dy <= 36 then
				return harv
			end
		end
	end
	return nil
end

local function EnsureBaseHarvester()
	if not NodBaseReclaimed or MissionFinished then return end
	local harv = FindNodBaseHarvester()
	if harv == nil then return end

	-- Keep the collector at the refinery; the original mission expects it
	-- parked there. Never call FindResources here.
	harv.Owner = Nod
	if harv.HasProperty("Stop") then harv.Stop() end
	if harv.HasProperty("Stance") then harv.Stance = "Defend" end
end

local function TransferNodBase()
	if NodBaseReclaimed or MissionFinished then return end

	if IsLive(NodFact) and NodFact.Owner ~= Nod then
		NodFact.Owner = Nod
	end
	if IsLive(NodProc) and NodProc.Owner ~= Nod then
		NodProc.Owner = Nod
	end

	NodBaseReclaimed = true
	EnsureBaseHarvester()
	if OnNodBaseReclaimed ~= nil then
		OnNodBaseReclaimed()
	end
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("base-reclaimed"))
end

local function TransferOutpost()
	if OutpostCaptured or MissionFinished then return end

	if IsLive(NodHand) and NodHand.Owner ~= Nod then
		NodHand.Owner = Nod
	end
	if IsLive(NodHQ) and NodHQ.Owner ~= Nod then
		NodHQ.Owner = Nod
	end

	OutpostCaptured = true
	Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("outpost-captured"))
end

local function CheckOutpostTransfer()
	if OutpostCaptured or MissionFinished then return end

	local hqCell = CPos.New(34, 41)
	for _, actor in ipairs(Nod.GetActors()) do
		-- Any Nod mobile actor counts, including the captured Chinook: the
		-- player should not have to park on the building. The flare and other
		-- static Nod actors have no Move property and are ignored.
		if IsLive(actor) and actor.HasProperty("Location") and actor.HasProperty("Move") then
			local dx = actor.Location.X - hqCell.X
			local dy = actor.Location.Y - hqCell.Y
			if dx * dx + dy * dy <= 144 then
				TransferOutpost()
				return
			end
		end
	end
end

-- Base transfer is independent of the objective flag so it still happens if
-- the objective was completed earlier (or from an old state).
local function CheckNodBaseReclaim()
	if NodBaseReclaimed or MissionFinished then return end
	local targets = { CPos.New(6, 34), CPos.New(7, 26) }
	for _, actor in ipairs(Nod.GetActors()) do
		if IsLive(actor) and actor.HasProperty("Location") and actor.HasProperty("Move") then
			for _, cell in ipairs(targets) do
				local dx = actor.Location.X - cell.X
				local dy = actor.Location.Y - cell.Y
				-- Radius 12 and aircraft included: the captured Chinook
				-- arriving at the flare is enough to reclaim the base.
				if dx * dx + dy * dy <= 144 then
					TransferNodBase()
					return
				end
			end
		end
	end
end

local function CheckOutpostObjective()
	if OutpostReached or MissionFinished then return end
	local targets = { CPos.New(6, 34), CPos.New(7, 26) }
	for _, actor in ipairs(Nod.GetActors()) do
		if IsLive(actor) and actor.HasProperty("Location") and actor.HasProperty("Move") then
			for _, cell in ipairs(targets) do
				local dx = actor.Location.X - cell.X
				local dy = actor.Location.Y - cell.Y
				if dx * dx + dy * dy <= 144 then
					TransferNodBase()
					OnOutpostReached()
					return
				end
			end
		end
	end
end

CheckObjectives = function()
	if MissionFinished then return end

	CheckTransportObjective()
	CheckOutpostTransfer()
	CheckNodBaseReclaim()
	CheckOutpostObjective()
	EnsureBaseHarvester()

	if GDI.HasNoRequiredUnits() then
		WinMission()
		return
	end

	if Nod.HasNoRequiredUnits() then
		FailMission()
		return
	end

	Trigger.AfterDelay(DateTime.Seconds(5), CheckObjectives)
end

local function SetupNodStances()
	Utils.Do(Nod.GetActors(), function(actor)
		if actor.HasProperty("Stance") then
			actor.Stance = "Defend"
		end
	end)
end


-- The original walkthrough captures the GDI Refinery together with the
-- Harvester docked inside it. OpenRA transfers only the building, so the
-- docked GDI harvester is handed to Nod on capture. The player can use the
-- captured refinery immediately; this is the mission's intended economy.
local function TransferRefineryHarvester()
	if MissionFinished then return end
	if not IsLive(GDIProc) or GDIProc.Owner ~= Nod then return end

	for _, harv in ipairs(AI.GetActorsByType(GDI, "harv")) do
		if IsLive(harv) and harv.Owner == GDI and harv.HasProperty("Location") then
			local dx = harv.Location.X - GDIProc.Location.X
			local dy = harv.Location.Y - GDIProc.Location.Y
			if dx * dx + dy * dy <= 36 then
				harv.Owner = Nod
				if harv.HasProperty("Stance") then
					harv.Stance = "Defend"
				end
				Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("harvester-captured"))
				return
			end
		end
	end
end

local function SetupRefineryCapture()
	if not IsLive(GDIProc) then return end

	Trigger.OnCapture(GDIProc, function(_, _, _, newOwner)
		if newOwner == Nod then
			TransferRefineryHarvester()
		end
	end)
end

local function SetupDzneTrigger()
	Trigger.OnEnteredFootprint(DzneCells, function(actor, id)
		if MissionFinished or actor.Owner ~= Nod then
			return
		end

		Trigger.RemoveFootprintTrigger(id)
		MarkDropZone()
	end)
end

local function SetupChurch()
	if not IsLive(Church) then
		return
	end

	Trigger.OnKilled(Church, function()
		if MissionFinished or ChurchCrateSpawned then
			return
		end

		ChurchCrateSpawned = true
		Actor.Create("moneycrate", true, { Owner = Neutral, Location = CPos.New(53, 44) })
		Media.DisplayMessageToPlayer(Nod, UserInterface.GetFluentMessage("crate-found"))
	end)
end

function WorldLoaded()
	Nod = Player.GetPlayer("Nod")
	GDI = Player.GetPlayer("GDI")
	Neutral = Player.GetPlayer("Neutral")
	NodBase = Player.GetPlayer("NodBase")

	Nod.Cash = NodStartingCash
	GDI.Cash = CurrentDifficulty().gdiCash

	InitObjectives(Nod)
	EliminateGDI = AddPrimaryObjective(Nod, "eliminate-gdi-forces")
	CaptureTransport = AddSecondaryObjective(Nod, "capture-transport")
	ReachOutpost = AddSecondaryObjective(Nod, "reach-outpost")

	SetupNodStances()
	SetupDzneTrigger()
	SetupChurch()
	SetupRefineryCapture()
	InitGDI()

	Camera.Position = DefaultCameraPosition.CenterPosition
	CheckObjectives()
end