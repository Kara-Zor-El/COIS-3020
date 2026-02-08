using CourseGraph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace A1Tests;

[TestClass]
public class RemoveEdgeTests {
  /// <summary>
  /// Removing existing edge removes it.
  /// And the same edge can be added again.
  /// </summary>
  [TestMethod]
  public void ExistingEdgeRemoved() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    g.AddVertex(a);
    g.AddVertex(b);
    g.AddEdge(a, b, CourseRelation.Prereq);
    g.RemoveEdge(a, b);
    Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(g, a, b));
    g.AddEdge(a, b, CourseRelation.Prereq);
    Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(g, a, b));
  }

  /// <summary>
  /// Removing non-existent edge does nothing.
  /// </summary>
  [TestMethod]
  public void MissingEdgeDoesNothing() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    g.AddVertex(a);
    g.AddVertex(b);
    g.RemoveEdge(a, b);
    Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(g, a, b));
  }

  /// <summary>
  /// RemoveEdge when source not in graph does nothing.
  /// </summary>
  [TestMethod]
  public void SourceMissingDoesNothing() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    g.AddVertex(b);
    g.RemoveEdge(a, b);
    Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(g, a, b));
  }
}
