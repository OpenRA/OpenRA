<?php
    header( 'Content-type: text/plain' );
    try
    {
        $db = new PDO('sqlite:openra.db');
        echo 'Connection to DB established.\n';
        if ($db->query('DROP TABLE servers'))
            echo 'Dropped table.\n';
        $schema = 'CREATE TABLE servers (id INTEGER PRIMARY KEY AUTOINCREMENT, name varchar(255), 
            address varchar(255) UNIQUE, players integer, state integer, ts integer, map varchar(255), mods varchar(255))';
        if ($db->query($schema))
            echo 'Created table.';
        $db = null;
    }
    catch (PDOException $e)
    {
        echo $e->getMessage();
    }
?>