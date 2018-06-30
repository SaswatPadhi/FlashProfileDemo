using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.ProgramSynthesis.Matching.Text;
using Microsoft.ProgramSynthesis.Matching.Text.Semantics;
using Newtonsoft.Json;
using NUnit.Framework;

namespace FlashProfileDemo {
    using AllowedTokens = AllowedTokens<string, bool>;
    using ClusteringParameters = ClusteringParameters<string, bool>;
    using DisjunctionsLimit = DisjunctionsLimit<string, bool>;

    public static class TestCase {
        public struct SingleCase {
            public List<string> ExcludedTokens;
            public List<Utils.CustomRegexToken> CustomRegexTokens;
            public bool? UseExtendedTokens;
            public uint? Disjuncts;
            public string IgnoreReason;
            public List<string> Description;
        }

        public struct CompositeCases {
            public List<string> Data;
            public List<SingleCase> Results;
        }

        public static IEnumerable<TestCaseData> LoadConstraints(string filename) {
            CompositeCases cc = JsonConvert.DeserializeObject<CompositeCases>(File.ReadAllText(filename));
            return cc.Results.Select(r => {
                var tcase = new TestCaseData(
                              cc.Data.Select(s => Learner.Instance.BuildPositiveConstraint(s, true, false))
                                     .Append(new ClusteringParameters(Utils.Default.Mu, Utils.Default.Theta))
                                     .Append(new DisjunctionsLimit(r.Disjuncts, r.Disjuncts))
                                     .Append(new AllowedTokens(Utils.LoadTokens(r.UseExtendedTokens, r.ExcludedTokens, r.CustomRegexTokens)))
                                     .ToList(),
                              r.Description,
                              $"clusters={r.Description.Count},avg_length={cc.Data.Average(d => d.Length)},entries={cc.Data.Count},auto={r.Disjuncts == null}").SetName(filename);
                if (r.IgnoreReason != null) tcase.Ignore(r.IgnoreReason);
                return tcase;
            });
        }

        public static IEnumerable<Tuple<List<string>, uint?, IEnumerable<IToken>>> LoadData(string filename) {
            CompositeCases cc = JsonConvert.DeserializeObject<CompositeCases>(File.ReadAllText(filename));
            return cc.Results.Select(r =>
                new Tuple<List<string>, uint?, IEnumerable<IToken>>(
                  cc.Data, r.Disjuncts, Utils.LoadTokens(r.UseExtendedTokens, r.ExcludedTokens)
            ));
        }

        public static List<string> LoadNonEmptyData(string filename)
            => TestCase.LoadData(filename).First().Item1.Where(s => !string.IsNullOrEmpty(s)).ToList();

        public static IEnumerable<TestCaseData> LoadTestCasesFromRelativeDir(string dirname) {
            return Directory.GetFiles(Path.Combine(Utils.Paths.Root, dirname), "*.json")
                            .SelectMany(LoadConstraints);
        }
    }

}