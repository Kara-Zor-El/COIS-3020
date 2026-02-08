using System;
using System.Collections.Generic;
using System.Reflection;
using CourseGraph;

namespace A1Tests {
  /// <summary>
  /// Helpers to build valid Course instances for graph tests.
  /// Edge direction: AddEdge(A, B) means B is pre-/co-requisite of A (A depends on B).
  /// </summary>
  internal static class GraphTestHelpers {
    private static readonly TimeTableInfo[] DefaultTimeSlots = [
      new() {
        OfferedTerm = Term.Fall,
        TimeSlots = [new TimeSlot(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0))]
      }
    ];

    /// <summary>Creates a degree course.</summary>
    public static Course CreateDegree(string name, List<string> preRequisites) {
      return new Course(name, [], preRequisites, [], isDegree: true);
    }

    /// <summary>Creates a non-degree course with valid timetable.</summary>
    public static Course CreateCourse(string name, List<string> preRequisites, List<string> coRequisites) {
      return new Course(name, coRequisites ?? [], preRequisites, DefaultTimeSlots, isDegree: false);
    }

    /// <summary>
    /// Returns the number of edges from, to in the graph.
    ///
    /// NOTE: We use reflection to get internal graph data, for testing.
    /// </summary>
    /// <param name="graph">The graph to inspect</param>
    /// <param name="source">The source node</param>
    /// <returns>The number of edges leaving this node, `-1` if the node doesn't exist</returns>
    public static int GetOutgoingEdgeCount(CourseGraph.CourseGraph graph, Course source) {
      var verticesProp = typeof(CourseGraph.CourseGraph).GetProperty("Vertices",
        BindingFlags.NonPublic | BindingFlags.Instance);
      if (verticesProp?.GetValue(graph) is not List<CourseVertex> vertices) return -1;
      foreach (var vertex in vertices) {
        if (vertex?.Value?.Equals(source) != true) continue;
        return vertex.Edges.Count;
      }
      return -1;
    }
  }
}
