<?php
$mirrors = file_get_contents("ra-mirrors.txt");
$mirrors_array = explode("\n", $mirrors);

$mirror = $mirrors_array[array_rand($mirrors_array, 1)];

header('Location: '. $mirror);
?>
