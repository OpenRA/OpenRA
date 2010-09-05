<?php
$mirrors_file = "";
switch ($_GET["file"])
{
    case "ra":
        $mirrors_file = "packages/ra-mirrors.txt";
        break;
    case "cnc":
        $mirrors_file = "packages/cnc-mirrors.txt";
        break;
    case "osx":
        $mirrors_file = "releases/mac/osx-dependencies-mirrors.txt";
        break;
    case "freetype":
        $mirrors_file = "releases/windows/freetype-mirrors.txt";
        break;
    case "cg":
        $mirrors_file = "releases/windows/cg-mirrors.txt";
        break;
    default:
        break;
}

$mirrors = file_get_contents($mirrors_file);
$mirrors_array = explode("\n", $mirrors);

$mirror = $mirrors_array[array_rand($mirrors_array, 1)];

header('Location: '. $mirror);
?>
