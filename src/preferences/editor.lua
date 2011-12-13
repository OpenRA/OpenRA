-- authors: Luxinia Dev (Eike Decker & Christoph Kubisch)
---------------------------------------------------------

preferencesDialog.addCategory {
  category = "editor";
  title = "Editor";
}

preferencesDialog.addPage {
  title = "Basic preferences";
  category = "editor";
  layout = {
    {type = 'group',title="Sessions"; minheight = 100; minwidth = 100};
    {type = "checkbox"; title = "Reopen files";name = 'session_restore'};
    {type = 'finishgroup'};
    {type = 'space'; space = 4};
  };
  onload = function ()
    return {testbox = true}
  end;
  onsave = function (values)
  end
}
