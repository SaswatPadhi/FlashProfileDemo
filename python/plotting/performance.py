#!/usr/bin/python3

import os
import sys
import xmltodict

import numpy as np
import matplotlib.pyplot as pl

from jsonpath_rw import parse
from operator import itemgetter

root_dir = os.path.join(os.path.dirname(sys.argv[0]), '..', '..')

with open(os.path.join(root_dir, 'TestResult.xml'), 'r') as f:
    j = xmltodict.parse(f.read(), force_cdata=True)

testcase_path = parse('$.."test-case"')
testcases = testcase_path.find(j)[0].value

data = []
for t in testcases:
    name = os.path.splitext(os.path.basename(t['@name']))[0]
    duration = t['@duration']
    clusters, avg_len, entries, auto = [e.split('=')[1]
                                        for e in t['output']['#text'].split(',')]

    entry = {'name': name,
             'duration': float(duration),
             'clusters': int(clusters),
             'avg_len': float(avg_len),
             'entries': int(entries),
             'auto': auto == 'True'}
    data.append(entry)

print('> Number of tasks :: %3d' % len(data))
print('> Number of tasks that took <= 2s :: %3d' %
      (sum(e['duration'] <= 2 for e in data)))
print('> Max time for all tasks :: %0.3f s' %
      (np.max([e['duration'] for e in data])))
print('> Median time for all tasks :: %0.3f s' %
      (np.median([e['duration'] for e in data])))
print('')

print('> Number of AUTO tasks :: %3d' %
      len([1 for e in data if e['auto']]))
print('> Number of AUTO tasks that took <= 2s :: %3d' %
      (sum(e['duration'] <= 2 for e in data if e['auto'])))
print('> Max time for AUTO tasks :: %0.3f s' %
      (np.max([e['duration'] for e in data if e['auto']])))
print('> Median time for AUTO tasks :: %0.3f s' %
      (np.median([e['duration'] for e in data if e['auto']])))
print('')

print('> Number of REFINE tasks :: %3d' %
      len([1 for e in data if not e['auto']]))
print('> Number of REFINE tasks that took <= 2s :: %3d' %
      (sum(e['duration'] <= 2 for e in data if not e['auto'])))
print('> Max time for REFINE tasks :: %0.3f s' %
      (np.max([e['duration'] for e in data if not e['auto']])))
print('> Median time for REFINE tasks :: %0.3f s' %
      (np.median([e['duration'] for e in data if not e['auto']])))
print('')

l_dat = sorted(data, key=itemgetter('avg_len'))

px = pl.figure(figsize=(12, 4))
sp = px.add_subplot(1, 1, 1)
sp.set_yscale('log', basey=2)
sp.set_xscale('log', basex=2)
sp.set_ylabel('Profiling Time (s)', fontsize=25)
sp.set_xlabel('Number of Strings in Dataset', fontsize=25)
sp.tick_params(axis='both', which='major', labelsize=20)
sp.grid(ls='dotted', alpha=0.75, which='both')

args = {'marker': 'x', 'mew': 2, 'ms': 9, 'ls': '--', 'lw': 0, 'alpha': 0.8}
auto_args = {'marker': 'o', 'ms': 5, 'ls': '--', 'lw': 0, 'alpha': 0.8}
title_yoffset = -0.25

x = [e['entries'] for e in l_dat if not e['auto']]
y = [e['duration'] for e in l_dat if not e['auto']]
sp.plot(x, y, 'r', **args)

x = [e['entries'] for e in l_dat if e['auto']]
y = [e['duration'] for e in l_dat if e['auto']]
sp.plot(x, y, 'r', **auto_args)

sp.axhline(y=2, ls='-', lw=4, c=(0, 0.8, 0), alpha=0.6)
sp.set_yticks([2**i for i in range(-9, 11, 2)])

leg = sp.legend(['Automatic', 'Refinement'], fontsize=20)

plot_path = os.path.join(root_dir, 'plots', 'Fig.21__time-vs-strings.pdf')
pl.savefig(plot_path, bbox_inches='tight')
print('> "Profiling Time vs Dataset Size" plot saved to %s' % plot_path)


px = pl.figure(figsize=(12, 4))
sp = px.add_subplot(1, 1, 1)
sp.set_yscale('log', basey=2)
sp.set_xscale('log', basex=2)
sp.set_ylabel('Profiling Time (s)', fontsize=25)
sp.set_xlabel('Avg (Length of String) over Dataset', fontsize=25)
sp.tick_params(axis='both', which='major', labelsize=20)
sp.grid(ls='dotted', alpha=0.75, which='both')

x = [e['avg_len'] for e in l_dat if not e['auto']]
y = [e['duration'] for e in l_dat if not e['auto']]
sp.plot(x, y, 'ro', **args)

x = [e['avg_len'] for e in l_dat if e['auto']]
y = [e['duration'] for e in l_dat if e['auto']]
sp.plot(x, y, 'ro', **auto_args)

sp.axhline(y=2, ls='-', lw=4, c=(0, 0.8, 0), alpha=0.6)
sp.set_yticks([2**i for i in range(-9, 11, 2)])

leg = sp.legend(['Automatic', 'Refinement'], fontsize=20)

plot_path = os.path.join(root_dir, 'plots', 'Fig.22__time-vs-length.pdf')
pl.savefig(plot_path, bbox_inches='tight')
print('> "Profiling Time vs String Length" plot saved to %s' % plot_path)
