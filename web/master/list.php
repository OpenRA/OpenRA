<?php
    header( 'Content-type: text/plain' );

    try
    {
        $db = new PDO('sqlite:openra.db');
        $stale = 60 * 5;
        $result = $db->query('SELECT * FROM servers WHERE (' . time() . ' - ts < ' . $stale . ')');
	$n = 0;
        foreach ( $result as $row )
        {
            echo "Game@" . $n++ . ":\n";
            echo "\tId: " . $row['id'] . "\n";
            echo "\tName: " . $row['name'] . "\n";
            echo "\tAddress: " . $row['address'] . "\n";
            echo "\tState: " . $row['state'] . "\n";
            echo "\tPlayers: " . $row['players'] . "\n";
            echo "\tMap: " . $row['map'] . "\n";
            echo "\tMods: " . $row['mods'] . "\n";
            echo "\tTTL: " . ($stale - (time() - $row['ts'])) . "\n";
        }
        $db = null;
    }
    catch (PDOException $e)
    {
        echo $e->getMessage();
    }
?>