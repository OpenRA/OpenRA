<?php
$latest = "OpenRA-20100425.exe";
header("Location: $latest");
$file = file_get_contents("../../downloads.txt");
$new_downloads = $file + 1;
file_put_contents("../../downloads.txt", $new_downloads);
?>
