#!/usr/bin/python3

import matplotlib.pyplot as pl
import numpy as np
import os
import sys

from sklearn.metrics import auc, precision_recall_curve

pr_auc = auc
root_dir = os.path.join(os.path.dirname(sys.argv[0]), '..', '..')

with open(os.path.join(root_dir, 'logs', 'Similarity.FlashProfile.log')) as f:
    val = {'True': 1, 'False': 0}
    data = f.read().split('\n')[:-1]
    labels = [val[s.split('|')[0].strip()] for s in data[::3]]
    times = np.fromiter((int(s.split('[')[1].split(']')[0])
                         for s in data[::3]), int)
    predictions = np.fromiter((float(s.split(' :: ')[0].split(' @ ')[1])
                               for s in data[::3]), float)
    precision, recall, _ = precision_recall_curve(labels, predictions)
auc = pr_auc(recall, precision)


def read_case(case):
    with open(os.path.join(root_dir, 'logs', 'Similarity.%sPR.log' % case), 'r') as f:
        case_precision, case_recall = zip(
            *(tuple(map(float, line.split('\t')))
              for line in f.read().split('\n')[1:] if len(line) > 0))
    return (pr_auc(case_recall, case_precision), case_recall, case_precision)


x = -0.02
y = 0.300
d = 0.0725

p = pl.figure(figsize=(9, 6))
p_auc = p.add_subplot(1, 1, 1)
p_auc.set_autoscale_on(False)
p_auc.axis([0, 1, 0, 1.025])

args = {'alpha': 0.5, 'lw': 4}
text_args = {'fontsize': 20, 'fontweight': 'bold', 'family': 'monospace'}

p_auc.tick_params(axis='both', which='major', labelsize=20)
p_auc.set_ylabel('Precision', fontsize=22)
p_auc.set_xlabel('Recall', fontsize=22)

p_auc.plot(recall, precision, color='green', ls='-', **args)

colors_data = ['brown', 'red', 'purple', 'blue']
ls = [(1, 1), (2.5, 1.25, 1, 1.25), (2, 2), (5, 2)]

base_data = [read_case(name) for name in sys.argv[1:]]
for i, (auc, recall, precision) in enumerate(base_data):
    p_auc.plot(recall, precision, color=colors_data[i], dashes=ls[i], **args)

leg = p_auc.legend(['FlashProfile'] + sys.argv[1:], fontsize=15)

plot_path = os.path.join(root_dir, 'plots', 'Fig.17(a)__Similarity.pdf')
pl.savefig(plot_path, bbox_inches='tight')
print('> "Accuracy of Similarity Prediction" plot saved to %s' % plot_path)