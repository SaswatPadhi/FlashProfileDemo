using System.Collections.Generic;
using CommandLine;

namespace FlashProfileDemo {

    [Verb("tests", HelpText = "Run all profiling tests, and measure running times.")]
    public class TestOptions { }

    [Verb("profile", HelpText = "Profile a JSON dataset, and show a few example strings for each pattern.")]
    public class ProfileOptions {
        [Option('s', "show-program", Default = false, HelpText = "Number of example strings to show.")]
        public bool ShowProgram { get; set; }

        [Option('n', "num-examples", Default = 1, HelpText = "Number of example strings to show.")]
        public int NumExamples { get; set; }

        [Value(0, MetaName ="data-file", Required = true, HelpText = "The JSON file containing the dataset.")]
        public string DataFile { get; set; }
    }

    [Verb("eta", HelpText = "Compute pairwise syntactic similarity of given strings.")]
    public class EtaOptions {
        [Option('n', "num-candidates", Default = 5, HelpText = "Number of candidate patterns to show.")]
        public int NumCandidates { get; set; }

        [Value(0, Min = 2, MetaName ="strings", Required = true, HelpText = "The strings (at least 2) to compute similarity over.")]
        public IEnumerable<string> Strings { get; set; }
    }

    [Verb("similarity", HelpText = "Compute the quality of patterns generated from Microsoft SSDT.")]
    public class SimilarityOptions {
        [Option('s', "sim-count", Required = true, HelpText = "Number of similar string pairs: to be picked from the same dataset (per dataset).")]
        public int SimCount { get; set; }

        [Option('d', "dis-count", Required = true, HelpText = "Number of dissimilar string pairs: to be picked from different datasets (per pair of different datasets).")]
        public int DisCount { get; set; }
    }

    [Verb("quality", HelpText = "Compute the quality of patterns generated from FlashProfile.")]
    public class QualityOptions {
        [Value(0, MetaName = "profile-fraction", Required = true, HelpText = "The fraction of each dataset to use for learning a profile (that is tested on the remaining fraction).")]
        public double ProfileFraction { get; set; }

        [Option('t', "theta", Default = 1.25, HelpText = "The theta parameter for FlashProfile.")]
        public double Theta { get; set; }

        [Option('m', "mu", Default = 4.0, HelpText = "The mu parameter for FlashProfile.")]
        public double Mu { get; set; }
    }

    [Verb("clustering", HelpText = "Compute the clustering accuracy (NMI) and profiling time for a given configuration.")]
    public class ClusteringOptions {
        [Option('t', "trials-per-clustering", Default = 10, HelpText = "Number of trials per a particular number of clusters.")]
        public int TrialsPerClustering { get; set; }

        [Option('s', "num-strings-per-cluster", Default = 256, HelpText = "The theta parameter for FlashProfile.")]
        public int NumStringsPerCluster { get; set; }

        [Option('m', "min-clusters", Default = 2, HelpText = "The minimum number of clusters to test.")]
        public int MinClusters { get; set; }

        [Option('M', "max-clusters", Default = 8, HelpText = "The maximum number of clusters to test.")]
        public int MaxClusters { get; set; }

        [Option('e', "theta", Default = 1.25, HelpText = "The theta parameter for FlashProfile.")]
        public double Theta { get; set; }

        [Option('u', "mu", Default = 4.0, HelpText = "The mu parameter for FlashProfile.")]
        public double Mu { get; set; }
    }

}