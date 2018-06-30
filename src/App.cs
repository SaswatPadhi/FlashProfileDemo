using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using CommandLine;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnitLite;

using MText = Microsoft.ProgramSynthesis.Matching.Text;

namespace FlashProfileDemo {
    using Constraint = Microsoft.ProgramSynthesis.Wrangling.Constraints.Constraint<string, bool>;
    using DisjunctionsLimit = MText.DisjunctionsLimit<string, bool>;

    public static class App {
        public static int Profile(ProfileOptions opts) {
            var all_inputs = JsonConvert.DeserializeObject<TestCase.CompositeCases>(File.ReadAllText(opts.DataFile)).Data;
            var all_inputs_count = all_inputs.Count;

            Console.WriteLine($"# {opts.DataFile} ({all_inputs_count} strings)");
            Console.WriteLine("==============================================\n");
            foreach (TestCaseData test in TestCase.LoadConstraints(opts.DataFile)) {
                IEnumerable<string> inputs = all_inputs;
                var constraints = test.Arguments.First() as IEnumerable<Constraint>;
                var disjuncts = constraints.OfType<DisjunctionsLimit>().FirstOrDefault()?.MaxDisjuncts;

                Console.WriteLine($"> Number of patterns allowed = {disjuncts?.ToString() ?? "<auto>"}");
                Console.WriteLine("----------------------------------------------");

                var prog = MText.Learner.Instance.Learn(constraints);
                foreach (string pattern in prog?.Description() ?? Enumerable.Empty<string>()) {
                    var matching_inputs = inputs.Where(s => prog.GetMatchingTokens(s).Describe() == pattern).ToList();
                    var matching_fraction = 100.0 * matching_inputs.Count / all_inputs_count;

                    Console.WriteLine($"    [{matching_inputs.Count,5} | {matching_fraction,6:F2} %] ==> {pattern}");
                    matching_inputs.Take(opts.NumExamples).ForEach(s => Console.WriteLine($"                        @ {s}"));
                    inputs = inputs.Where(s => !matching_inputs.Contains(s));
                }
                if (opts.ShowProgram) Console.WriteLine($"Program:\n{prog}");
                Console.WriteLine("..............................................\n");
            }
            return 0;
        }

        public static string ParseNullEmpty(string s) {
            if (s == "([NULL])") return null;
            else if (s == "([EMPTY])") return "";
            return s;
        }

        public static int ComputeEta(EtaOptions opts) {
            var states = opts.Strings.Select(ParseNullEmpty).Select(Synthesizer.StringToState).ToArray();
            var pairs = states.Select((s, i) => new { s, i })
                              .SelectMany(x => states.Skip(x.i + 1), (x, y) => Tuple.Create(x.s, y));

            List<double> dissims = new List<double>();
            foreach (var pair in pairs) {
                var res = Similarity.ScoreStates(true, pair.Item1, pair.Item2);
                Console.WriteLine($"> {{ '{res.A}' , '{res.B}' }} => {res.Pattern}");
                Console.WriteLine($"> Pairwise Dissimilarity = {res.Cost}\n");
                dissims.Add(res.Cost);
            }

            if (opts.Strings.Count() > 2) {
                Console.WriteLine($"> Avg. Pairwise Dissimilarity = {dissims.Sum() / dissims.Count(),12:F5}");
                var prog = Synthesizer.Learn(1, states);
                Console.WriteLine($"> Best Overall Pattern = {prog.Description()}");
            }

            var vsa = Synthesizer.LearnAll(states);
            Console.WriteLine($"> Total Number of Consistent Patterns = {vsa.Size}.");

            if (opts.NumCandidates < 1) return 0;
            int sample_size = vsa.Size > opts.NumCandidates ? opts.NumCandidates : (int)vsa.Size;
            Console.WriteLine($"\n> {sample_size} Randomly Selected Patterns:");

            var rnd = new Random();
            var sampledProgs = new SortedSet<ProgramNode>(
                Comparer<ProgramNode>.Create((p1, p2) => p1.Description().CompareTo(p2.Description()))
            );
            while (sampledProgs.Count() < sample_size)
                sampledProgs.Add(vsa.Sample(rnd));

            foreach (var p in sampledProgs)
                Console.WriteLine($"  * {-p.GetFeatureValue(MText.Learner.Instance.ScoreFeature),12:F5} : {p.Description()}");

            return 0;
        }

        public static int Main(string[] args) {
            return CommandLine.Parser.Default.ParseArguments<
              ClusteringOptions,
              EtaOptions,
              ProfileOptions,
              QualityOptions,
              SimilarityOptions,
              TestOptions
            >(args).MapResult(
              (ClusteringOptions opts) => Clustering.Estimate(opts),
              (EtaOptions opts) => ComputeEta(opts),
              (ProfileOptions opts) => Profile(opts),
              (QualityOptions opts) => Quality.Estimate(opts),
              (SimilarityOptions opts) => Similarity.Estimate(opts),
              (TestOptions _) => new AutoRun(Assembly.GetEntryAssembly()).Execute(null),
              _ => 1);
        }
    }

}