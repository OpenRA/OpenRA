
CurrentManual = 1
CurrentClosest = 1
Targets = { Right, Top, Left }
Waypoints = { RightWay, TopWay, LeftWay}

WorldLoaded = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		NextDock();
		NextClosestDock();
	end)
end

MoveToWaypont = function ()
	Closest.Move(Waypoints[CurrentClosest].Location)
	Media.DisplayMessage("Harv1 moving to waypoint " .. tostring(Waypoints[CurrentClosest]));
	Trigger.AfterDelay(DateTime.Seconds(10), NextClosestDock)
end

NextClosestDock = function()
	if Closest.LinkToClosestHost({}, true) then
		Media.DisplayMessage("Harv1 found closest dock");
	else
		Media.DisplayMessage("Harv1 couldn't find dock");
	end
	CurrentClosest = CurrentClosest + 1
	if CurrentClosest > 3 then
		CurrentClosest = 1
	end

	Trigger.AfterDelay(DateTime.Seconds(7), MoveToWaypont)
end

NextDock = function()
	if Manual.CanLinkTo(Targets[CurrentManual], { "Unload" }, true) then
		Manual.Link(Targets[CurrentManual], { }, true)
		Media.DisplayMessage("Harv2 moving to " .. tostring(Targets[CurrentManual]));

		CurrentManual = CurrentManual + 1
		if CurrentManual > 3 then
			CurrentManual = 1
		end
	else
		Media.DisplayMessage("Harv2 could not dock to " .. tostring(Targets[CurrentManual]));
	end

	Trigger.AfterDelay(DateTime.Seconds(7), NextDock)
end
