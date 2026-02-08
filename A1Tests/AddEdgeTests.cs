using System;
using CourseGraph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace A1Tests;

[TestClass]
public class AddEdgeTests {
  /// <summary>Add the edge, remove it, add it again.</summary>
  [TestMethod]
  public void CanAddAndRemoveEdge() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    g.AddVertex(a);
    g.AddVertex(b);
    Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(g, a, b));
    g.AddEdge(a, b, CourseRelation.Prereq);
    Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(g, a, b));
    g.RemoveEdge(a, b);
    Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(g, a, b));
    g.AddEdge(a, b, CourseRelation.Prereq);
    Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(g, a, b));
  }

  /// <summary>
  /// Adding same pair again (regardless of relation) does not create a second edge.
  /// </summary>
  [TestMethod]
  public void DuplicateEdgeIgnored() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    g.AddVertex(a);
    g.AddVertex(b);
    g.AddEdge(a, b, CourseRelation.Prereq);
    Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(g, a, b));
    g.AddEdge(a, b, CourseRelation.Prereq);
    g.AddEdge(a, b, CourseRelation.Coreq);
    Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(g, a, b), "Duplicate adds must not create a second edge.");
  }

  /// <summary>
  /// Loops throw ArgumentException.
  /// </summary>
  [TestMethod]
  public void LoopsThrowArgumentException() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    g.AddVertex(a);
    try {
      g.AddEdge(a, a, CourseRelation.Prereq);
      Assert.Fail("Expected ArgumentException for loop.");
    }
    catch (ArgumentException) { /* expected */ }
  }

  /// <summary>
  /// Adding B->A when A->B exists throws.
  /// </summary>
  [TestMethod]
  public void DirectCycleThrowsArgumentException() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    g.AddVertex(a);
    g.AddVertex(b);
    g.AddEdge(a, b, CourseRelation.Prereq);
    try {
      g.AddEdge(b, a, CourseRelation.Prereq);
      Assert.Fail("Expected ArgumentException for cycle.");
    }
    catch (ArgumentException) { /* expected */ }
  }

  /// <summary>
  /// Indirect cycles throw ArgumentException
  /// (A -> B -> C -> A)
  /// </summary>
  [TestMethod]
  public void IndirectCyclesThrowArgumentException() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    var c = GraphTestHelpers.Course("C");
    g.AddVertex(a);
    g.AddVertex(b);
    g.AddVertex(c);
    g.AddEdge(a, b, CourseRelation.Prereq);
    g.AddEdge(b, c, CourseRelation.Prereq);
    try {
      g.AddEdge(c, a, CourseRelation.Prereq);
      Assert.Fail("Expected ArgumentException for cycle.");
    }
    catch (ArgumentException) { /* expected */ }
  }

  /// <summary>
  /// When a source is missing, the edge is not added.
  /// </summary>
  [TestMethod]
  public void SourceMissingDoesNothing() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    g.AddVertex(b);
    g.AddEdge(a, b, CourseRelation.Prereq);
    var data = g.GetCourseData();
    Assert.HasCount(1, data.Courses);
  }

  /// <summary>
  /// When a target is missing, the edge is not added.
  /// </summary>
  [TestMethod]
  public void TargetMissingDoesNothing() {
    var g = new CourseGraph.CourseGraph();
    var a = GraphTestHelpers.Course("A");
    var b = GraphTestHelpers.Course("B");
    g.AddVertex(a);
    g.AddEdge(a, b, CourseRelation.Prereq);
    var data = g.GetCourseData();
    Assert.HasCount(1, data.Courses);
  }
}
