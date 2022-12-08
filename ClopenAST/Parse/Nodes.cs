using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Linq;

namespace ClopenDream {

    public class Node {
        [JsonIgnore]
        public int Indent { get; set; }

        [JsonIgnore]
        public OpenDreamShared.Compiler.Location Location { get; set; }

        [JsonIgnore]
        public int RawLine { get; set; }

        [JsonIgnore]
        public string Text { get; set; }

        [JsonIgnore]
        public HashSet<string> Labels { get; set; } = new();

        [JsonIgnore]
        public Dictionary<string, object> Tags { get; set; } = new();

        [JsonIgnore]
        public Node Trunk { get; set; }
        [JsonIgnore]
        public List<Node> Leaves { get; set; } = new();

        public List<string> L { get; set; } = new();
        public Dictionary<string, object> T { get; set; } = new();
        public List<Node> N { get; set; } = new();

        public void Connect() {
            foreach (var leaf in Leaves) {
                leaf.Trunk = this;
                leaf.Connect();
            }
        }

        public void FixLabels() {
            foreach (var label in Labels.ToList()) {
                Labels.Remove(label);
                Labels.Add(label.Substring(label.IndexOf("Check") + 5));
            }
            foreach (var leaf in Leaves) {
                leaf.FixLabels();
            }
        }

        public void PrepJson() {
            L = Labels.ToList();
            T = Tags;
            N = Leaves;
            foreach (var leaf in Leaves) {
                leaf.PrepJson();
            }
        }
        public void ClearLabels() {
            Labels.Clear();
            foreach (var leaf in Leaves) {
                leaf.ClearLabels();
            }
        }
        public void Cycle(HashSet<Node> seen) {
            seen.Add(this);
            foreach (var leaf in Leaves) {
                if (seen.Contains(leaf)) {
                    throw new Exception("cycle detected");
                } else {
                    leaf.Cycle(seen);
                }
            }

        }
        public HashSet<int> MaxDepth(HashSet<int> depth, int cdepth) {
            depth.Add(cdepth);
            foreach (var leaf in Leaves) {
                leaf.MaxDepth(depth, cdepth+1);
            }
            return depth;
        }

        public bool CheckTagArray(string key, int index, dynamic value) {
            if (!Tags.ContainsKey(key)) { return false; }
            if (Tags[key] is object[] ss) {
                if (ss.Length != 1) { return false; }
                return ss[index].Equals(value);
            }
            return false;
        }

        public bool CheckTag(string key, dynamic value) {
            if (!Tags.ContainsKey(key)) { return false; }
            if (Tags[key].Equals(value)) { return true; }
            return false;
        }

        public Node UniqueLeaf() {
            if (Leaves.Count != 1) { return null; }
            else { return Leaves[0]; }
        }

        public Node UniqueBlank() {
            if (Leaves.Count != 1) { return null; }
            else {
                Node maybeBlank = Leaves[0];
                if (!maybeBlank.Tags.ContainsKey("blank") || maybeBlank.Leaves.Count != 1) {
                    return null;
                }
                return maybeBlank.Leaves[0];
            }
        }

        public Node IgnoreBlank() {
            if (Leaves.Count != 1) { return this; }
            if (Tags.ContainsKey("blank")) { return Leaves[0]; }
            return this;
        }

        public string FormatLabels() {
            return Labels.Aggregate((s1, s2) => s1 + " " + s2);
        }

        public IEnumerable<Node> AllNodes() {
            yield return this;
            foreach (var leaf in Leaves) {
                foreach (var leaf2 in leaf.AllNodes()) {
                    yield return leaf2;
                };
            }
        }
        public string Print() {
            StringBuilder sb = new();
            sb.Append(RawLine + " ");
            foreach (var k in Tags.Keys) {
                if (Tags[k] is string[] ss) {
                    sb.Append(k + ":");
                    foreach (var s in ss) { sb.Append(s + ","); }
                    sb.Append(" ");
                }
                else if (Tags[k] != null) {
                    sb.Append(k + ":" + Tags[k].ToString() + " ");
                }
                else {
                    sb.Append(k + " ");
                }
            }
            if (Labels.Count > 0) {
                sb.Append(" ||| ");
                foreach (var l in Labels) {
                    sb.Append(l + " ");
                }
            }
            return sb.ToString();
        }

        public Exception Error(string s) {
            var sr = new StringWriter();
            sr.WriteLine(Text);
            sr.WriteLine(PrintLeaves(5));
            if (Labels.Count > 0) {
                sr.WriteLine(RawLine + Labels.Aggregate((s1, s2) => s1 + " " + s2));
            }
            sr.WriteLine(s);
            return new Exception( sr.ToString() );
        }

        public IEnumerable<Node> Unwind() {
            Node n = this;
            while (n.Trunk != null) {
                yield return n;
                n = n.Trunk;
            }
            yield break;
        }
        public string PrintLeaves(int mdepth, int cdepth = 0) {
            if (cdepth > mdepth) {
                return "";
            }
            StringBuilder sb = new();
            sb.Append(new String(' ', 2 * cdepth));
            sb.Append(Print());
            sb.Append('\n');
            foreach (var child in Leaves) {
                sb.Append(child.PrintLeaves(mdepth, cdepth + 1));
            }
            return sb.ToString();
        }

        public void Delete(Node n) {
            Leaves.Remove(n);
            if (Leaves.Count == 0) {
                this.Trunk.Delete(this);
            }
        }

        public Node Next() {
            var idx = Trunk.Leaves.IndexOf(this);
            if (idx < Trunk.Leaves.Count - 1) {
                return Trunk.Leaves[idx + 1];
            }
            else {
                return null;
            }
        }

        public string GetHash() {
            StringBuilder sb = new();
            Node cnode = this;
            while (cnode != null) {
                sb.Append(cnode.LocalHash() + "@");
                cnode = cnode.Trunk;
            }
            return sb.ToString();
        }

        public string LocalHash() {
            if (Tags.ContainsKey("root")) {
                return "";
            }
            if (Labels.Contains("ObjectDecl")) {
                return "OD-" + (string)Tags["bare"];
            }
            if (Labels.Contains("ObjectVarDecl")) {
                return "OVD-" + (string)Tags["bare"];
            }
            if (Labels.Contains("PathTerminated") || Labels.Contains("PathDecl")) {
                return "P-" + (string)Tags["bare"];
            }
            if (Labels.Contains("Proc")) {
                if (Tags.ContainsKey("bare")) {
                    return "PRC-" + (string)Tags["bare"];
                }
                if (Tags.ContainsKey("operator")) {
                    return "OO-" + (string)Tags["operator"];
                }
            }
            if (Labels.Contains("ProcDecl")) {
                return "";
            }
            if (Labels.Contains("ParentDecl")) {
                return "PT- " + string.Join("", Tags["path"]);
            }
            if (Labels.Contains("ChildDecl")) {
                return "CT- " + string.Join("", Tags["path"]);
            }
            if (Labels.Contains("ObjectAssignStmt")) {
                var strs = ((string[])Leaves[0].Tags["ident"]);
                if (strs.Length != 1) {
                    throw Error("non bare ident in objectassignstmt");
                }
                return "OAS-" + strs[0];
            }
            throw Error("");
        }
    }

}