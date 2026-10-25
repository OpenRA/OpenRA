#!/usr/bin/env python3
# Copyright (c) The OpenRA Developers and Contributors. GPL-3.0-or-later.
"""Prepare two isolated TS maps using terrain assets from the stock mod."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--engine', type=Path, required=True)
parser.add_argument('--content', type=Path, required=True)
parser.add_argument('--out', type=Path, required=True, help='New isolated Support directory')
parser.add_argument('--case', choices=['direct', 'movie', 'missing', 'loss', 'restart', 'locked', 'unsupported'], default='direct')
parser.add_argument('--movie', type=Path, help='An installed VQA file, required for the movie case')
args = parser.parse_args()
engine = args.engine.resolve()
fixtures = Path(__file__).resolve().parent
support = args.out.resolve()
if args.case == 'movie' and (not args.movie or not args.movie.is_file()):
    parser.error('--case movie requires an existing --movie VQA file')
support.mkdir(parents=True, exist_ok=False)
if args.case == 'movie':
    overlay = support / 'Content/ts'
    overlay.mkdir(parents=True)
    for entry in (args.content.resolve() / 'ts').iterdir():
        if entry.name != 'progression-probe.vqa':
            (overlay / entry.name).symlink_to(entry, target_is_directory=entry.is_dir())
    shutil.copy2(args.movie, overlay / 'progression-probe.vqa')
else:
    (support / 'Content').symlink_to(args.content.resolve(), target_is_directory=True)
(support / 'settings.yaml').write_text('Game:\n\tIntroductionPromptVersion: 2\nDebug:\n\tSystemInformationVersionPrompt: 6\n')
version = (engine / 'VERSION').read_text().strip()
first = support / 'maps/ts' / version / 'progression-first'
second = support / 'maps/ts' / version / 'progression-second'
for stage, directory in [('first', first), ('second', second)]:
    directory.mkdir(parents=True)
    for name in ['map.bin', 'map.png']:
        shutil.copy2(engine / 'mods/ts/maps/fields-of-green' / name, directory / name)
    text = (fixtures / 'map.yaml.in').read_text()
    values = {'STAGE': stage, 'DEFAULT': 'hard' if stage == 'first' else 'normal',
              'LOCKED': 'True' if stage == 'second' and args.case == 'locked' else 'False',
              'HARD_VALUE': '' if stage == 'second' and args.case == 'unsupported' else '\t\t\t\thard: probe-hard'}
    for key, value in values.items():
        text = text.replace('@' + key + '@', value)
    (directory / 'map.yaml').write_text(text)
    (directory / 'probe.ftl').write_text('probe-difficulty = Difficulty\nprobe-normal = Normal\nprobe-hard = Hard\n')
(second / 'mission.lua').write_text((fixtures / 'second.lua').read_text())
env = dict(os.environ, ENGINE_DIR=str(engine))
uid = subprocess.check_output(['dotnet', str(engine / 'bin/OpenRA.Utility.dll'), 'ts', '--map-hash', str(second)], cwd=engine, env=env, text=True).strip().splitlines()[-1]
text = (fixtures / 'first.lua').read_text().replace('@CASE@', args.case).replace('@NEXT_UID@', uid)
(first / 'mission.lua').write_text(text)
print(json.dumps({'first': str(first), 'second': str(second), 'next_uid': uid, 'case': args.case}))
