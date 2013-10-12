-- Copyright 2013 Paul Kulchenko, ZeroBrane LLC

ide.proto.Document = {__index = {
  GetFileName = function(self) return self.fileName end,
  GetFilePath = function(self) return self.filePath end,
  GetModTime = function(self) return self.modTime end,
  GetEditor = function(self) return self.editor end,
  GetTabIndex = function(self) return self.index end,
  IsModified = function(self) return self.isModified end,
  SetModified = function(self, modified) SetDocumentModified(self.editor:GetId(), modified) end,
  SetTabText = function(self, text) SetDocumentModified(self.editor:GetId(), self.isModified, text) end,
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
}}
