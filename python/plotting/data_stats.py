#!/usr/bin/python3

import json
import os
import sys

import numpy as np
import matplotlib.pyplot as pl

from operator import itemgetter
data = []

root_dir = os.path.join(os.path.dirname(sys.argv[0]), '..', '..')


def load_data_from(src):
    global data

    src = os.path.join(root_dir, src)
    for root, dirs, filenames in os.walk(src):
        for f in filenames:
            fullpath = os.path.join(src, f)
            with open(fullpath, encoding="utf8") as json_file:
                j = [len(s) for s in json.load(json_file)['Data']]
            data.append({
                'strings': len(j),
                'min_len': np.min(j),
                'med_len': np.median(j),
                'avg_len': np.mean(j),
                'max_len': np.max(j),
            })


print('> Loading all test cases: ...')

load_data_from('tests/hetero')
load_data_from('tests/homo')
load_data_from('tests/homo.simple')

data = sorted(data, key=itemgetter('strings'))

px = pl.figure(figsize=(13, 5.5))
sp = px.add_subplot(2, 1, 1)
sp.set_ylabel(' Dataset Size', fontsize=22)
sp.set_yscale('log', basey=2)
sp.tick_params(axis='both', which='major', labelsize=18)
sp.grid(ls='dotted', alpha=0.75, which='both')

pl.setp(sp.get_xticklabels(), visible=False)

str_data = [e['strings'] for e in data]
str_data_med = np.mean(str_data)
print('> Minimum number of strings: %d' % str_data[0])
# print(str_data_med)
print('> Maximum number of strings: %d' % str_data[-1])
sp.bar(np.arange(len(data)), str_data, width=0.85, color='green', alpha=0.32)
#sp.hlines([str_data_med] * len(data), 0, len(data), lw=2, color='blue')

sp.set_ylim(ymin=0.5, ymax=2.4e6)
sp.set_xlim(xmin=-1, xmax=76)

sp.set_yticks([2**i for i in range(0, 21, 4)])
sp.set_xticks(np.arange(0, 80, 5))

sp.legend(['Number of strings'], fontsize=21, loc=2)

#px = pl.figure(figsize=(12,4))
sp = px.add_subplot(2, 1, 2, sharex=sp)
sp.set_ylabel('String Length', fontsize=22)
sp.set_xlabel('Dataset Id (sorted by size)', fontsize=22)
sp.set_yscale('log', basey=2, nonposy='clip')
sp.tick_params(axis='both', which='major', labelsize=18)
sp.grid(ls='dotted', alpha=0.75, which='both')

med_data = [e['med_len'] for e in data]
sp.bar(np.arange(len(data)), med_data, width=0.85, alpha=0.32, color='blue')
leg = sp.legend(['Median lengths'], fontsize=21, loc=2)

min_data = [e['min_len'] for e in data]
max_data = [e['max_len'] for e in data]
sp.plot(np.arange(len(data)), min_data, color='blue',
        lw=0, marker='.', ms=2, label='_nolegend_')
sp.vlines(np.arange(len(data)), min_data, max_data, lw=1.5, color='blue')


sp.set_ylim(ymin=0.75, ymax=3500)
sp.set_xlim(xmin=-1, xmax=76)

sp.set_yticks([2**i for i in range(1, 12, 2)])
sp.set_xticks(np.arange(0, 80, 5))

sp.legend(['Range of lengths'], fontsize=21, loc=1)
pl.gca().add_artist(leg)

pl.subplots_adjust(hspace=0.06)

plot_path = os.path.join(root_dir, 'plots', 'Fig.15__data_stats.pdf')
pl.savefig(plot_path, bbox_inches='tight')
print('> "Dataset Stats" plot saved to: %s' % plot_path)
