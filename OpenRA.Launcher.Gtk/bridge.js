window.external=new Object();
window.external.do_ajax=function(uri)
{
    request = new XMLHttpRequest();
    request.open("GET", uri, false);
    try
    {
	request.send(null);
    }
    catch(err)
    {
    }
    return request.responseText;
};
window.external.log=function(msg)
{
    window.external.do_ajax("http://localhost:48764/log?msg=" + escape(msg));
};
window.external.launchMod=function(mod)
{
    window.external.do_ajax("http://localhost:48764/launch?mod=" + mod);
};
window.external.existsInMod=function(file, mod)
{

    return window.external.do_ajax("http://localhost:48764/fileExists?mod=" + mod + "&file=" + escape(file));
};