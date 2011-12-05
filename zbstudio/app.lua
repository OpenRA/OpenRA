local ide = ide
local app = {

  loadfilters = {
    tools = function(file) return false end,
    specs = function(file) return true end,
    interpreters = function(file) return true end,
  },

  preinit = function ()
    local artProvider = wx.wxLuaArtProvider()
    local icons = {}
    artProvider.CreateBitmap = function(self, id, client, size)
      local width = size:GetWidth()
      local key = width .. "/" .. id
      local file = "zbstudio/res/" .. key .. ".png"
      if not wx.wxFileName(file):FileExists() then return wx.wxNullBitmap end
      local icon = icons[key] or wx.wxBitmap(file)
      icons[key] = icon
      return icon
    end
    wx.wxArtProvider.Push(artProvider)

    ide.config.interpreter = "luadeb";
  end,

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
