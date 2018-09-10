#!/usr/bin/python3

import glob
import os
import re
import sys

import matplotlib.pyplot as pl
import numpy as np


root_dir = os.path.join(os.path.dirname(sys.argv[0]), '..', '..')

rf = re.compile(r""".*NMI-(.*)x(.*)\.log""")
rl = re.compile(r"""(.*) @ (.*)ms""")

all_markers = ('s', '^', 'o', '*', 'p', 'X', 'd', '$\\bigcirc$')


def extract(lines):
    for l in lines:
        res = rl.match(l)
        if res is not None:
            yield (float(res.groups()[0]), float(res.groups()[1]) / 1000.0)


files = glob.glob(os.path.join(root_dir, 'logs', 'NMI-*.log'))
NMI, TIM, TMAX = dict(), dict(), dict()
for fname in files:
    print('> Reading %s' % fname)
    with open(fname, 'r') as f:
        res = rf.match(fname).groups()
        res = [float(res[0]), float(res[1])]
        if 1 < res[0] < 2:
            continue
        nmi, time = zip(*list(extract(f.readlines())))
        if res[1] > 5:
            TMAX[res[0]] = time
            continue
        if res[1] in NMI:
            NMI[res[1]][res[0]] = nmi
            TIM[res[1]][res[0]] = time
        else:
            NMI[res[1]] = {res[0]: nmi}
            TIM[res[1]] = {res[0]: time}

mus = list(NMI.values())[0]
mus = sorted(mus.keys())
thetas = sorted(NMI.keys())
tdelta = 0.25
mdelta = 0.5

all_markers = dict(zip(mus, all_markers))



p = pl.figure(1, figsize=(17, 5))
p_nmi = p.add_subplot(1, 1, 1)
# p_nmi.grid(ls='dotted', alpha=0.6, which='both')
p_nmi.set_ylabel('Mean NMI', fontsize=25)
p_nmi.set_xlabel(u"Pattern-Sampling Factor (\u03B8)", fontsize=25)

for mu in mus:
    p_nmi.plot(thetas, [np.mean(NMI[t][mu]) for t in thetas], ls='-',
               marker=all_markers[mu], ms=12, lw=1, alpha=0.85)
p_nmi.plot(thetas, [np.median(NMI[t][4.0]) for t in thetas],
           ls='--', dashes=(5, 5), marker=all_markers[4.0],
           c='C'+str(mus.index(4.0)), ms=9, lw=3, alpha=0.8)

p_nmi.plot([1.25], [np.mean(NMI[1.25][4.0])],
           clip_on=False, c='black', marker='$\\bigcirc$', ms=24, alpha=0.8)

p_nmi.tick_params(axis='both', which='major', labelsize=20)
p_nmi.xaxis.set_ticks(np.arange(min(thetas), max(thetas)+tdelta, tdelta))

leg = p_nmi.legend(
    mus, title=u"String-Sampling Factor (\u03bc)", fontsize=20, ncol=2)
leg.get_title().set_fontsize('22')

plot_path = os.path.join(root_dir, 'plots', 'Fig.18__accuracy_vs_sampling.pdf')
pl.savefig(plot_path, bbox_inches='tight')
print('> "Accuracy w.r.t Sampling" plot saved to %s' % plot_path)



p = pl.figure(2, figsize=(12, 6.5))
p_nmi = p.add_subplot(1, 1, 1)
# p_nmi.grid(ls='dotted', alpha=0.6, which='both')
p_nmi.set_ylabel('Mean Profiling  Time (s)', fontsize=25)
p_nmi.set_xlabel(u"Pattern-Sampling Factor (\u03B8)", fontsize=25)

c = -1
for mu in mus:
    c += 1
    if int(mu) != mu:
        continue
    p_nmi.plot(thetas, [np.mean(TIM[t][mu]) for t in thetas], ls='-',
               marker=all_markers[mu], c='C%d' % c, ms=12, lw=1, alpha=0.8)
c = -1
for mu in mus:
    c += 1
    if int(mu) != mu:
        continue
    p_nmi.axhline(y=np.mean(TMAX[mu]), ls=':', c='C%s' %
                  c, lw=4, alpha=0.85, dashes=(1, 3))
    p_nmi.plot([max(thetas)+0.06], [np.mean(TMAX[mu])], clip_on=False,
               marker=all_markers[mu], c='C%s' % c, ms=12, alpha=0.7)
p_nmi.plot(thetas, [np.median(TIM[t][4.0]) for t in thetas], ls='--',
           marker=all_markers[4.0], c='C'+str(mus.index(4.0)), ms=9, lw=3,
           alpha=0.8, dashes=(5, 5))

p_nmi.plot([1.25], [np.mean(TIM[1.25][4.0])],
           clip_on=False, c='black', marker='$\\bigcirc$', ms=24, alpha=0.8)

p_nmi.axis([min(thetas)-0.025, max(thetas)+0.025, 1.5, 12.5])
p_nmi.tick_params(axis='both', which='major', labelsize=20)
p_nmi.yaxis.set_ticks(np.arange(1.5, 12.5, 1))
p_nmi.xaxis.set_ticks(np.arange(min(thetas), max(thetas)+tdelta, tdelta))

leg = p_nmi.legend([m for m in mus if int(m) == m],
                   title=u"String-Sampling Factor (\u03bc)",
                   fontsize=18, ncol=2)
leg.get_title().set_fontsize('20')

plot_path = os.path.join(
    root_dir, 'plots', 'Fig.21(a)__performance_vs_sampling.pdf')
pl.savefig(plot_path, bbox_inches='tight')
print('> "Profiling Time w.r.t Sampling" plot saved to %s' % plot_path)



p = pl.figure(3, figsize=(5, 6.5))
p_nmi = p.add_subplot(1, 1, 1)
p_nmi.tick_params(axis='both', which='major', labelsize=20)
# p_nmi.grid(ls='dotted', alpha=0.75, which='both')
p_nmi.set_ylabel(u"Mean Speed Up over \u03bc = 1", fontsize=25)
p_nmi.set_xlabel(u"Mean NMI", fontsize=25)

baseTMAX = TMAX[1]

for m in mus:
    nmis = np.unique(sorted([np.mean(NMI[t][m]) for t in thetas]))
    tims = {np.mean(NMI[t][m]): np.mean([maxt/tim for (maxt, tim) in zip(baseTMAX, TIM[t][m])])
            for t in thetas}
    p_nmi.plot(nmis, [tims[n] for n in nmis], ls='-',
               marker=all_markers[m], ms=12, lw=2, alpha=0.8)

p_nmi.plot([np.mean(NMI[1.25][4.0])], [np.mean([maxt/tim for (maxt, tim) in zip(baseTMAX, TIM[1.25][4.0])])],
           clip_on=False, c='black', marker='$\\bigcirc$', ms=24, alpha=0.8)

#p_nmi.set_ylim([0.9, 3.3])
p_nmi.yaxis.set_ticks(np.arange(1.00, 5.00, 0.5))
p_nmi.xaxis.set_ticks(list(np.arange(0.75, 1.0, 0.1)) + [1])

# leg = p_nmi.legend(mus, title=u"String-Sampling Factor (\u03bc)", fontsize=20, ncol=2, loc='lower left')
# leg.get_title().set_fontsize('22')

plot_path = os.path.join(
    root_dir, 'plots', 'Fig.21(b)__performance_vs_accuracy.pdf')
pl.savefig(plot_path, bbox_inches='tight')
print('> "Performance ~ Accuracy Trade-off" plot saved to %s' % plot_path)
