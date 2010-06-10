<?php

    /**
        Diff implemented in pure php, written from scratch.
        Copyright (C) 2003  Daniel Unterberger <diff.phpnet@holomind.de>
        Copyright (C) 2005  Nils Knappmeier next version 
        
        This program is free software; you can redistribute it and/or
        modify it under the terms of the GNU General Public License
        as published by the Free Software Foundation; either version 2
        of the License, or (at your option) any later version.
        
        This program is distributed in the hope that it will be useful,
        but WITHOUT ANY WARRANTY; without even the implied warranty of
        MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
        GNU General Public License for more details.
        
        You should have received a copy of the GNU General Public License
        along with this program; if not, write to the Free Software
        Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
        
        http://www.gnu.org/licenses/gpl.html

        About:
        I searched a function to compare arrays and the array_diff()
        was not specific enough. It ignores the order of the array-values.
        So I reimplemented the diff-function which is found on unix-systems
        but this you can use directly in your code and adopt for your needs.
        Simply adopt the formatline-function. with the third-parameter of arr_diff()
        you can hide matching lines. Hope someone has use for this.

        Contact: d.u.diff@holomind.de <daniel unterberger>
    **/

    
## PHPDiff returns the differences between $old and $new, formatted
## in the standard diff(1) output format.
function PHPDiff($old,$new) 
{
   # split the source text into arrays of lines
   $t1 = explode("\n",$old);
   $x=array_pop($t1); 
   if ($x>'') $t1[]="$x\n\\ No newline at end of file";
   $t2 = explode("\n",$new);
   $x=array_pop($t2); 
   if ($x>'') $t2[]="$x\n\\ No newline at end of file";

   # build a reverse-index array using the line as key and line number as value
   # don't store blank lines, so they won't be targets of the shortest distance
   # search
   foreach($t1 as $i=>$x) if ($x>'') $r1[$x][]=$i;
   foreach($t2 as $i=>$x) if ($x>'') $r2[$x][]=$i;

   $a1=0; $a2=0;   # start at beginning of each list
   $actions=array();

   # walk this loop until we reach the end of one of the lists
   while ($a1<count($t1) && $a2<count($t2)) {
     # if we have a common element, save it and go to the next
     if ($t1[$a1]==$t2[$a2]) { $actions[]=4; $a1++; $a2++; continue; } 

     # otherwise, find the shortest move (Manhattan-distance) from the
     # current location
     $best1=count($t1); $best2=count($t2);
     $s1=$a1; $s2=$a2;
     while(($s1+$s2-$a1-$a2) < ($best1+$best2-$a1-$a2)) {
       $d=-1;
       foreach((array)@$r1[$t2[$s2]] as $n) 
         if ($n>=$s1) { $d=$n; break; }
       if ($d>=$s1 && ($d+$s2-$a1-$a2)<($best1+$best2-$a1-$a2))
         { $best1=$d; $best2=$s2; }
       $d=-1;
       foreach((array)@$r2[$t1[$s1]] as $n) 
         if ($n>=$s2) { $d=$n; break; }
       if ($d>=$s2 && ($s1+$d-$a1-$a2)<($best1+$best2-$a1-$a2))
         { $best1=$s1; $best2=$d; }
       $s1++; $s2++;
     }
     while ($a1<$best1) { $actions[]=1; $a1++; }  # deleted elements
     while ($a2<$best2) { $actions[]=2; $a2++; }  # added elements
  }

  # we've reached the end of one list, now walk to the end of the other
  while($a1<count($t1)) { $actions[]=1; $a1++; }  # deleted elements
  while($a2<count($t2)) { $actions[]=2; $a2++; }  # added elements

  # and this marks our ending point
  $actions[]=8;

  # now, let's follow the path we just took and report the added/deleted
  # elements into $out.
  $op = 0;
  $x0=$x1=0; $y0=$y1=0;
  $out = array();
  foreach($actions as $act) {
    if ($act==1) { $op|=$act; $x1++; continue; }
    if ($act==2) { $op|=$act; $y1++; continue; }
    if ($op>0) {
      $xstr = ($x1==($x0+1)) ? $x1 : ($x0+1).",$x1";
      $ystr = ($y1==($y0+1)) ? $y1 : ($y0+1).",$y1";
      if ($op==1) $out[] = "{$xstr}d{$y1}";
      elseif ($op==3) $out[] = "{$xstr}c{$ystr}";
      while ($x0<$x1) { $out[] = '< '.$t1[$x0]; $x0++; }   # deleted elems
      if ($op==2) $out[] = "{$x1}a{$ystr}";
      elseif ($op==3) $out[] = '---';
      while ($y0<$y1) { $out[] = '> '.$t2[$y0]; $y0++; }   # added elems
    }
    $x1++; $x0=$x1;
    $y1++; $y0=$y1;
    $op=0;
  }
  $out[] = '';
  return join("\n",$out);
}

    
?>

<?php
function sdir( $path='.', $mask='*', $nocache=0 ){
    static $dir = array(); // cache result in memory
    if ( !isset($dir[$path]) || $nocache) {
        $dir[$path] = scandir($path);
    }
    foreach ($dir[$path] as $i=>$entry) {
        if ($entry!='.' && $entry!='..' && fnmatch($mask, $entry) ) {
            $sdir[] = $entry;
        }
    }
    return ($sdir);
}
?>

<?php 
$post_file = fopen('compress.zlib://php://input', 'rb');

$game_id = $_SERVER['HTTP_GAME_ID'];

mkdir($game_id);

$client_hash = md5($_SERVER['REMOTE_ADDR']);

$post_data = '';

while (!feof($post_file))
    $post_data .= fread($post_file, 8192);

if ($_SERVER['HTTP_DIFF']) {
  foreach (sdir($game_id, '*.'.$_SERVER['HTTP_CHANNEL'].'.log') as $f) {
    $hash = substr($f, 0, strpos($f, '.'));
    $diff = PHPDiff(file_get_contents($game_id . '/' . $f), $post_data);
    file_put_contents($game_id .'/'. $hash .'.'. $client_hash .'.'. $_SERVER['HTTP_CHANNEL'] . '.diff', $diff);
  }
}

file_put_contents($game_id .'/'. $client_hash . '.' . $_SERVER['HTTP_CHANNEL'] . '.log',$post_data);

fclose($post_file);
?>
