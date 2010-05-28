<?php 
$post_file = fopen('compress.zlib://php://input', 'rb');

$game_id = $_SERVER['HTTP_GAME_ID'];

$log_zip = new ZipArchive();
$log_zip->open($game_id.'.zip', ZIPARCHIVE::CREATE | ZIPARCHIVE::OVERWRITE);

$post_data = '';

while (!feof($post_file))
    $post_data .= fread($post_file, 8192);


$log_zip->addFromString(md5($_SERVER['REMOTE_ADDR']).'.log',$post_data);

fclose($post_file);
$log_zip->close();
?>
