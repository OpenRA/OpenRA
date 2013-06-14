-- Copyright 2013 Paul Kulchenko, ZeroBrane LLC

local ide = ide

function PackageEventHandle(event, ...)
  local success
  for _, package in pairs(ide.packages) do
    if type(package[event]) == 'function' then
      local ok, res = pcall(package[event], package, ...)
      if ok then
        if res == false then success = false end
      else
        DisplayOutputLn(TR("%s event failed: %s"):format(event, res))
      end
    end
  end
  return success
end

local function PackageEventHandleOne(file, event, ...)
  local package = ide.packages[file]
  if package and type(package[event]) == 'function' then
    local ok, res = pcall(package[event], package, ...)
    if ok then
      if res == false then return false end
    else
      DisplayOutputLn(TR("%s event failed: %s"):format(event, res))
    end
  end
end

function PackageUnRegister(file, ...)
  PackageEventHandleOne(file, "onUnRegister", ...)
  -- remove from the list of installed packages
  ide.packages[file] = nil
end

function PackageRegister(file, ...)
  if not ide.packages[file] then
    local packages = {}
    local package = MergeFullPath(
      GetPathWithSep(ide.editorFilename), "packages/"..file..".lua")
    LoadLuaFileExt(packages, package, ide.proto.Plugin)
    packages[file].fname = file
    ide.packages[file] = packages[file]
  end
  return PackageEventHandleOne(file, "onRegister", ...)
end

function ide:GetEditor() return GetEditor() end
function ide:GetMenuBar() return self.frame.menuBar end
function ide:GetMainFrame() return self.frame end
function ide:GetDocument(ed) return self.openDocuments[ed:GetId()] end
function ide:GetInterpreter() return self.interpreter end
function ide:GetConfig() return self.config end
