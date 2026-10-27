#!/usr/bin/env python3
"""Native stock-engine terrain fixture. Private mod/content links; never installs."""
import argparse,hashlib,json,os,shutil,struct,subprocess,time
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--large-map',action='store_true');p.add_argument('--network',action='store_true');p.add_argument('--engine',type=Path,required=True);p.add_argument('--content',type=Path,required=True)
p.add_argument('--out',type=Path,required=True);p.add_argument('--probe',type=Path);p.add_argument('--baseline',action='store_true');p.add_argument('--cold',action='store_true')
a=p.parse_args()
if a.network and a.cold:p.error('--network and --cold are separate runs')
engine=a.engine.resolve();out=a.out.resolve();out.mkdir(parents=True,exist_ok=False)
fixture=Path(__file__).resolve().parent
probe=a.probe or fixture/'bin/Release/OpenRA.TerrainProbe.dll'
private=out/'engine';private.mkdir()
for item in engine.iterdir():
 if item.name!='mods':(private/item.name).symlink_to(item,target_is_directory=item.is_dir())
mods=private/'mods';mods.mkdir()
for name in ('common','common-content','ts-content'):
 if(engine/'mods'/name).exists():(mods/name).symlink_to(engine/'mods'/name,target_is_directory=True)
mod=mods/'terrainprobe';shutil.copytree(engine/'mods/ts',mod,symlinks=True)
support=out/'Support';support.mkdir();(support/'Content').symlink_to(a.content.resolve(),target_is_directory=True)
shutil.copy2(probe,support/probe.name)
manifest=(mod/'mod.yaml').read_text().replace('$ts: ts','$terrainprobe: ts').replace('maps/ts/','maps/terrainprobe/')
manifest=manifest.replace('Assemblies: OpenRA.Mods.Common.dll, OpenRA.Mods.Cnc.dll','Assemblies: OpenRA.Mods.Common.dll, OpenRA.Mods.Cnc.dll, '+str(support/probe.name))
manifest=manifest.replace('LoadScreen: LogoStripeLoadScreen','LoadScreen: TerrainProbeLoadScreen')
(mod/'mod.yaml').write_text(manifest)
(support/'settings.yaml').write_text('Game:\n\tIntroductionPromptVersion: 2\nDebug:\n\tSystemInformationVersionPrompt: 6\nSound:\n\tEngine: null\n')
mapdir=support/'maps/terrainprobe/{DEV_VERSION}/terrain-probe';mapdir.mkdir(parents=True)
width,height=(128,256) if a.large_map else (60,120);n=width*height
(mapdir/'map.bin').write_bytes(struct.pack('<BHHIII',2,width,height,17,17+3*n,17+4*n)+b'\0\0\0'*n+bytes([4])*n+b'\0\0'*n)
shutil.copy2(engine/'mods/ts/maps/fields-of-green/map.png',mapdir/'map.png')
(mapdir/'map.yaml').write_text('''MapFormat: 12
RequiresMod: terrainprobe
Title: Dynamic terrain regression
Author: OpenRA regression fixture
Tileset: TEMPERATE
MapSize: 60,120
Bounds: 2,4,56,110
Visibility: Lobby
Categories: Campaign, Diagnostic
Players:
\tPlayerReference@Neutral:
\t\tName: Neutral
\t\tOwnsWorld: True
\t\tNonCombatant: True
\t\tFaction: gdi
\tPlayerReference@GDI:
\t\tName: GDI
\t\tFaction: gdi
\t\tPlayable: True
\t\tAllowBots: False
Actors:
\tCamera: camera
\t\tOwner: GDI
\t\tLocation: 42,6
Rules:
\tWorld:
\t\t-CrateSpawner:
\t\t-SpawnStartingUnits:
\t\t-AutoSave:
\t\t-MapStartingLocations:
\t\t-StartGameNotification:
\t\tMapOptions:
\t\t\tGameSpeed: fastest
\t\t\tGameSpeedDropdownLocked: True
\t\tTerrainRuntimeProbe:
\t\tMutableProbeLayer:
\t\tSpriteOverrideProbe:
\tE1:
\t\tRevealsShroud:
\t\t\tRange: 8c0
\t\t\tMaxHeightDelta: 0
\tCAMERA:
\t\t-RevealsShroud:
\tPOLICYUNIT:
\t\tInherits: E1
\t\tProbeHeightPolicy:
\tTARGETUNIT:
\t\tInherits: E1
\t\t-Armament@PRIMARY:
\t\t-Armament@ELITE:
\tPlayer:
\t\t-ConquestVictoryConditions:
''')
if a.large_map:
 text=(mapdir/'map.yaml').read_text().replace('MapSize: 60,120','MapSize: 128,256').replace('Bounds: 2,4,56,110','Bounds: 2,4,124,246')
 (mapdir/'map.yaml').write_text(text)
if a.network:
 text=(mapdir/'map.yaml').read_text().replace('Actors:\n','\tPlayerReference@GDI2:\n\t\tName: GDI2\n\t\tFaction: gdi\n\t\tPlayable: True\n\t\tAllowBots: False\nActors:\n')
 (mapdir/'map.yaml').write_text(text)
