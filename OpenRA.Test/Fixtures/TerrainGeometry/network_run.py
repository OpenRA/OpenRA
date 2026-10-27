"""Two real clients with the normal lockstep protocol, private Support directories."""
import json,shutil,socket,subprocess,time
from pathlib import Path


def run_network(command,support,out,private,env,check_inputs,trace_name='terrain.jsonl'):
    check_inputs()
    second=out/'Support-client2';shutil.copytree(support,second,symlinks=True)
    with socket.socket() as sock:
        sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
    processes=[];logs=[];commands=[];clients=[support,second];started=time.monotonic()
    try:
        for index,directory in enumerate(clients):
            cmd=[x for x in command if not x.startswith(('Launch.Map=','Engine.SupportDir='))]
            cmd+=['Engine.SupportDir='+str(directory),'TerrainProbe.NetworkPort='+str(port),
                  'TerrainProbe.NetworkHost='+str(index==0),'Player.Name=TerrainClient'+str(index)]
            commands.append(cmd);log=open(out/('network-client'+str(index)+'.log'),'w');logs.append(log)
            processes.append(subprocess.Popen(cmd,cwd=private,env=env,stdout=log,stderr=subprocess.STDOUT))
            if index==0:
                deadline=time.monotonic()+45
                while time.monotonic()<deadline and processes[0].poll() is None:
                    try:
                        with socket.create_connection(('127.0.0.1',port),timeout=.2):break
                    except OSError:time.sleep(.2)
                else:raise RuntimeError('Network host failed to listen')
        paths=[d/'Logs'/trace_name for d in clients];events=[[],[]]
        deadline=time.monotonic()+180
        while time.monotonic()<deadline and all(p.poll() is None for p in processes):
            for i,path in enumerate(paths):
                if path.exists():events[i]=[json.loads(line) for line in path.read_text().splitlines() if line]
            if all(any(e['kind']=='complete' for e in trace) for trace in events):break
            if any(any(e['kind'] in ('exception','fail') for e in trace) for trace in events):break
            time.sleep(.3)
        checks=[sum(e['kind']=='pass' for e in trace) for trace in events]
        hashes=[[e for e in trace if e['kind']=='sync-hash'] for trace in events]
        equal=bool(hashes[0]) and hashes[0]==hashes[1]
        errors=[str(p) for d in clients for p in (d/'Logs').glob('exception*.log')]
        errors += [str(p) for d in clients for p in (d/'Logs').glob('syncreport*')]
        passed=all(any(e['kind']=='complete' for e in trace) and not any(e['kind'] in ('fail','exception') for e in trace) for trace in events) and equal and events[0]==events[1] and not errors
        check_inputs()
        (out/'network-commands.json').write_text(json.dumps(commands,indent=2)+'\n')
        return {'passed':passed,'clients':2,'checks':checks,'syncHashTicks':len(hashes[0]),'syncHashesEqual':equal,
                'eventsEqual':events[0]==events[1],'exceptions':errors,'traces':[str(p) for p in paths],
                'elapsedSeconds':round(time.monotonic()-started,2)}
    finally:
        for p in processes:
            if p.poll() is None:p.terminate()
        for p in processes:
            try:p.wait(timeout=10)
            except subprocess.TimeoutExpired:p.kill();p.wait()
        for log in logs:log.close()
