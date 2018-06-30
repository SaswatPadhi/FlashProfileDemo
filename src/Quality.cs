using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.ProgramSynthesis.Matching.Text;

namespace FlashProfileDemo {

    public static class Quality {
        public static int Estimate(QualityOptions opts) {
            Random rnd = new Random(0xf00d);

            // Do a learning call and just ignore the result.
            // To warm-up PROSE. The first learning call always takes longer for some reason.
            Synthesizer.Learn(1, Synthesizer.StringToState(">)#*$&"), Synthesizer.StringToState("969dvb"));

            var log_path = Path.Combine(Utils.Paths.Root, "logs", $"Quality.FlashProfile.{opts.Mu,4:F2}x{opts.Theta,4:F2}.{opts.ProfileFraction:F2}.log");
            File.WriteAllText(log_path, "");

            var datasets_dir = Path.Combine(Utils.Paths.Root, "logs", $"datasets.{opts.ProfileFraction}");
            if (!Directory.Exists(datasets_dir))
                Directory.CreateDirectory(datasets_dir);

            Console.WriteLine($"  {"FILENAME",50} => (+VE) - (-VE) =   Δ   | Avg Δ");
            Console.WriteLine(new string('-', 128));

            double final_result = 0, count = 0, precision = 0, recall = 0;
            for (int i = 0; i < Utils.Paths.DomainsDatasets.Length; ++i) {
                var file_path = Utils.Paths.DomainsDatasets[i];
                var file_name = Path.GetFileNameWithoutExtension(file_path);
                var short_file_path = file_path.Replace(Utils.Paths.Root, "");
                var inputs = TestCase.LoadNonEmptyData(file_path).OrderBy(s => rnd.Next());

                Console.Write($"\r[+] ({i,2} / {Utils.Paths.DomainsDatasets.Length,2}) {short_file_path} ... ");

                int profile_data_size = Convert.ToInt32(inputs.Count() * opts.ProfileFraction);
                if (profile_data_size < 8) {
                    Console.WriteLine($"\r> {short_file_path,50} => ignore: dataset too small");
                    continue;
                }

                var profile_data = inputs.Take(profile_data_size).ToList();
                var constraints = profile_data.Select(s => Learner.Instance.BuildPositiveConstraint(s, true, false))
                                              .Append(new AllowedTokens<string, bool>(Utils.Default.Atoms))
                                              .Append(new ClusteringParameters<string, bool>(opts.Mu, opts.Theta));
                var program = Learner.Instance.Learn(constraints);

                var should_match_data = inputs.Skip(profile_data_size);
                int should_mismatch_data_size = Math.Max(1, should_match_data.Count()
                                              / (Utils.Paths.DomainsDatasets.Length - 1));
                var should_mismatch_data
                    = Utils.Paths.DomainsDatasets.Where(s => s != file_path)
                           .SelectMany(src => TestCase.LoadNonEmptyData(src).OrderBy(s => rnd.Next())
                           .Take(should_mismatch_data_size)).ToList();
                should_match_data = should_match_data.Take(should_mismatch_data.Count);

                double mismatch = should_mismatch_data.Count(s => program?.Run(s) ?? false);
                double match = should_match_data.Count(s => program?.Run(s) ?? false);

                precision += match / (match + mismatch);
                recall += match / should_mismatch_data.Count;

                match /= should_mismatch_data.Count;
                mismatch /= should_mismatch_data.Count;
                double result = match - mismatch;
                final_result += result;
                Console.WriteLine($"\r> {short_file_path,50} => {match:F3} - {mismatch:F3} = {result:F3} | {final_result / ++count:F3}");

                // Since `file_name` may be same for two files within test/*, we preprend i.
                File.WriteAllText(Path.Combine(datasets_dir, $"{i}_{file_name}.profiled"), string.Join("\n", profile_data));
                File.WriteAllText(Path.Combine(datasets_dir, $"{i}_{file_name}.match"), string.Join("\n", should_match_data));
                File.WriteAllText(Path.Combine(datasets_dir, $"{i}_{file_name}.mismatch"), string.Join("\n", should_mismatch_data));

                File.AppendAllText(log_path, $"> {short_file_path}\n\n");
                File.AppendAllText(log_path, $"  * Profiled Subset of Data:\n    {string.Join("\n    ", profile_data)}\n\n");
                File.AppendAllText(log_path, $"  * Default Profile:\n    {string.Join("\n    ", program?.Description() ?? Enumerable.Empty<string>())}\n\n");
                File.AppendAllText(log_path, $"  * Result:\n    + Match = {match}\n    + Mismatch = {mismatch}\n    + Score = {result}\n\n- - - - - - - -\n\n");
            }

            precision /= count;
            recall /= count;
            double f1 = 2.0 * precision * recall / (precision + recall);

            File.AppendAllText(log_path, $"> Summary:");
            File.AppendAllText(log_path, $"\n  * Average Score = {final_result / count}");
            File.AppendAllText(log_path, $"\n  * Precision = {precision}");
            File.AppendAllText(log_path, $"\n  * Recall = {recall}");
            File.AppendAllText(log_path, $"\n  * F1 = {f1}");

            Console.WriteLine($"\n> Processed = {count}   :   Precision = {precision}  |  Recall = {recall}  |  F1 = {f1}");
            return 0;
        }
    }
}
