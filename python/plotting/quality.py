#!/usr/bin/python3

import os
import sys

import numpy as np
import matplotlib.pyplot as pl

root_dir = os.path.join(os.path.dirname(sys.argv[0]), '..', '..')

f = pl.figure(1, figsize=(15, 5))
p = f.add_subplot(1, 1, 1)
p.tick_params(axis='both', which='major', labelsize=24)
p.set_ylabel('Match Fraction', fontsize=26)
p.set_xlabel('Dataset Id', fontsize=26)


def filterLine(prefix, lines):
    for line in lines:
        if line.startswith(prefix):
            yield float(line[len(prefix):])


with open(sys.argv[1]) as f:
    data = f.readlines()
    f1 = float(data[-1].split('=')[1])
    match = list(filterLine('    + Match = ', data))
    mismatch = list(filterLine('    + Mismatch = ', data))

p.fill_between(range(len(match)), match, mismatch,
               facecolor='green', alpha=0.32)
p.fill_between(range(len(match)), mismatch, 0.0,
               facecolor='#880000', alpha=0.96)

# p.text(23, 1.04, u"F1 = %0.2f%%" % (f1 * 100), color='#cc0000', fontweight='bold', fontsize=36)
# p.text(0, 1.04, u"F1 = %0.2f%%" % (f1 * 100), color='#cc0000', fontweight='bold', fontsize=36)
p.text(23, 0.45, u"F1 = %0.2f%%" % (f1 * 100),
       color='green', fontweight='bold', fontsize=36)

p.axhline(y=np.mean(match), ls='dashed', lw=3,
          c='green', alpha=0.9, dashes=(4, 4))
p.axhline(y=np.mean(mismatch), ls='dotted', lw=3,
          c='red', alpha=0.9, dashes=(1, 3))
p.set_yticks(list(p.get_yticks())[2:] + [np.mean(match), np.mean(mismatch)])

p.set_autoscale_on(False)
p.axis([0, 62, 0, 1.005])
p.xaxis.set_ticks(np.arange(0, 62, 5))

plot_path = os.path.join(root_dir, 'plots', 'Fig.18__quality.pdf')
pl.savefig(plot_path, bbox_inches='tight')
print('> "Profiling Quality" plot saved to %s' % plot_path)
