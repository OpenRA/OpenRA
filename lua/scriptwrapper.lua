--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]
environment = {}

-- Reset package path
package.path = EngineDir .. "/lua/?.lua"

-- Note: sandbox has been customized to remove math.random
local sandbox = require('sandbox')
local stp = require('stacktraceplus')

local PrintStackTrace = function(msg)
	return stp.stacktrace("", 2) .. "\nError message\n===============\n" .. msg .. "\n==============="
end

local TryRunSandboxed = function(fn)
	local success, err = xpcall(function() sandbox.run(fn, {env = environment, quota = MaxUserScriptInstructions}) end, PrintStackTrace)
	if not success then
		FatalError(err)
	end
end

WorldLoaded = function()
	if environment.WorldLoaded ~= nil then
		TryRunSandboxed(environment.WorldLoaded)
	end
end

Tick = function()
	if environment.Tick ~= nil then
		TryRunSandboxed(environment.Tick)
	end
end

ExecuteSandboxedScript = function(file, contents)
	local script, err = loadstring(contents, file)
	if (script == nil) then
		FatalError("Error parsing " .. file .. ". Reason: " .. err)
	else
		TryRunSandboxed(script)
	end
end

RegisterSandboxedGlobal = function(key, value)
	environment[key] = value
end
