-- Copyright 2013 Paul Kulchenko, ZeroBrane LLC

local ide = ide
local iscaseinsensitive = wx.wxFileName("A"):SameAs(wx.wxFileName("a"))
local q = EscapeMagic

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

function PackageEventHandleOnce(event, ...)
  local success = PackageEventHandle(event, ...)
  -- remove all handlers as they need to be called only once
  -- this allows them to be re-installed if needed
  for _, package in pairs(ide.packages) do
    if type(package[event]) == 'function' then
      package[event] = nil
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

function ide:GetEditor(index) return GetEditor(index) end
function ide:GetMenuBar() return self.frame.menuBar end
function ide:GetStatusBar() return self.frame.statusBar end
function ide:GetToolBar() return self.frame.toolBar end
function ide:GetMainFrame() return self.frame end
function ide:GetDocument(ed) return self.openDocuments[ed:GetId()] end
function ide:GetDocuments() return self.openDocuments end
function ide:FindDocument(path)
  local fileName = wx.wxFileName(path)
  for _, doc in pairs(ide.openDocuments) do
    if doc.filePath and fileName:SameAs(wx.wxFileName(doc.filePath)) then
      return doc
    end
  end
  return
end
function ide:FindDocumentsByPartialPath(path)
  local seps = "[\\/]"
  -- add trailing path separator to make sure full directory match
  if not path:find(seps.."$") then path = path .. GetPathSeparator() end
  local pattern = "^"..q(path):gsub(seps, seps)
  local lpattern = pattern:lower()

  local docs = {}
  for _, doc in pairs(ide.openDocuments) do
    if doc.filePath
    and (doc.filePath:find(pattern)
         or iscaseinsensitive and doc.filePath:lower():find(lpattern)) then
      table.insert(docs, doc)
    end
  end
  return docs
end
function ide:GetInterpreter() return self.interpreter end
function ide:GetInterpreters() return ide.interpreters end
function ide:GetConfig() return self.config end
function ide:GetOutput() return self.frame.bottomnotebook.errorlog end
function ide:GetEditorNotebook() return self.frame.notebook end
function ide:GetProject() return FileTreeGetDir() end

function ide:GetSetting(path, setting)
  local settings = self.settings
  local curpath = settings:GetPath()
  settings:SetPath(path)
  local ok, value = settings:Read(setting)
  settings:SetPath(curpath)
  return ok and value or nil
end

function ide:AddInterpreter(name, interpreter)
  self.interpreters[name] = setmetatable(interpreter, ide.proto.Interpreter)
  UpdateInterpreters()
end
function ide:RemoveInterpreter(name)
  self.interpreters[name] = nil
  UpdateInterpreters()
end

function ide:AddSpec(name, spec)
  self.specs[name] = spec
  UpdateSpecs()
end
function ide:RemoveSpec(name) self.specs[name] = nil end

function ide:AddAPI(type, name, api)
  self.apis[type] = self.apis[type] or {}
  self.apis[type][name] = api
end
function ide:RemoveAPI(type, name) self.apis[type][name] = nil end

function ide:AddConsoleAlias(alias, table) return ShellSetAlias(alias, table) end
function ide:RemoveConsoleAlias(alias) return ShellSetAlias(alias, nil) end

function ide:AddMarker(...) return StylesAddMarker(...) end
function ide:GetMarker(marker) return StylesGetMarker(marker) end
function ide:RemoveMarker(marker) StylesRemoveMarker(marker) end

-- this provides a simple stack for saving/restoring current configuration
local configcache = {}
function ide:AddConfig(name, files)
  if not name or configcache[name] then return end -- don't overwrite existing slots
  configcache[name] = require('mobdebug').dump(ide.config, {nocode = true})
  for _, file in pairs(files) do LoadLuaConfig(MergeFullPath(name, file)) end
end
function ide:RemoveConfig(name)
  if not name or not configcache[name] then return end
  local ok, res = LoadSafe(configcache[name])
  if ok then ide.config = res
  else
    DisplayOutputLn(("Error while restoring configuration: '%s'."):format(res))
  end
  configcache[name] = nil -- clear the slot after use
end
