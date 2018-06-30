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

    public static class Synthesizer {

        public static Symbol SRegionSymbol { get; }
            = Learner.Instance.ScoreFeature.Grammar.Symbols["sRegion"];

        public static State StringToState(string s) => State.Create(SRegionSymbol, new SuffixRegion(s));

        public static string StateToString(State s) => (s[SRegionSymbol] as SuffixRegion).Value;

        private static Symbol MatchSymbol { get; } = Learner.Instance.ScoreFeature.Grammar.Symbols["match"];

        public static Witnesses Witnesses { get; }
            = new Witnesses(Learner.Instance.ScoreFeature.Grammar,
                            Learner.Instance.ScoreFeature,
                            new Witnesses.Options(1, 1, false),
                            Utils.Default.Atoms.ToHashSet());

        public static SynthesisEngine Engine { get; }
            = new SynthesisEngine(Learner.Instance.ScoreFeature.Grammar,
                        new SynthesisEngine.Config {
                            Strategies = new ISynthesisStrategy[] {
                                    new DeductiveSynthesis(Witnesses)
                            },
                            CacheSize = int.MaxValue,
                        });

        public static LearningTask Task(params State[] states)
            => new LearningTask(MatchSymbol, new ExampleSpec(states.Distinct().ToDictionary(s => s, s => (object)true)));

        public static ProgramSet LearnAll(params State[] states)
            => Engine.Learn(Task(states));

        public static ProgramNode Learn(int k, params State[] states)
            => Engine.Learn(Task(states).WithTopKRequest(k, Learner.Instance.ScoreFeature))?
                     .RealizedPrograms.FirstOrDefault();

    }

}