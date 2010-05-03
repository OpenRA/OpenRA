<?php

	if (!($db = sqlite_open( 'openra.db', 0666, $e )))
	{
		echo 'Database error: ', $e;
		return;
	}
	
	$addr = $_SERVER['REMOTE_ADDR'] . ':' . $_REQUEST['port'];
	$prune = 'DELETE FROM servers WHERE address = \'' . sqlite_escape_string( $addr ) . '\'';
	echo $prune . "\n\n";
	
	sqlite_exec( $db, $prune );

	$q =  'INSERT INTO servers VALUES ('.
		'\'' . sqlite_escape_string( $_REQUEST['name'] ) . '\', '.
		'\'' . sqlite_escape_string( $addr ) . '\', '.
		sqlite_escape_string( $_REQUEST['players'] ) . ', '.
		sqlite_escape_string( $_REQUEST['state'] ) . ', '.
		time() . ', '.
		'\'' . sqlite_escape_string( $_REQUEST['map'] ) . '\', '.
		'\'' . sqlite_escape_string( $_REQUEST['mods'] ) . '\')';
		
	echo $q;	
	
	if (!sqlite_exec( $db, $q ))
	{
		echo 'Error in query:' . sqlite_error_string( sqlite_last_error() );
	}
	
	sqlite_close( $db );
	
	if (isset( $_REQUEST['new']))
	{
		$games = file_get_contents("../games.txt");
		file_put_contents("../games.txt", $games + 1);
	}
?>