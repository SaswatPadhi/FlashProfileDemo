using System;
using System.Linq;

using Microsoft.ProgramSynthesis.Matching.Text.Semantics;
using VDS.Common.Tries;

namespace FlashProfileDemo {

    public class TrieAtom : IToken, IEquatable<TrieAtom> {
        public StringTrie<string> trie { get; }

        public TrieAtom(string name, string[] strings, bool case_sensitive = false) {
            Name = name;
            Score = -1.0 * strings.Length / strings.Sum(s => s.Length);
            Description = name;
            CaseSensitive = case_sensitive;

            trie = new StringTrie<string>();
            foreach (string s in strings)
                trie.Add(CaseSensitive ? s : s.ToUpper(), s);
        }

        public string Description { get; }

        public string Name { get; }

        public double Score { get; }

        public bool CaseSensitive { get; }

        public bool Equals(TrieAtom other) {
            return Name.Equals(other.Name) && Score.Equals(other.Score);
        }

        public bool Equals(IToken other) {
            if (other == null || GetType() != other.GetType()) {
                return false;
            }
            return Equals((TrieAtom)other);
        }

        public override int GetHashCode() {
            return 101 * trie.GetHashCode() ^ 47 * Name.GetHashCode() ^ Score.GetHashCode();
        }

        public uint PrefixMatchLength(string target) {
            var node = trie.FindPredecessor(CaseSensitive ? target : target.ToUpper());
            return node == null ? 0 : (uint)node.Value.Length;
        }

        public override string ToString() => Name;
    }

}