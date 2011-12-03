local ide = ide
local app = {

  loadfilters = {
    tools = function(file) return true end,
    specs = function(file) return true end,
    interpreters = function(file) return true end,
  },

  postinit = function ()
    dofile("estrela/menu_help.lua")
  
    local icon = wx.wxIcon()
    icon:LoadFile("estrela/res/estrela.ico",wx.wxBITMAP_TYPE_ICO)
    ide.frame:SetIcon(icon)
  end,
  
  stringtable = {
    editor = "Estrela Editor",
    about = "About Estrela Editor",
    editormessage = "Estrela Editor Message",
    statuswelcome = "Welcome to Estrela Editor",
    settingsapp = "EstrelaEditor",
    settingsvendor = "LuxiniaDev",
  },
  
}

return app
