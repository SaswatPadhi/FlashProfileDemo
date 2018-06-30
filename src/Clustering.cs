using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Matching.Text;
using Microsoft.ProgramSynthesis.Matching.Text.Semantics;
using Microsoft.ProgramSynthesis.Utils;
using Microsoft.ProgramSynthesis.Utils.Clustering;

namespace FlashProfileDemo {

    public static class Clustering {
        private static double Entropy(List<List<string>> clusters) {
            double totalPoints = clusters.Sum(list => list.Count);
            return -clusters.Select(list => list.Count / totalPoints).Sum(d => d * Math.Log(d));
        }

        private static double MutualInfo(List<List<string>> clustersA, List<List<string>> clustersB) {
            double totalPoints = clustersA.Sum(list => list.Count);
            var combos = (from c1 in clustersA
                          from c2 in clustersB
                          select EquatablePair.Create(c1.Intersect(c2).Count(), c1.Count * c2.Count));
            return combos.Sum(pair => pair.Item1 == 0 ? 0
                         : pair.Item1 * Math.Log((pair.Item1 * totalPoints) / pair.Item2)) / totalPoints;
        }

        private static double NormalizedMutualInfo(List<List<string>> clustersA,
                                                   List<List<string>> clustersB)
            => 2.0 * MutualInfo(clustersA, clustersB) / (Entropy(clustersA) + Entropy(clustersB));

        private static List<List<string>> StringClustersFromDendrograms(IEnumerable<Dendrogram<State>> dendrograms)
            => dendrograms.Select(d => d.Data.Select(s => (s[Synthesizer.SRegionSymbol] as SuffixRegion).Value).ToList()).ToList();

        public enum ahc_info { NMI, TIME };

        public static int Estimate(ClusteringOptions opts) {
            Random rnd = new Random(0xface);

            // Do a learning call and just ignore the result.
            // To warm-up PROSE. The first learning call always takes longer for some reason.
            Synthesizer.Learn(1, Synthesizer.StringToState(">)#*$&"), Synthesizer.StringToState("969dvb"));

            Stopwatch watch;
            Console.Write($"\n[+] Accuracy of recovering N ϵ [{opts.MinClusters},{opts.MaxClusters}] clusters NMI with (θ={opts.Theta},μ={opts.Mu}) @ {opts.NumStringsPerCluster} strings x {opts.TrialsPerClustering} trials ...");
            var separator = new string('=', 80);
            using (var file = File.CreateText(Path.Combine(Utils.Paths.LogsDir, $"NMI-{opts.Mu}x{opts.Theta}.log"))) {
                for (int clusters = opts.MinClusters; clusters <= opts.MaxClusters; ++clusters) {
                    Console.Write($"\nN = {clusters}:");
                    file.WriteLine($"\n\nN = {clusters} ... ");

                    var stats = new Dictionary<ahc_info, double> {
                        [ahc_info.NMI] = 0,
                        [ahc_info.TIME] = 0
                    };

                    for (int i = 1; i <= opts.TrialsPerClustering; i++) {
                        file.WriteLine($"\n\n{separator}");
                        file.Flush();

                        List<string> columns = Utils.Paths.CleanDatasets.OrderBy(s => rnd.Next()).Take(clusters).ToList();
                        List<List<string>> data = columns.Select(f => TestCase.LoadNonEmptyData(f).Distinct().OrderBy(s => rnd.Next())
                                                                              .Take(opts.NumStringsPerCluster).ToList()).ToList();

                        file.WriteLine("Data:");
                        for (int j = data.Count - 1; j >= 0; j--)
                            file.WriteLine($"  [#] {columns[j]} = {string.Join("  .-.  ", data[j])}");
                        file.Flush();

                        watch = Stopwatch.StartNew();
                        var program = Learner.Instance.Learn(data.SelectMany(d => d.Select(s => Learner.Instance.BuildPositiveConstraint(s, true, false)))
                                                                 .OrderBy(c => rnd.Next())
                                                                 .Append(new DisjunctionsLimit<string, bool>((uint)clusters, (uint)clusters))
                                                                 .Append(new AllowedTokens<string, bool>(Utils.Default.Atoms))
                                                                 .Append(new ClusteringParameters<string, bool>(opts.Mu, opts.Theta)));
                        watch.Stop();
                        Synthesizer.Engine.ClearLearningCache();

                        file.WriteLine($"\nProfile:\n  [$] {string.Join("\n  [$] ", program.Description())}");
                        List<List<string>> clustered_data = data.SelectMany(d => d).GroupBy(s => program.GetMatchingTokens(s))
                                                                .Select(g => g.ToList()).ToList();
                        double nmi = NormalizedMutualInfo(data, clustered_data);
                        stats[ahc_info.NMI] += nmi;
                        double time = watch.ElapsedMilliseconds;
                        stats[ahc_info.TIME] += time;

                        file.WriteLine("\nClusters");
                        foreach (var d in clustered_data)
                            file.WriteLine($"  [=]  {string.Join("  .-.  ", d)}");

                        file.WriteLine($"\n{nmi,4:F2} @ {time,5}ms");
                        Console.Write($"   {nmi,4:F2} ({Math.Round(time/1000.0, 0),3}s)");
                    }

                    file.WriteLine($"\n\nSum(Time) = {stats[ahc_info.TIME]}ms");
                    file.WriteLine($"Avg(Time) = {stats[ahc_info.TIME] / opts.TrialsPerClustering:F2}s");
                    file.WriteLine($"Avg(NMI) = {stats[ahc_info.NMI] / opts.TrialsPerClustering,4:F2}");
                    file.Flush();
                }
            }

            return 0;
        }
    }
}
