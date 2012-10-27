-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

local clccbinpath = os.getenv("CLCC_BIN_PATH")

return clccbinpath and {
  fninit = function(frame,menuBar)
    local myMenu = wx.wxMenu{
      { ID "cl.allplatforms", "&All", "Compiled with all available platforms (otherwise only first)", wx.wxITEM_CHECK },
      { ID "cl.output", "&Output", "Generates output files", wx.wxITEM_CHECK },
      { ID "cl.info", "&Info", "Prints Info", wx.wxITEM_CHECK },
      { },
      { ID "cl.compile", "&Compile", "Compile Kernels in File" },
    }
    menuBar:Append(myMenu, "&OpenCL")

    local data = {
      allplatforms = false,
      output = false,
      info = false,
    }

    -- Compile Arg
    frame:Connect(ID "cl.allplatforms",wx.wxEVT_COMMAND_MENU_SELECTED,
      function(event)
        data.allplatforms = event:IsChecked()
      end)
    frame:Connect(ID "cl.output",wx.wxEVT_COMMAND_MENU_SELECTED,
      function(event)
        data.output = event:IsChecked()
      end)
    frame:Connect(ID "cl.info",wx.wxEVT_COMMAND_MENU_SELECTED,
      function(event)
        data.info = event:IsChecked()
      end)
    -- Compile
    local function evCompile(event)
      local filename,info = GetEditorFileAndCurInfo()
      local editor = GetEditor()

      if (not (filename)) then
        DisplayOutput("Error: OpenCL Compile: Insufficient parameters (nofile)!\n")
        return
      end

      local fullname = filename:GetFullPath()
      local cmdline = " "
      cmdline = cmdline..(data.allplatforms and "--platform -1 " or "")
      cmdline = cmdline..(data.info and "--info " or "")
      cmdline = cmdline..(data.output and "--output " or "")
      cmdline = cmdline..'"'..fullname..'"'

      cmdline = clccbinpath.."/clcc.exe"..cmdline

      -- run compiler process
      CommandLineRun(cmdline,nil,true,nil,nil)
    end

    frame:Connect(ID "cl.compile",wx.wxEVT_COMMAND_MENU_SELECTED,evCompile)
  end,
}
