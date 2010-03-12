<?php

	if ($db = sqlite_open('openra.db', 0666, $e))
	{
		echo 'sqlite_open ok.';
		sqlite_query( $db, 'CREATE TABLE servers (name varchar(255), address varchar(255), players integer, state integer, ts integer)' );
		sqlite_close( $db );
	}
	else
	{
		echo $e;
	}
	
?>