local ide = ide
local app = {

  loadfilters = {
    tools = function(file) return false end,
    specs = function(file) return true end,
    interpreters = function(file) return true end,
  },

  postinit = function ()
    local icon = wx.wxIcon()
    icon:LoadFile("zbstudio/res/zbstudio.ico",wx.wxBITMAP_TYPE_ICO)
    ide.frame:SetIcon(icon)
  end,
  
  stringtable = {
    editor = "ZeroBrane Studio",
    about = "About ZeroBrane Studio",
    editormessage = "ZeroBrane Studio Message",
    statuswelcome = "Welcome to ZeroBrane Studio",
    settingsapp = "ZeroBraneStudio",
    settingsvendor = "ZeroBraneLLC",
  },
  
}

return app
