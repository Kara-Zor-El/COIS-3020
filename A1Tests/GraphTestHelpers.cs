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
    private static TimeTableInfo[] CreateDefaultTimeSlots() {
      var data = new List<TimeTableInfo>();
      foreach (Term term in Enum.GetValues(typeof(Term))) {
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek))) {
          // Skip Weekends
          if (day == DayOfWeek.Sunday) continue;
          if (day == DayOfWeek.Saturday) continue;
          // Generate A section at every hour at every term
          for (TimeOnly time = TimeTableInfo.EarliestTime; time <= TimeTableInfo.LatestTime.AddHours(-1); time = time.AddHours(1)) {
            data.Add(new() {
              OfferedTerm = term,
              TimeSlots = [new TimeSlot(day, time, time.AddHours(1))]
            });
          }
        }
      }
      return data.ToArray();
    }

    /// <summary>Creates a degree course.</summary>
    public static Course CreateDegree(string name, List<string> preRequisites) {
      return new Course(name, [], preRequisites, [], isDegree: true);
    }

    /// <summary>Creates a non-degree course with valid timetable.</summary>
    public static Course CreateCourse(string name, List<string> preRequisites, List<string> coRequisites) {
      return new Course(name, coRequisites ?? [], preRequisites, CreateDefaultTimeSlots(), isDegree: false);
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
