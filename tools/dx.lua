-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local dxpath = os.getenv("DXSDK_DIR")
local dxprofile

return dxpath and {
  fninit = function(frame,menuBar)
    dxprofile = ide.config.dxprofile or "dx_4"

    local myMenu = wx.wxMenu{
      { ID "dx.profile.dx_2x", "DX SM&2_x", "DirectX sm2_x profile", wx.wxITEM_CHECK },
      { ID "dx.profile.dx_3", "DX SM&3_0", "DirectX sm3_0 profile", wx.wxITEM_CHECK },
      { ID "dx.profile.dx_4", "DX SM&4_0", "DirectX sm4_0 profile", wx.wxITEM_CHECK },
      { ID "dx.profile.dx_5", "DX SM&5_0", "DirectX sm5_0 profile", wx.wxITEM_CHECK },
      { },
      { ID "dx.compile.input", "&Custom Args", "when set a popup for custom compiler args will be envoked", wx.wxITEM_CHECK },
      { ID "dx.compile.legacy", "&Legacy", "when set compiles in legacy mode", wx.wxITEM_CHECK },
      { ID "dx.compile.backwards", "&Backwards Compatibility", "when set compiles in backwards compatibility mode", wx.wxITEM_CHECK },
      { },
      { ID "dx.compile.vertex", "Compile &Vertex", "Compile Vertex shader (select entry word)" },
      { ID "dx.compile.fragment", "Compile &Fragment", "Compile pixel shader (select entry word)" },
      { ID "dx.compile.geometry", "Compile &Geometry", "Compile Geometry shader (select entry word)" },
      { ID "dx.compile.domain", "Compile &Domain", "Compile Domain shader (select entry word)" },
      { ID "dx.compile.hull", "Compile &Hull", "Compile Hull shader (select entry word)" },
    }
    menuBar:Append(myMenu, "&Dx")

    local data = {}
    data.customarg = false
    data.custom = ""
    data.legacy = false
    data.backwards = false
    data.profid = ID ("dx.profile."..dxprofile)
    data.domains = {
      [ID "dx.compile.vertex"] = 1,
      [ID "dx.compile.fragment"] = 2,
      [ID "dx.compile.geometry"] = 3,
      [ID "dx.compile.domain"] = 4,
      [ID "dx.compile.hull"] = 5,
    }
    data.profiles = {
      [ID "dx.profile.dx_2x"] = {"vs_2_0","ps_2_x",false,false,false,ext=".fxc.txt"},
      [ID "dx.profile.dx_3"] = {"vs_3_0","ps_3_0",false,false,false,ext=".fxc.txt"},
      [ID "dx.profile.dx_4"] = {"vs_4_0","ps_4_0","gs_4_0",false,false,ext=".fxc.txt"},
      [ID "dx.profile.dx_5"] = {"vs_5_0","ps_5_0","gs_5_0","ds_5_0","hs_5_0",ext=".fxc.txt"},
    }
    data.domaindefs = {
      " /D _VERTEX_=1 /D _DX_=1 ",
      " /D _FRAGMENT_=1 /D _DX_=1 ",
      " /D _GEOMETRY_=1 /D _DX_=1 ",
      " /D _TESS_CONTROL_=1 /D _DX_=1 ",
      " /D _TESS_EVAL_=1 /D _DX_=1 ",
    }
    -- Profile related
    menuBar:Check(data.profid, true)

    local function selectProfile (id)
      for id,profile in pairs(data.profiles) do
        menuBar:Check(id, false)
      end
      menuBar:Check(id, true)
      data.profid = id
    end

    local function evSelectProfile (event)
      local chose = event:GetId()
      selectProfile(chose)
    end

    for id,profile in pairs(data.profiles) do
      frame:Connect(id,wx.wxEVT_COMMAND_MENU_SELECTED,evSelectProfile)
    end

    -- Compile Arg
    frame:Connect(ID "dx.compile.input",wx.wxEVT_COMMAND_MENU_SELECTED,
      function(event)
        data.customarg = event:IsChecked()
      end)
    frame:Connect(ID "dx.compile.legacy",wx.wxEVT_COMMAND_MENU_SELECTED,
      function(event)
        data.legacy = event:IsChecked()
      end)
    frame:Connect(ID "dx.compile.backwards",wx.wxEVT_COMMAND_MENU_SELECTED,
      function(event)
        data.backwards = event:IsChecked()
      end)
    -- Compile
    local function evCompile(event)
      local filename,info = GetEditorFileAndCurInfo()
      local editor = GetEditor()

      if (not (filename and info.selword and dxpath)) then
        DisplayOutput("Error: Dx Compile: Insufficient parameters (nofile / not selected entry function!\n")
        return
      end

      local domain = data.domains[event:GetId()]
      local profile = data.profiles[data.profid]
      if (not profile[domain]) then return end

      -- popup for custom input
      data.custom = data.customarg and wx.wxGetTextFromUser("Compiler Args","Dx",data.custom) or data.custom
      local args = data.custom:len() > 0 and data.custom or nil

      local fullname = filename:GetFullPath()

      local outname = fullname.."."..info.selword.."^"
      outname = args and outname..args:gsub("%s*[%-%/]",";-")..";^" or outname
      outname = outname..profile[domain]..profile.ext
      outname = '"'..outname..'"'

      local cmdline = " /T "..profile[domain].." "
      cmdline = cmdline..(args and args.." " or "")
      cmdline = cmdline..(data.legacy and "/LD " or "")
      cmdline = cmdline..(data.backwards and "/Gec " or "")
      cmdline = cmdline..data.domaindefs[domain]
      cmdline = cmdline.."/Fc "..outname.." "
      cmdline = cmdline.."/E "..info.selword.." "
      cmdline = cmdline.."/nologo "
      cmdline = cmdline..' "'..fullname..'"'

      cmdline = dxpath.."/Utilities/bin/x86/fxc.exe"..cmdline

      -- run compiler process
      CommandLineRun(cmdline,nil,true,nil,nil)
    end

    frame:Connect(ID "dx.compile.vertex",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
    frame:Connect(ID "dx.compile.fragment",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
    frame:Connect(ID "dx.compile.geometry",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
    frame:Connect(ID "dx.compile.domain",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
    frame:Connect(ID "dx.compile.hull",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
  end,
}
