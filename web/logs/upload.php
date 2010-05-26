<?php 
$post_file = fopen("php://input", "rb");
$log_file = fopen("log.".time().".gz", "wb");
$post_data = '';

while (!feof($post_file)) {
$post_data .= fread($post_file, 8192);
}

fwrite($log_file, $post_data);

fclose($post_file);
fclose($log_file);
?>
