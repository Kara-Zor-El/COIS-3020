using System;
using CourseGraph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace A1Tests;

[TestClass]
public class UpdateVertexTests {
  /// <summary>
  /// UpdateVertex with non-phantom degree throws an ArgumentException.
  /// </summary>
  [TestMethod]
  public void NonPhantomDegreeThrows() {
    var g = new CourseGraph.CourseGraph();
    var deg = GraphTestHelpers.Course("NotPhantom");
    var c = GraphTestHelpers.Course("C1");
    g.AddVertex(deg);
    g.AddVertex(c);
    try {
      g.UpdateVertex(c, deg);
      Assert.Fail("Expected ArgumentException for non-phantom degree.");
    }
    catch (ArgumentException) { /* expected */ }
  }

  /// <summary>
  /// Setting course required adds it to degree PreRequisites.
  /// </summary>
  [TestMethod]
  public void SetRequiredAddsToDegree() {
    var g = new CourseGraph.CourseGraph();
    var deg = GraphTestHelpers.Phantom("Degree");
    var c = GraphTestHelpers.Course("C1");
    g.AddVertex(deg);
    g.AddVertex(c);
    g.UpdateVertex(c, deg);
    Assert.Contains("C1", deg.PreRequisites);
  }

  /// <summary>
  /// Toggling required off removes course from degree PreRequisites.
  /// </summary>
  [TestMethod]
  public void TogglesOffRemovesFromRequired() {
    var g = new CourseGraph.CourseGraph();
    var deg = GraphTestHelpers.Phantom("Degree");
    var c = GraphTestHelpers.Course("C1");
    g.AddVertex(deg);
    g.AddVertex(c);
    g.UpdateVertex(c, deg);
    Assert.Contains("C1", deg.PreRequisites);
    g.UpdateVertex(c, deg);
    Assert.DoesNotContain("C1", deg.PreRequisites);
  }
}
