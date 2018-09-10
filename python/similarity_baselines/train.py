#!/usr/bin/python3

import json
import os
import random
import sys

from glob import glob
from itertools import combinations, islice, product
from sklearn.ensemble import RandomForestRegressor
from sklearn.externals import joblib


SEED = 0xfaded


def get_features(s1, s2):
    def digits(s): return sum(c.isdigit() for c in s)

    def lowers(s): return sum(c.islower() for c in s)

    def uppers(s): return sum(c.isupper() for c in s)

    def spaces(s): return sum(c.isspace() for c in s)

    def dots(s): return sum(c == '.' for c in s)

    def commas(s): return sum(c == ',' for c in s)

    def hyphens(s): return sum(c == '-' for c in s)

    def dist(f): return abs(f(s1) - f(s2))

    return [
        dist(len),
        dist(digits), dist(lowers), dist(uppers),
        dist(spaces), dist(dots), dist(commas), dist(hyphens),
        s1[0].islower() and s2[0].islower(),
        s1[0].isupper() and s2[0].isupper(),
        s1[0].isdigit() and s2[0].isdigit()
    ]


def main(args):
    random.seed(SEED)
    for pair in args['sim-dis-combination']:
        num_sim_pairs, num_dis_pairs = pair.split(',')
        num_sim_pairs, num_dis_pairs = int(num_sim_pairs), int(num_dis_pairs)
        features, labels = [], []

        files = glob(os.path.join(args['root_dir'], 'tests', 'homo', '*.json'))
        print('> +ve/-ve Ratio = %0.2f%%' %
              ((100.0 * num_sim_pairs) / (num_dis_pairs * num_dis_pairs * (len(files) - 1))))

        for i, clean_file in enumerate(files):
            sys.stdout.write('\r+ Feature vector computation @ %0.2f %%' %
                             ((100.0 * i) / len(files)))
            sys.stdout.flush()

            with open(clean_file, 'r') as f:
                data = json.load(f)['Data']
                random.shuffle(data)
                for pair in islice(combinations(data, 2), num_sim_pairs):
                    features.append(get_features(*pair))
                    labels.append(1)
                for file_2 in files:
                    if file_2 == clean_file:
                        continue
                    with open(file_2, 'r') as f:
                        data_2 = json.load(f)['Data']
                        random.shuffle(data_2)
                    for pair in product(data[:num_dis_pairs], data_2[:num_dis_pairs]):
                        features.append(get_features(*pair))
                        labels.append(0)
        sys.stdout.write('\r> Feature vector computation DONE.\n')

        sys.stdout.write('\r+ Training ... (%d data points)' % len(labels))
        sys.stdout.flush()
        model = RandomForestRegressor(random_state=SEED).fit(features, labels)
        sys.stdout.write('\r> Training DONE (%d data points).\n')

        model_file = os.path.join(os.path.join(root_dir, 'logs'),
            'RandomForest.%d.%d.pkl' % (num_sim_pairs, num_dis_pairs))
        joblib.dump(model, model_file)
        print('> Model saved to %s.\n' % model_file)


if __name__ == '__main__':
    root_dir = os.path.join(os.path.dirname(sys.argv[0]), '..', '..')

    import argparse
    parser = argparse.ArgumentParser(prog='train')
    parser.add_argument('sim-dis-combination', nargs='+', help='Comma-separated pairs')
    args = parser.parse_args()
    args.root_dir = root_dir

    main(args.__dict__)
