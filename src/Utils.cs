using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Matching.Text;
using Microsoft.ProgramSynthesis.Matching.Text.Learning;
using Microsoft.ProgramSynthesis.Matching.Text.Semantics;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;

namespace FlashProfileDemo {

    public static class Utils {

        public static class Paths {
            public static string Root { get; }
                = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                "..","..",".."));

            public static string LogsDir { get; } = Path.Combine(Root, "logs");

            public static string[] DomainsDatasets { get; }
                = Directory.GetFiles(Path.Combine(Root, "tests"),
                                     "*.json", SearchOption.AllDirectories)
                           .Where(path => !path.EndsWith("_cleaned.json"))
                           .Except(new []{
                               Path.Combine(Root, "tests", "hetero", "companies.json"),
                               Path.Combine(Root, "tests", "hetero", "locations.json"),
                               Path.Combine(Root, "tests", "homo", "emails.json")
                           })
                           .ToArray();

            public static string[] CleanDatasets { get; }
                = Directory.GetFiles(Path.Combine(Root, "tests", "homo"), "*.json");
        }

        public static class Default {
            public static double Theta { get; } = 1.25;

            public static double Mu { get; } = 4.0;

            public static IEnumerable<IToken> Atoms { get; }
                = Microsoft.ProgramSynthesis.Matching.Text.Learning.DefaultTokens.AllTokens;
        }

        public struct CustomRegexToken {
            public string Name;
            public string Regex;
            public double Score;
        }

        public static string Describe(this IToken t) {
            var ct = t as ConstantToken;
            if (ct != null) return $"'{ct.Constant}'";

            if (Microsoft.ProgramSynthesis.Matching.Text.Learning.DefaultTokens.EmptyToken.Equals(t))
                return "";

            return t.Description;
        }

        public static string Describe(this IEnumerable<IToken> atoms) {
            return string.Join(" · ", atoms.Select(t => t.Describe()));
        }

        public static string Description(this ProgramNode node)
            => node.AcceptVisitor(new TokensCollector()).Describe();

        public static IEnumerable<string> Description(this Program prog) {
            return prog.ProgramNode.GetFeatureValue(Learner.Instance.DisjunctsFeature)
                       .Select(Description);
        }

        public static HashSet<IToken> ExtendedAtoms { get; }
            = new HashSet<IToken> {
                new RegexToken("<EMail>", @"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+", -5),
                new RegexToken("<PhoneNum>", @"(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}", -5),
                new RegexToken("<URL>", @"(http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?", -5),
            };

        public static IEnumerable<IToken> DefaultWithExtendedTokens { get; }
            = Default.Atoms.Concat(ExtendedAtoms);

        public static IEnumerable<IToken> LoadTokens(bool? use_extended_tokens = null,
                                                     IEnumerable<string> exclude_names = null,
                                                     IEnumerable<CustomRegexToken> regex_tokens = null) {
            return ((use_extended_tokens ?? false) ? DefaultWithExtendedTokens : Default.Atoms)
                     .Where(t => !(exclude_names?.Contains(t.Name) ?? false))
                     .Concat(regex_tokens?.Select(r => new RegexToken(r.Name, r.Regex, r.Score))
                             ?? Enumerable.Empty<IToken>());
        }

        static Utils() {
            foreach (var file in Directory.GetFiles(Path.Combine(Paths.Root, "semantic_atoms"), "*.nocase", SearchOption.AllDirectories))
                ExtendedAtoms.Add(new TrieAtom(Path.GetFileNameWithoutExtension(file), File.ReadAllLines(file)));
            foreach (var file in Directory.GetFiles(Path.Combine(Paths.Root, "semantic_atoms"), "*.case", SearchOption.AllDirectories))
                ExtendedAtoms.Add(new TrieAtom(Path.GetFileNameWithoutExtension(file), File.ReadAllLines(file), true));
        }
    }

}