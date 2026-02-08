using CourseGraph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace A1Tests;

[TestClass]
public class TaskAGraphEdgeCaseTests {
  /// <summary>
  /// Add vertex, add edge, remove vertex runs without error.
  /// </summary>
  [TestMethod]
  public void AddVertexAddEdgeRemoveVertex() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    g.AddVertex(a);
    g.AddVertex(b);
    g.AddEdge(a, b, CourseRelation.Prereq);
    g.RemoveVertex(b);
    Assert.HasCount(1, g.GetCourseData().Courses);
  }

  /// <summary>
  /// Removing a course from an empty graph does nothing.
  /// </summary>
  [TestMethod]
  public void EmptyGraphRemoveNoOp() {
    var g = new CourseGraph.CourseGraph();
    g.RemoveVertex(GraphTestHelpers.Course("X"));
    Assert.IsEmpty(g.GetCourseData().Courses);
  }

  /// <summary>
  /// For chain A->B->C, removing B rewires A->C and edge A->C is removable.
  /// </summary>
  [TestMethod]
  public void ChainRemoveMiddleRewires() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    var c = GraphTestHelpers.Course("C");
    g.AddVertex(a);
    g.AddVertex(b);
    g.AddVertex(c);
    g.AddEdge(a, b, CourseRelation.Prereq);
    g.AddEdge(b, c, CourseRelation.Prereq);
    g.RemoveVertex(b);
    g.AddEdge(a, c, CourseRelation.Prereq);
    g.RemoveEdge(a, c);
    var data = g.GetCourseData();
    Assert.HasCount(2, data.Courses);
  }
}
