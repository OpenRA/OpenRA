<?php

	if ($db = sqlite_open('openra.db', 0666, $e))
	{
		echo 'sqlite_open ok.';
		sqlite_query( $sb, 'DROP TABLE servers' );
		sqlite_query( $db, 'CREATE TABLE servers (name varchar(255), address varchar(255), players integer, state integer, ts integer, map varchar(255), mods varchar(255))' );
		sqlite_close( $db );
	}
	else
	{
		echo $e;
	}
	
?>