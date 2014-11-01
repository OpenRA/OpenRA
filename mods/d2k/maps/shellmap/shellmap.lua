InitializeHarvester = function(harvester)
	harvester.FindResources()
	Trigger.OnRemovedFromWorld(harvester, InsertHarvester)
end

InsertHarvester = function()
	local harvesters = Reinforcements.ReinforceWithTransport(atreides, "carryalla", { "harvester" },
		{ Entry.Location, AtreidesSpiceRefinery.Location + CVec.New(2, 3) }, { Entry.Location })[2]

	Utils.Do(harvesters, function(harvester)
		Trigger.OnAddedToWorld(harvester, function() InitializeHarvester(harvester) end)
	end)
end

WorldLoaded = function()
	atreides = Player.GetPlayer("Atreides")

	InsertHarvester()
	Media.PlayMusic("score")
end
