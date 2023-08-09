
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
	if Closest.DockToClosestHost({}, true) then
		Media.DisplayMessage("Harv1 found closest dock");
	else
		Media.DisplayMessage("Harv1 couldn't find dock");
	end
	CurrentClosest = CurrentClosest + 1
	if CurrentClosest > #Waypoints then
		CurrentClosest = 1

	end
	Trigger.AfterDelay(DateTime.Seconds(7), MoveToWaypont)
end

NextDock = function()
	local Target = Targets[CurrentManual]
	if Target.IsDead then
		table.remove(Targets, CurrentManual)
		if #Targets == 0 then
			Media.DisplayMessage("No more targets to dock to")
			return
		end

		if CurrentManual > #Targets then
			CurrentManual = 1
		end
		NextDock()
		return
	end

	if Manual.CanDockAt(Target, { "Unload" }, true) then
		Manual.Dock(Target, { }, true)
		Media.DisplayMessage("Harv2 moving to " .. tostring(Target));

		CurrentManual = CurrentManual + 1
		if CurrentManual > #Targets then
			CurrentManual = 1
		end
	else
		Media.DisplayMessage("Harv2 could not dock to " .. tostring(Target));
	end

	Trigger.AfterDelay(DateTime.Seconds(7), NextDock)
end
