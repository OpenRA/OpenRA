local G = ... -- this now points to the global environment in the script

local env = {}
G.setmetatable(env, {__index = G})

local function runtests()
  -- add a test function to detect loops
  function limit (limit, func)
    debug.sethook(function() error("exceeded") end, "", limit)
    local ok, res = pcall(func)
    debug.sethook()
    if ok and ide:GetOutput():GetText():find("Auto-complete was aborted") then
      ok, res = false, "Auto-complete was aborted"
      ide:GetOutput():SetText('')
    end
    return ok, res
  end

  function limitdepth (limit, func)
    local n = 0
    debug.sethook(function(event)
      if event == 'call' then n = n + 1
      elseif event == 'return' or event == 'tail return' then n = n - 1
      end
      if n > limit then error("exceeded") end
    end, "cr")
    local ok, res = pcall(func)
    n = 0
    debug.sethook()
    return ok, res
  end

  -- find all test files and load them
  local files = FileSysGetRecursive("t", true, "*.lua")
  for k, v in ipairs(files) do
    if v:find("[/\\]test%.lua$") then files[k] = nil end
  end
  table.sort(files)

  -- load test module in the environment for tests
  local tw = require "testwell"

  for _,file in ipairs(files) do
    local testfn, err = loadfile(file)
    if not testfn then
      print(("Error loading test file '%s': '%s'."):format(file, err))
    else
      setfenv(testfn, env)
      local ok, err = pcall(testfn)
      if not ok then
        print(("Error executing test file '%s': '%s'."):format(file, err))
      end
    end
  end

  tw.report()
end

package {
  onAppLoad = function()
    local start = G.TimeGet()
    G.setfenv(runtests, env)
    G.print = function(s, ...)
      G.DisplayOutput(s, ...)
      G.DisplayOutputLn(s:match("ok %d") and (" -- %.3fs"):format(G.TimeGet()-start) or "")
    end
    runtests()
  end,
  onIdleOnce = function() G.ide:GetOutput():GotoLine(G.ide:GetOutput():GetLineCount()-1) end,
  onAppDone = function()
    local ini = G.ide.config.ini
    if ini then G.FileRemove(ini) end
  end,
}
