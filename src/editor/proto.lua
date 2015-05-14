-- Copyright 2013-15 Paul Kulchenko, ZeroBrane LLC
---------------------------------------------------------

local q = EscapeMagic
local modpref = ide.MODPREF

ide.proto.Document = {__index = {
  GetFileName = function(self) return self.fileName end,
  GetFilePath = function(self) return self.filePath end,
  GetFileExt = function(self) return GetFileExt(self.fileName) end,
  GetModTime = function(self) return self.modTime end,
  GetEditor = function(self) return self.editor end,
  GetTabIndex = function(self) return self.index end,
  IsModified = function(self) return self.isModified end,
  SetModified = function(self, modified)
    self.isModified = modified
    self:SetTabText()
  end,
  SetTabText = function(self, text)
    ide:GetEditorNotebook():SetPageText(self.index,
      (self.isModified and modpref or '')..(text or self:GetTabText()))
  end,
  GetTabText = function(self)
    if self.index == nil then return self.fileName end
    return ide:GetEditorNotebook():GetPageText(self.index):gsub("^"..q(modpref), "")
  end,
  SetActive = function(self) SetEditorSelection(self.index) end,
  Save = function(self) return SaveFile(self.editor, self.filePath) end
}}

ide.proto.Plugin = {__index = {
  GetName = function(self) return self.name end,
  GetFileName = function(self) return self.fname end,
  GetConfig = function(self) return ide.config[self.fname] or {} end,
  GetSettings = function(self) return SettingsRestorePackage(self.fname) end,
  SetSettings = function(self, settings) SettingsSavePackage(self.fname, settings) end,
}}

ide.proto.Interpreter = {__index = {
  GetName = function(self) return self.name end,
  GetFileName = function(self) return self.fname end,
  GetAPI = function(self) return self.api end,
  fprojdir = function(self,wfilename)
    return wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
  fworkdir = function (self,wfilename)
    local proj = ide:GetProject()
    return proj and proj:gsub("[\\/]$","") or wfilename:GetPath(wx.wxPATH_GET_VOLUME)
  end,
}}

ide.proto.Debugger = {__index = {
  IsRunning = function(self) return self.running end,
  IsConnected = function(self) return self.server end,
  GetHostName = function(self) return self.hostname end,
  GetPortNumber = function(self) return self.portnumber end,
}}

ide.proto.ID = {
  __index = function(_, id) return _G['ID_'..id] end,
  __call = function(_, id) return IDgen(id) end,
}
