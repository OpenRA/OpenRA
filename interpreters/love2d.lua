-- Copyright 2011-12 Paul Kulchenko, ZeroBrane LLC

local love2d
local win = ide.osname == "Windows"
local mac = ide.osname == "Macintosh"

return {
  name = "Love2d",
  description = "Love2d game engine",
  api = {"baselib", "love2d"},
  frun = function(self,wfilename,rundebug)
    love2d = love2d or ide.config.path.love2d -- check if the path is configured
    if not love2d then
      local sep = win and ';' or ':'
      local default =
           win and (GenerateProgramFilesPath('love', sep)..sep)
        or mac and ('/Applications/love.app/Contents/MacOS'..sep)
        or ''
      local path = default
                 ..(os.getenv('PATH') or '')..sep
                 ..(GetPathWithSep(self:fworkdir(wfilename)))..sep
                 ..(os.getenv('HOME') and GetPathWithSep(os.getenv('HOME'))..'bin' or '')
      local paths = {}
      for p in path:gmatch("[^"..sep.."]+") do
        love2d = love2d or GetFullPathIfExists(p, win and 'love.exe' or 'love')
        table.insert(paths, p)
      end
      if not love2d then
        DisplayOutput("Can't find love2d executable in any of the following folders: "
          ..table.concat(paths, ", ").."\n")
        return
      end
    end

    if not GetFullPathIfExists(self:fworkdir(wfilename), 'main.lua') then
      DisplayOutput("Can't find 'main.lua' file in the current project folder.\n")
      return
    end

    if rundebug then
      DebuggerAttachDefault({runstart = ide.config.debugger.runonstart == true})
    end

    local params = ide.config.arg.any or ide.config.arg.love2d
    local cmd = ('"%s" "%s"%s%s'):format(love2d, self:fworkdir(wfilename),
      params and " "..params or "", rundebug and ' -debug' or '')
    -- CommandLineRun(cmd,wdir,tooutput,nohide,stringcallback,uid,endcallback)
    return CommandLineRun(cmd,self:fworkdir(wfilename),true,true)
  end,
  hasdebugger = true,
  fattachdebug = function(self) DebuggerAttachDefault() end,
  scratchextloop = true,
  takeparameters = true,
}
