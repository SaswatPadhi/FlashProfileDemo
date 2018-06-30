using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Matching.Text;
using Microsoft.ProgramSynthesis.Matching.Text.Learning;
using Microsoft.ProgramSynthesis.Matching.Text.Semantics;

namespace FlashProfileDemo {

    public static class Similarity {
        public static SimData ComputeScoreForStrings(bool sim, string A, string B)
          => ScoreStates(sim, Synthesizer.StringToState(A), Synthesizer.StringToState(B));

        public static SimData ScoreStates(bool ground_truth, State A, State B) {
            Stopwatch s = new Stopwatch();
            s.Start();
            ProgramNode program = Synthesizer.Learn(1, A, B);
            s.Stop();
            Synthesizer.Engine.ClearLearningCache();

            return new SimData {
                A = Synthesizer.StateToString(A),
                B = Synthesizer.StateToString(B),
                GroundTruth = ground_truth,
                SynthesisTime = Convert.ToUInt32(s.ElapsedMilliseconds),
                Pattern = (program == null ? "<NULL>" : program.AcceptVisitor(new TokensCollector()).CombinedDescription()),
                Cost = Convert.ToSingle(program == null ? -DefaultTokens.Any.Score
                                                        : -program.GetFeatureValue(Learner.Instance.ScoreFeature)),
                Score = Convert.ToSingle(1.0 / (program == null ? Witnesses.ScoreTransform(-DefaultTokens.Any.Score)
                                                                : Witnesses.ScoreTransform(-program.GetFeatureValue(Learner.Instance.ScoreFeature))))
            };
        }

        public static int Estimate(SimilarityOptions opts) {
            Random rnd = new Random(0xf00d);

            // Do a learning call and just ignore the result.
            // To warm up PROSE. The first learning call always takes longer for some reason.
            Synthesizer.Learn(1, Synthesizer.StringToState(">)#*$&"), Synthesizer.StringToState("969dvb"));

            var log_path = Path.Combine(Utils.Paths.LogsDir, "Similarity.FlashProfile.log");
            File.WriteAllText(log_path, "");

            int sim_total = 0, dis_total = 0;
            for (int i = 0; i < Utils.Paths.CleanDatasets.Length; ++i) {
                Console.Write($"\r[+] Saving to {log_path}: [{sim_total,8} +ve, {dis_total,8} -ve] ... {(100.0 * i) / Utils.Paths.CleanDatasets.Length,5:F2} %");

                var file_1 = Utils.Paths.CleanDatasets[i];
                List<string> inputs = TestCase.LoadNonEmptyData(file_1);
                var results = (from s1 in inputs.OrderBy(s => rnd.Next())
                               from s2 in inputs.OrderBy(s => rnd.Next())
                               where s1 != s2
                               select ComputeScoreForStrings(true, s1, s2)
                              ).Take(opts.SimCount);
                foreach (var res in results) {
                    File.AppendAllText(log_path, $"{res}\n");
                    sim_total++;
                }

                results = Utils.Paths.CleanDatasets.Where(file_2 => file_2 != file_1)
                               .Select(file_2 => TestCase.LoadNonEmptyData(file_2).OrderBy(s => rnd.Next()).Take(opts.DisCount))
                               .SelectMany(disContent => (from s1 in inputs.OrderBy(s => rnd.Next()).Take(opts.DisCount)
                                                          from s2 in disContent
                                                          select ComputeScoreForStrings(false, s1, s2)));
                foreach (var res in results) {
                    File.AppendAllText(log_path, $"{res}\n");
                    dis_total++;
                }
            }
            Console.WriteLine($"\r[+] Saving to {log_path}: [{sim_total,8} +ve, {dis_total,8} -ve] ... 100 %");

            return 0;
        }

    }

    public struct SimData {
        public string A;
        public string B;

        public bool GroundTruth;
        public float Cost;
        public float Score;
        public string Pattern;
        public uint SynthesisTime;

        public override string ToString()
            => $"{GroundTruth,5}  |  [{SynthesisTime,5}] @ {Score,8:F5} :: {Pattern,96}\n       => \"{A}\"\n       => \"{B}\"";
    }
}
