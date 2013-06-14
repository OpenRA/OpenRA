-- Copyright 2013 Paul Kulchenko, ZeroBrane LLC

ide.proto.Document = {__index = {
  GetFileName = function(self) return self.fileName end,
  GetFilePath = function(self) return self.filePath end,
  GetModTime = function(self) return self.modTime end,
  IsModified = function(self) return self.isModified end,
}}

ide.proto.Plugin = {__index = {
  GetName = function(self) return self.name end,
  GetFileName = function(self) return self.fname end,
}}

ide.proto.Interpreter = {__index = {
  GetName = function(self) return self.name end,
  GetFileName = function(self) return self.fname end,
}}