if a.baseline:
 text=(mapdir/'map.yaml').read_text();start=text.index('\tPOLICYUNIT:');end=text.index('\tPlayer:',start);(mapdir/'map.yaml').write_text(text[:start]+text[end:])
files=[mapdir/'map.yaml',mapdir/'map.bin',fixture/'NetworkProbe.cs',fixture/'network_run.py']+[engine/'bin'/name for name in ['OpenRA.dll','OpenRA.Game.dll','OpenRA.Mods.Common.dll','OpenRA.Mods.Cnc.dll','OpenRA.Platforms.Default.dll']]+[support/probe.name]
def_digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
hashes={str(p):def_digest(p)for p in files}
(out/'inputs.json').write_text(json.dumps({'binaries':hashes,'baseline':a.baseline,'cold':a.cold,'fixtureSha256':def_digest(fixture/'Probe.cs')},indent=2)+'\n')
def check_inputs():
 if any(def_digest(Path(p))!=sha for p,sha in hashes.items()):raise RuntimeError('Fixture binaries changed during verification')
env=dict(os.environ,LIBGL_ALWAYS_SOFTWARE='1',SDL_AUDIODRIVER='dummy',ALSOFT_DRIVERS='null')
xvfb=subprocess.Popen(['Xvfb','-displayfd','1','-screen','0','1280x800x24','-nolisten','tcp'],stdout=subprocess.PIPE,stderr=open(out/'xvfb.log','w'),text=True)
try:
 display=xvfb.stdout.readline().strip();assert display,'Xvfb failed';env['DISPLAY']=':'+display
 cmd=['dotnet',str(engine/'bin/OpenRA.dll'),'Engine.EngineDir='+str(private),'Engine.SupportDir='+str(support),'Game.Mod=terrainprobe','Launch.Map=terrain-probe','Graphics.Mode=Windowed','Graphics.WindowedSize=1280,800','Game.FetchNews=False','Debug.CheckVersion=False']
 (out/'command.json').write_text(json.dumps(cmd,indent=2))
 def launch(command,label):
  check_inputs()
  trace=support/'Logs/terrain.jsonl'
  with open(out/(label+'.log'),'w')as log:
   process=subprocess.Popen(command,cwd=private,env=env,stdout=log,stderr=subprocess.STDOUT)
   try:
    deadline=time.monotonic()+150;events=[];primed=False;resumed=False
    while time.monotonic()<deadline and process.poll()is None:
     if trace.exists():
      events=[json.loads(line)for line in trace.read_text().splitlines()if line]
      if events and not primed and shutil.which('xdotool'):
       windows=subprocess.check_output(['xdotool','search','--onlyvisible','--pid',str(process.pid)],env=env,text=True).split()
       if windows:
        subprocess.run(['xdotool','windowfocus','--sync',windows[0],'mousemove','640','400'],env=env,check=True,timeout=5);primed=True
      if label=='cold'and not resumed and any(not e['catchUp']for e in events):
       assert shutil.which('xdotool'),'Cold test needs xdotool to resume the normal load-game menu'
       subprocess.run(['xdotool','key','Escape'],env=env,check=True,timeout=5);resumed=True
      if any(e['kind']in('complete','exception')for e in events):break
     time.sleep(.3)
    if shutil.which('import'):subprocess.run(['import','-window','root',str(out/(label+'.png'))],env=env,timeout=15)
    if trace.exists():events=[json.loads(line)for line in trace.read_text().splitlines()if line]
    failures=[e for e in events if e['kind']in('fail','exception')]
    passed=bool(events)and any(e['kind']=='complete'for e in events)and not failures
    exceptions=list((support/'Logs').glob('exception*.log'))
    if exceptions:passed=False
    return {'passed':passed,'failures':failures,'checks':sum(e['kind']=='pass'for e in events),'events':str(trace),'engine':str(engine),'exceptions':[str(e)for e in exceptions]},events
   finally:
    if process.poll()is None:process.terminate()
    try:process.wait(timeout=10)
    except subprocess.TimeoutExpired:process.kill();process.wait()
 if a.network:
  import sys
  sys.dont_write_bytecode=True
  sys.path.insert(0,str(fixture))
  from network_run import run_network
  result=run_network(cmd,support,out,private,env,check_inputs)
  passed=result['passed'];first=[]
 else:
  result,first=launch(cmd,'runtime');passed=result['passed']
 if a.cold and passed:
  checkpoint=next((support/'Saves').rglob('terrain-checkpoint.orasav'),None)
  assert checkpoint,'No checkpoint produced'
  trace=support/'Logs/terrain.jsonl';trace.rename(out/'baseline.jsonl');result['events']=str(out/'baseline.jsonl')
  coldcmd=[arg for arg in cmd if not arg.startswith('Launch.Map=')]+['TerrainProbe.LoadSave='+str(checkpoint)]
  cold,second=launch(coldcmd,'cold');result['cold']=cold
  live=[e for e in second if not e['catchUp']]
  first_live=[e for e in first if live and e['tick']>=live[0]['tick']]
  equal=bool(live)and first_live==live
  result['coldEventsEqual']=equal;passed=passed and cold['passed']and equal
 check_inputs()
 result['passed']=passed
 (out/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))

finally:
 xvfb.terminate();xvfb.wait(timeout=10)
raise SystemExit(0 if passed else 1)
