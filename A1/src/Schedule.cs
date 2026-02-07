using System;
using System.Text.Json;

namespace CourseGraph {
  public class Schedule {
    public readonly Course Degree;
    public readonly int N;
    public readonly int C;
    /// <summary>
    /// The maximum depth of the dependency chain from the degree.
    /// </summary>
    public int ChainDepth { get; }
    /// <summary>
    /// The latest term a course should be taken for that degree.
    /// TMax = 2 * C - chain_depth,
    /// Where:
    ///  - C = required courses
    ///  - chain_depth = max dependency depth.
    /// </summary>
    public int TMax { get; }

    public Schedule(Course degree, int n, int c, int chainDepth) {
      this.Degree = degree;
      this.N = n;
      this.C = c;
      this.ChainDepth = chainDepth;
      this.TMax = 2 * this.C - chainDepth;
    }

    public override string ToString() {
      return JsonSerializer.Serialize(this, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
    }
  }
}
