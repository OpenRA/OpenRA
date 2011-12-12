-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

preferencesDialog.addCategory {
  category = "project";
  title = "Project";
}

preferencesDialog.addPage {
  title = "Project settings";
  category = "project";
  layout = {
    {type = 'group',title="Visible menus"; minheight = 100; minwidth = 100};
    {type = "checkbox"; title = "Tools";name = 'tools'};
    {type = 'linebreak'; space = 4};
    {type = "checkbox"; title = "Help";name = 'help'};
    {type = 'linebreak'; space = 4};
    --{type='static'; title = "foo"};
    {type = 'finishgroup'};
    {type = "space"; space = 4};
    {type = 'group',title="Interpreter"; minheight = 100; minwidth = 100};
    {type = "static"; title = "Interpreter"};
    {type = "space"; space = 4};
    {type = "combobox"; name = "interpreterlist"};
    {type = 'linebreak'; space = 4};
    {type = "static";title = "Working directory"};
    {type = "dirpicker"; name = "workingdir", title='Working directory'};
    {type = 'linebreak'; space = 4};
    {type = "static"; title = "Argument"};
    {type = "edit"; name = "argument"};
    {type = 'finishgroup'};
  };
  onload = function () return {
    interpreterlist = {"1","2"}
  } end;
  onsave = function (values) end;
}
