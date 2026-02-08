using CourseGraph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace A1Tests;

[TestClass]
public class AddVertexTests {
  /// <summary>
  /// Adds a single course.
  /// Make sure graph contains it with correct name.
  /// </summary>
  [TestMethod]
  public void SingleCourseAddedCorrectly() {
    var g = new CourseGraph.CourseGraph();
    var c = GraphTestHelpers.Course("C1");
    g.AddVertex(c);
    var data = g.GetCourseData();
    Assert.HasCount(1, data.Courses);
    Assert.AreEqual("C1", data.Courses[0].Name);
  }

  /// <summary>
  /// Adding the same course again does not create a duplicate vertex.
  /// </summary>
  [TestMethod]
  public void DuplicateCourseNotAdded() {
    var g = new CourseGraph.CourseGraph();
    var c = GraphTestHelpers.Course("C1");
    g.AddVertex(c);
    g.AddVertex(c);
    var data = g.GetCourseData();
    Assert.HasCount(1, data.Courses);
  }

  /// <summary>
  /// Multiple distinct courses all appear in the course list.
  /// </summary>
  [TestMethod]
  public void MultipleCoursesAllPresent() {
    var g = new CourseGraph.CourseGraph();
    g.AddVertex(GraphTestHelpers.Course("A"));
    g.AddVertex(GraphTestHelpers.Course("B"));
    g.AddVertex(GraphTestHelpers.Course("C"));
    var data = g.GetCourseData();
    Assert.HasCount(3, data.Courses);
    Assert.IsTrue(data.Courses.Exists(x => x.Name == "A"));
    Assert.IsTrue(data.Courses.Exists(x => x.Name == "B"));
    Assert.IsTrue(data.Courses.Exists(x => x.Name == "C"));
  }

  /// <summary>
  /// Phantom course appears in Degrees, not Courses.
  /// </summary>
  [TestMethod]
  public void PhantomCourseInDegrees() {
    var g = new CourseGraph.CourseGraph();
    var deg = GraphTestHelpers.Phantom("Degree");
    g.AddVertex(deg);
    var data = g.GetCourseData();
    Assert.HasCount(1, data.Degrees);
    Assert.AreEqual("Degree", data.Degrees[0].Name);
    Assert.HasCount(0, data.Courses);
  }
}
