using System;
using System.Collections.Generic;
using System.Reflection;
using CourseGraph;

namespace A1Tests;

/// <summary>
/// Helpers to build valid Course instances for graph tests.
/// Edge direction: AddEdge(A, B) means B is pre-/co-requisite of A (A depends on B).
/// </summary>
internal static class GraphTestHelpers {
  private static readonly TimeTableInfo[] DefaultTimeSlots = {
    new() {
      OfferedTerm = Term.Fall,
      TimeSlots = new[] { new TimeSlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0)) }
    }
  };

  /// <summary>Creates a phantom course (e.g. degree).</summary>
  public static Course Phantom(string name) =>
    new(name, new List<string>(), new List<string>(), Array.Empty<TimeTableInfo>(), isPhantom: true);

  /// <summary>Creates a non-phantom course with valid timetable.</summary>
  public static Course Course(string name, List<string>? preRequisites = null, List<string>? coRequisites = null) =>
    new(name, coRequisites ?? new List<string>(), preRequisites ?? new List<string>(), DefaultTimeSlots, isPhantom: false);

  /// <summary>
  /// Returns the number of edges from <paramref name="from"/> to <paramref name="to"/> in the graph.
  /// Uses reflection to read the graph structure so tests can assert on actual edge count.
  /// </summary>
  public static int GetOutgoingEdgeCount(CourseGraph.CourseGraph graph, Course from, Course to) {
    var verticesProp = typeof(CourseGraph.CourseGraph).GetProperty("Vertices",
      BindingFlags.NonPublic | BindingFlags.Instance);
    if (verticesProp?.GetValue(graph) is not List<CourseVertex> vertices)
      return 0;
    foreach (var vertex in vertices) {
      if (vertex?.Value?.Equals(from) != true) continue;
      int count = 0;
      foreach (var edge in vertex.Edges)
        if (edge.AdjVertex?.Value?.Equals(to) ?? false) count++;
      return count;
    }
    return 0;
  }
}
