using CourseGraph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace A1Tests;

[TestClass]
public class RemoveVertexTests {
  /// <summary>
  /// Removing an existing course removes it.
  /// And the course list is empty.
  /// </summary>
  [TestMethod]
  public void ExistingCourseRemoved() {
    var g = new CourseGraph.CourseGraph();
    var c = GraphTestHelpers.Course("C1");
    g.AddVertex(c);
    g.RemoveVertex(c);
    var data = g.GetCourseData();
    Assert.IsEmpty(data.Courses);
  }

  /// <summary>
  /// Removing a course not in the graph does nothing.
  /// </summary>
  [TestMethod]
  public void MissingCourseDoesNothing() {
    var g = new CourseGraph.CourseGraph();
    var c = GraphTestHelpers.Course("C1");
    g.AddVertex(GraphTestHelpers.Course("Other"));
    g.RemoveVertex(c);
    var data = g.GetCourseData();
    Assert.HasCount(1, data.Courses);
  }

  /// <summary>
  /// Removing middle B (A -> B -> C) rewires so A gets C as prereq.
  /// </summary>
  [TestMethod]
  public void MiddleCourseRemovedRewiresPrereqs() {
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
    var data = g.GetCourseData();
    Assert.HasCount(2, data.Courses);
    Assert.IsTrue(data.Courses.Exists(x => x.Name == "A"));
    Assert.IsTrue(data.Courses.Exists(x => x.Name == "C"));
    Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(g, a, c), "Rewire gives A->C; AddEdge is no-op.");
  }

  /// <summary>
  /// After rewiring, inherited edge can be removed and more vertices added.
  /// </summary>
  [TestMethod]
  public void InheritedEdgeRemovableAfterRewire() {
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
    g.RemoveEdge(a, c);
    g.AddVertex(GraphTestHelpers.Course("D"));
    var data = g.GetCourseData();
    Assert.HasCount(3, data.Courses);
  }

  /// <summary>
  /// Removing shared prereq B (A -> B, C -> B) leaves A and C; both rewired.
  /// </summary>
  [TestMethod]
  public void SharedPrereqRemovedBothRemain() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    var c = GraphTestHelpers.Course("C");
    g.AddVertex(a);
    g.AddVertex(b);
    g.AddVertex(c);
    g.AddEdge(a, b, CourseRelation.Prereq);
    g.AddEdge(c, b, CourseRelation.Prereq);
    g.RemoveVertex(b);
    var data = g.GetCourseData();
    Assert.HasCount(2, data.Courses);
  }

  /// <summary>
  /// Removing vertex with mixed coreq/prereq edges rewires without error.
  /// </summary>
  [TestMethod]
  public void MixedRelationsRewireOk() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    var c = GraphTestHelpers.Course("C");
    g.AddVertex(a);
    g.AddVertex(b);
    g.AddVertex(c);
    g.AddEdge(a, b, CourseRelation.Coreq);
    g.AddEdge(b, c, CourseRelation.Prereq);
    g.RemoveVertex(b);
    var data = g.GetCourseData();
    Assert.HasCount(2, data.Courses);
  }
}
