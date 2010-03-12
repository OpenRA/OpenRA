<?php
	header( 'Content-type: text/plain' );

	if (!($db = sqlite_open( 'openra.db', 0666, $e )))
	{
		echo 'Database error: ', $e;
		return;
	}
	
	$stale = 60 * 5;
	$result = sqlite_query( $db, 'SELECT * FROM servers WHERE (' . time() . ' - ts < ' . $stale . ')' );
	
	$rows = sqlite_fetch_all( $result, SQLITE_ASSOC );
	
	$n = 0;
	foreach( $rows as $a ) {
		echo "Game@" . $n++ . ":\n";
		echo "\tName: " . $a['name'] . "\n";
		echo "\tAddress: " . $a['address'] . "\n";
		echo "\tState: " . $a['state'] . "\n";
		echo "\tPlayers: " . $a['players'] . "\n";
		echo "\tTTL: " . ($stale - (time() - $a['ts'])) . "\n";
	}
	
	sqlite_close( $db );
?>