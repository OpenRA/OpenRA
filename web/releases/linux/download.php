<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN"
        "http://www.w3.org/TR/html4/loose.dtd">
<html>
<head>
	<link rel="shortcut icon" type="image/x-icon" href="/favicon.ico" />
	<title>OpenRA - Download for Linux</title>
	<link rel="Stylesheet" type="text/css" href="/openra.css" />
	<!--[if IE 7]>
			<link rel="stylesheet" type="text/css" href="/ie7.css">
	<![endif]-->
</head>
<body>
	<div id="header" class="bar">
		<h1>
			<img src="/soviet-logo.png" alt="Logo" />OpenRA</h1>
	</div>
	<div id="main">
		<div id="menu">
			<span class="links"><a href="/index.html">Home</a></span> 
			<span class="links"><a href="/footage.html">Gameplay Footage</a></span> 
			<span class="links"><a href="/mods.html">Mods</a></span> 
			<span class="links"><a href="/stats.html">Stats</a></span>
			<span class="links"><a href="/getinvolved.html">Get Involved</a></span>
			<span class="links"><a href="irc://irc.freenode.net/openra">IRC</a></span>
			<span class="links"><a href="http://twitter.com/openRA">Twitter</a></span>
		</div>
		<div>
		    <?php
		        function generateDownloadButton($target, $text, $desc)
		        {
		            echo "<div class=\"rounded download\" style=\"display: block; text-align: center; width: 320px\" onclick=\"document.location='".$target."'\">\n";
		            echo "\t<img src=\"/arrow.png\" alt=\"Download Arrow\" style=\"float: left; margin-top: 3px\" />\n";
		            echo "\t". $text ."<br />\n";
		            echo "\t<span class=\"desc\">". $desc ."</span>\n";
		            echo "</div>";
		        }
		        
		        $archTarget = file_get_contents("archlatest.txt");
		        list($version,$size,$target) = explode(",", $archTarget);
		        $desc = sprintf("version: %s size: %.2fMB", $version, $size/1048576);
		        
		        generateDownloadButton(trim($target), "Download for Arch Linux", $desc);
		        
		        $rpmTarget = file_get_contents("rpmlatest.txt");
		        list($version,$size,$target) = explode(",", $rpmTarget);
		        $desc = sprintf("version: %s size: %.2fMB", $version, $size/1048576);
		        
		        generateDownloadButton(trim($target), "Download for RPM based systems", $desc);
		        
		        $debTarget = file_get_contents("deblatest.txt");
		        list($version,$size,$target) = explode(",", $debTarget);
		        $desc = sprintf("version: %s size: %.2fMB", $version, $size/1048576);
		        
		        generateDownloadButton(trim($target), "Download for deb based systems", $desc);
		        
		        $version = file_get_contents("srclatest.txt");
		        $target = "http://github.com/chrisforbes/OpenRA/tarball/playtest-".trim($version);
		        
		        generateDownloadButton($target, "Download for other systems", "(source package)");
		    ?>
		</div>
	</div>
	<div id="footer" class="bar">
		<p id="trademarks">
			<img src="/soviet-logo.png" alt="OpenRA Logo" height="70px" style="float: right; margin-right: 10px" />
			Command &amp; Conquer and Command &amp; Conquer Red Alert are trademarks or registered
			trademarks of Electronic Arts Inc.in the U.S. and/or other countries.<br />
			Windows is a registered trademark of Microsoft Corporation in the United States
			and other countries.<br />
			Mac OS X is a trademark of Apple Inc., registered in the U.S. and other countries.<br />
			Linux is the registered trademark of Linus Torvalds in the U.S. and other countries.<br />
			Mono is a registered trademark of Novell, Inc. in the United States and other countries.<br />
		</p>
	</div>
</body>
</html>
