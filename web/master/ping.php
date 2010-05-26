<?php
    header( 'Content-type: text/plain' );
    try 
    {
        $db = new PDO('sqlite:openra.db');
        $addr = $_SERVER['REMOTE_ADDR'] . ':' . $_REQUEST['port'];
        
        $insert = $db->prepare('INSERT OR REPLACE INTO servers 
            (name, address, players, state, ts, map, mods) 
            VALUES (:name, :addr, :players, :state, :time, :map, :mods)');
        $insert->bindValue(':name', $_REQUEST['name'], PDO::PARAM_STR);
        $insert->bindValue(':addr', $addr, PDO::PARAM_STR);
        $insert->bindValue(':players', $_REQUEST['players'], PDO::PARAM_INT);
        $insert->bindValue(':state', $_REQUEST['state'], PDO::PARAM_INT);
        $insert->bindValue(':time', time(), PDO::PARAM_INT);
        $insert->bindValue(':map', $_REQUEST['map'], PDO::PARAM_STR);
        $insert->bindValue(':mods', $_REQUEST['mods'], PDO::PARAM_STR);
        
        $insert->execute();

        if (isset( $_REQUEST['new']))
        {
            $select = $db->prepare('SELECT id FROM servers WHERE address = :addr');
            $select->bindValue(':addr', $addr, PDO::PARAM_STR);

            $select->execute();

            echo (int)$select->fetchColumn();
    
            $games = file_get_contents("../games.txt");
            file_put_contents("../games.txt", $games + 1);
        }

        $db = null;
    }
    catch (PDOException $e)
    {
        echo $e->getMessage();
    }
?>