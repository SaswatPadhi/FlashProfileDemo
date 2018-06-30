#!/usr/bin/python3

import jellyfish as jelly
import numpy as np
import os
import sys

from sklearn.externals import joblib
from sklearn.metrics import auc, precision_recall_curve

from train import get_features


def main(args):
    pairs = []
    features, labels = [], []
    dist_predictions = []

    val = {'True': 1, 'False': 0}
    sys.stdout.write('> Computing features for test data ...')
    with open(args['flashprofile_output'], 'r') as f:
        val = {'True': 1, 'False': 0}
        data = f.read().split('\n')[:-1]
        dist_predictions.append((
            'FlashProfile',
            np.fromiter((float(s.split(' :: ')[0].split(' @ ')[1])
                         for s in data[::3]), float)))
        labels.extend(val[s.split('|')[0].strip()] for s in data[::3])
        strings = iter(s[11:-1] for (i, s) in enumerate(data) if i % 3 > 0)
        for s1, s2 in zip(strings, strings):
            features.append(get_features(s1, s2))
            pairs.append((s1, s2))
    print('\r> Feature vector computation DONE (on %d points)\n' % len(pairs))

    dist_predictions.append(('JaroWinkler', [jelly.jaro_winkler(*p) for p in pairs]))

    for pair in args['sim-dis-combination']:
        num_sim_pairs, num_dis_pairs = pair.split(',')
        num_sim_pairs, num_dis_pairs = int(num_sim_pairs), int(num_dis_pairs)
        model = joblib.load(os.path.join(
            args['root_dir'], 'logs',
            'RandomForest.%d.%d.pkl' % (num_sim_pairs, num_dis_pairs)))
        dist_predictions.append((
            'RF.%d.%d' % (num_sim_pairs, num_dis_pairs),
            model.predict(features)))

    for (dfile, predictions) in dist_predictions:
        with open(os.path.join(args['root_dir'], 'logs', 'Similarity.%sPR.log' % dfile), 'w') as f:
            f.write('precision\trecall\n')
            precision, recall, _ = precision_recall_curve(labels, predictions)
            for pr in zip(precision, recall):
                f.write('%f\t%f\n' % pr)
        vauc = auc(recall, precision, reorder=True)
        print('AUC(%s) = %f' % (dfile, vauc))


if __name__ == '__main__':
    root_dir = os.path.join(os.path.dirname(sys.argv[0]), '..', '..')

    import argparse
    parser = argparse.ArgumentParser(prog='test')
    parser.add_argument('-f', '--flashprofile-output',
                        default=os.path.join(root_dir, 'logs',
                                             'Similarity.FlashProfile.log'))
    parser.add_argument('sim-dis-combination', nargs='+',
                        help='Comma-separated pairs')
    args = parser.parse_args()
    args.root_dir = root_dir

    main(args.__dict__)
