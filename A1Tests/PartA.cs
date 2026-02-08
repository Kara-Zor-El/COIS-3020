using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;
using VerifyTests;

namespace A1Tests {
  [TestClass]
  public class PartATest : VerifyBase {
    private VerifySettings CreateSettings() {
      var settings = new VerifySettings();
      settings.UseDirectory(System.IO.Path.Combine("Snapshots", nameof(A1Tests)));
      return settings;
    }
    #region  MainApiTest
    [TestMethod]
    public void CreateGraphTest() {
      var graph = new CourseGraph.CourseGraph();
      var serializedGraph = graph.GetCourseData(); // Serialize the course data
      Assert.HasCount(0, serializedGraph.Degrees); // Ensure we have no degrees in the graph
      Assert.HasCount(0, serializedGraph.Courses); // Ensure we have no courses in the graph
    }
    // AddVertex Testing
    #region AddVertexTest
    [TestMethod]
    public void AddVertexSingleTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var course1 = GraphTestHelpers.CreateCourse("C1", [], []); // Create a dummy course named c1
      graph.AddVertex(course1); // Add the course to the graph
      var serializedGraph = graph.GetCourseData(); // Serialize the course data
      Assert.HasCount(1, serializedGraph.Courses); // Ensure we have 1 course in the graph
      Assert.AreEqual("C1", serializedGraph.Courses[0].Name); // Ensure the course is the one we added
    }
    [TestMethod]
    public void AddVertexMultipleTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var course1 = GraphTestHelpers.CreateCourse("C1", [], []); // Create a dummy course named c1
      var course2 = GraphTestHelpers.CreateCourse("C2", [], []);
      var course3 = GraphTestHelpers.CreateCourse("C3", [], []);
      graph.AddVertex(course1); // Add the course to the graph
      var serializedGraph1 = graph.GetCourseData(); // Serialize the course data
      Assert.HasCount(1, serializedGraph1.Courses); // Ensure we have 2 course in the graph
      graph.AddVertex(course2); // Add the course to the graph
      var serializedGraph2 = graph.GetCourseData(); // Serialize the course data
      Assert.HasCount(2, serializedGraph2.Courses); // Ensure we have 2 course in the graph
      graph.AddVertex(course3); // Add the course to the graph
      var serializedGraph3 = graph.GetCourseData(); // Serialize the course data
      Assert.HasCount(3, serializedGraph3.Courses); // Ensure we have 1 course in the graph
      Assert.IsNotNull(serializedGraph3.Courses.Find(c => c.Name == "C1")); // Ensure we have c1
      Assert.IsNotNull(serializedGraph3.Courses.Find(c => c.Name == "C2")); // Ensure we have c3
      Assert.IsNotNull(serializedGraph3.Courses.Find(c => c.Name == "C3")); // Ensure we have c3
    }
    [TestMethod]
    public void AddVertexDuplicateTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var course1 = GraphTestHelpers.CreateCourse("C1", [], []); // Create a dummy course named c1
      graph.AddVertex(course1); // Add the course to the graph
      graph.AddVertex(course1); // Add the course to the graph
      var serializedGraph = graph.GetCourseData(); // Serialize the course data
      Assert.HasCount(1, serializedGraph.Courses); // Ensure we have 1 course in the graph (not added twice)
      Assert.AreEqual("C1", serializedGraph.Courses[0].Name); // Ensure the course is the one we added
    }
    [TestMethod]
    public void AddVertexDegreeTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var course1 = GraphTestHelpers.CreateDegree("C1", []); // Create a dummy course named c1
      graph.AddVertex(course1); // Add the course to the graph
      graph.AddVertex(course1); // Add the course to the graph
      var serializedGraph = graph.GetCourseData(); // Serialize the course data
      Assert.HasCount(0, serializedGraph.Courses); // Ensure we have no courses in the course list
      Assert.HasCount(1, serializedGraph.Degrees); // Ensure we have 1 course in the course list
      Assert.AreEqual("C1", serializedGraph.Degrees[0].Name); // Ensure the course is the one we added
    }
    #endregion
    // AddEdge Testing
    #region AddEdgeTest
    /*
     * NOTE: We don't explicitly test the requirement that all direct and indirect pre- and co reqs are required.
     *
     *       1. All direct and indirect pre- and co-requisites of a required course must be set to required as well.
     * 
     *       This decision was made because in our courseGraph we can have multiple majors, a course may be required 
     *       by one major but not another, it's far easier for us to just consider a required course not by a field
     *       but by the shape of the data. In our graph a course is considered required if it is on a pre-req chain 
     *       of a required course. As such the requirement implicitly get's tested, we do better testing in the schedule
     *       testing for part B to ensure that all "required" courses are added.
     */
    [TestMethod]
    public void AddEdgeSingleTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var course1 = GraphTestHelpers.CreateCourse("C1", [], []); // Create a dummy course named c1
      var course2 = GraphTestHelpers.CreateCourse("C2", [], []); // Create a dummy course named c2
      graph.AddVertex(course1);
      graph.AddVertex(course2);
      graph.AddEdge(course1, course2, CourseGraph.CourseRelation.Prereq);
      Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(graph, course1));
    }
    [TestMethod]
    public void AddEdgeMultipleTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var a = GraphTestHelpers.CreateCourse("C1", [], []);
      var b = GraphTestHelpers.CreateCourse("C2", [], []);
      var c = GraphTestHelpers.CreateCourse("C3", [], []);
      graph.AddVertex(a);
      graph.AddVertex(b);
      graph.AddVertex(c);
      // Add edges (source, to, relation)
      graph.AddEdge(a, b, CourseGraph.CourseRelation.Prereq);
      graph.AddEdge(a, c, CourseGraph.CourseRelation.Prereq);
      graph.AddEdge(b, c, CourseGraph.CourseRelation.Prereq);
      Assert.AreEqual(2, GraphTestHelpers.GetOutgoingEdgeCount(graph, a));
      Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(graph, b));
      Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(graph, c));
    }
    [TestMethod]
    public void AddEdgeDuplicateTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var a = GraphTestHelpers.CreateCourse("C1", [], []);
      var b = GraphTestHelpers.CreateCourse("C2", [], []);
      graph.AddVertex(a);
      graph.AddVertex(b);
      // Add our Initial Edge
      graph.AddEdge(a, b, CourseGraph.CourseRelation.Prereq);
      Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(graph, a));
      Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(graph, b));
      // Add our Duplicate Edge (Same Relation)
      graph.AddEdge(a, b, CourseGraph.CourseRelation.Prereq);
      Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(graph, a));
      Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(graph, b));
      // Add our Duplicate Edge (Different Relation)
      graph.AddEdge(a, b, CourseGraph.CourseRelation.Coreq);
      Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(graph, a));
      Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(graph, b));
    }
    // 3. No course can be a pre- or co-requisite to itself, either directly or indirectly.
    [TestMethod]
    public void AddEdgeDirectCycleTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var a = GraphTestHelpers.CreateCourse("C1", [], []);
      var b = GraphTestHelpers.CreateCourse("C2", [], []);
      graph.AddVertex(a);
      // Add `A` cyclic Edge
      Assert.Throws<ArgumentException>(() => graph.AddEdge(a, a, CourseGraph.CourseRelation.Prereq));
      Assert.Throws<ArgumentException>(() => graph.AddEdge(a, a, CourseGraph.CourseRelation.Coreq));
    }
    [TestMethod]
    public void AddEdgeIndirectCycleTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var a = GraphTestHelpers.CreateCourse("C1", [], []);
      var b = GraphTestHelpers.CreateCourse("C2", [], []);
      var c = GraphTestHelpers.CreateCourse("C2", [], []);
      graph.AddVertex(a);
      graph.AddVertex(b);
      graph.AddVertex(c);
      // Add our basic edges
      graph.AddEdge(a, b, CourseGraph.CourseRelation.Prereq);
      graph.AddEdge(b, c, CourseGraph.CourseRelation.Prereq);
      // Add Cycle: a -> b -> c -> a
      Assert.Throws<ArgumentException>(() => graph.AddEdge(c, a, CourseGraph.CourseRelation.Prereq));
    }
    [TestMethod]
    public void AddEdgeMissingVertexTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var a = GraphTestHelpers.CreateCourse("C1", [], []);
      var b = GraphTestHelpers.CreateCourse("C2", [], []);
      graph.AddVertex(a);
      // Missing Source
      graph.AddEdge(b, a, CourseGraph.CourseRelation.Prereq);
      // Missing Target
      graph.AddEdge(a, b, CourseGraph.CourseRelation.Prereq);
      // Ensure we only have one vertex (i.e vertex not added implicitly)
      var serializedGraph = graph.GetCourseData();
      Assert.HasCount(1, serializedGraph.Courses);
      // Ensure No edges on `A`
      Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(graph, a));
      Assert.AreEqual(-1, GraphTestHelpers.GetOutgoingEdgeCount(graph, b)); // Not in graph
    }
    #endregion
    // RemoveVertex Testing
    #region RemoveVertexTest
    [TestMethod]
    public void RemoveVertexSingleTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var course1 = GraphTestHelpers.CreateCourse("C1", [], []); // Create a dummy course named c1
      graph.AddVertex(course1); // Add the course to the graph
      var serializedGraph1 = graph.GetCourseData();
      Assert.HasCount(1, serializedGraph1.Courses);
      graph.RemoveVertex(course1);
      var serializedGraph2 = graph.GetCourseData();
      Assert.HasCount(0, serializedGraph2.Courses);
    }
    [TestMethod]
    public void RemoveVertexNotInGraphTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var course1 = GraphTestHelpers.CreateCourse("C1", [], []); // Create a dummy course named c1
      var course2 = GraphTestHelpers.CreateCourse("C2", [], []); // Create a dummy course named c2
      graph.RemoveVertex(course1); // Remove when nothing is in the graph
      var serializedGraph1 = graph.GetCourseData();
      Assert.HasCount(0, serializedGraph1.Courses);
      graph.AddVertex(course1); // Add to the graph
      var serializedGraph2 = graph.GetCourseData();
      Assert.HasCount(1, serializedGraph2.Courses);
      graph.RemoveVertex(course2); // Remove something else
      var serializedGraph3 = graph.GetCourseData();
      Assert.HasCount(1, serializedGraph3.Courses);
    }
    // 2. If a course B is removed then its pre- and co-requisite courses become the pre- and co-requisite courses
    // for those course for which B was a pre- and co-requisite.
    [TestMethod]
    public void RemoveVertexRewirePreqTest() {
      // Create our objects
      var graph = new CourseGraph.CourseGraph();
      var course1 = GraphTestHelpers.CreateCourse("C1", [], []);
      var course2 = GraphTestHelpers.CreateCourse("C2", [], []);
      var course3 = GraphTestHelpers.CreateCourse("C2", [], []);
      var course4 = GraphTestHelpers.CreateCourse("C4", [], []);
      // Add Vertices
      graph.AddVertex(course1);
      graph.AddVertex(course2);
      graph.AddVertex(course3);
      graph.AddVertex(course4);
      // Add Relations
      graph.AddEdge(course1, course2, CourseGraph.CourseRelation.Prereq);
      graph.AddEdge(course2, course3, CourseGraph.CourseRelation.Prereq);
      graph.AddEdge(course2, course4, CourseGraph.CourseRelation.Prereq);
      // Check PreRewire
      Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(graph, course1));
      Assert.AreEqual(2, GraphTestHelpers.GetOutgoingEdgeCount(graph, course2));
      Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(graph, course3));
      // Remove Middle Node
      graph.RemoveVertex(course2);
      // Check Rewire
      Assert.AreEqual(2, GraphTestHelpers.GetOutgoingEdgeCount(graph, course1)); // Ensure we propagated the reqs
      Assert.AreEqual(-1, GraphTestHelpers.GetOutgoingEdgeCount(graph, course2));
      Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(graph, course3));
    }
    // 2. If a course B is removed then its pre- and co-requisite courses become the pre- and co-requisite courses
    // for those course for which B was a pre- and co-requisite.
    [TestMethod]
    public void RemoveVertexRewireCoreqTest() {
      // Create our objects
      var graph = new CourseGraph.CourseGraph();
      var course1 = GraphTestHelpers.CreateCourse("C1", [], []);
      var course2 = GraphTestHelpers.CreateCourse("C2", [], []);
      var course3 = GraphTestHelpers.CreateCourse("C2", [], []);
      var course4 = GraphTestHelpers.CreateCourse("C4", [], []);
      // Add Vertices
      graph.AddVertex(course1);
      graph.AddVertex(course2);
      graph.AddVertex(course3);
      graph.AddVertex(course4);
      // Add Relations
      graph.AddEdge(course1, course2, CourseGraph.CourseRelation.Coreq);
      graph.AddEdge(course2, course3, CourseGraph.CourseRelation.Coreq);
      graph.AddEdge(course2, course4, CourseGraph.CourseRelation.Coreq);
      // Check PreRewire
      Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(graph, course1));
      Assert.AreEqual(2, GraphTestHelpers.GetOutgoingEdgeCount(graph, course2));
      Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(graph, course3));
      // Remove Middle Node
      graph.RemoveVertex(course2);
      // Check Rewire
      Assert.AreEqual(2, GraphTestHelpers.GetOutgoingEdgeCount(graph, course1)); // Ensure we propagated the reqs
      Assert.AreEqual(-1, GraphTestHelpers.GetOutgoingEdgeCount(graph, course2));
      Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(graph, course3));
    }
    #endregion
    // RemoveEdge Testing
    #region  RemoveEdgeTest
    [TestMethod]
    public void RemoveEdgeSingleTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var course1 = GraphTestHelpers.CreateCourse("C1", [], []);
      var course2 = GraphTestHelpers.CreateCourse("C2", [], []);
      graph.AddVertex(course1);
      graph.AddVertex(course2);
      graph.AddEdge(course1, course2, CourseGraph.CourseRelation.Prereq);
      // Ensure Edge Exists
      Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(graph, course1));
      // Test Remove
      graph.RemoveEdge(course1, course2);
      Assert.AreEqual(0, GraphTestHelpers.GetOutgoingEdgeCount(graph, course1));
    }
    [TestMethod]
    public void RemoveEdgeMissingTest() {
      var graph = new CourseGraph.CourseGraph(); // Create our graph
      var course1 = GraphTestHelpers.CreateCourse("C1", [], []);
      var course2 = GraphTestHelpers.CreateCourse("C2", [], []);
      graph.AddVertex(course1);
      graph.AddVertex(course2);
      graph.AddEdge(course1, course2, CourseGraph.CourseRelation.Prereq);
      // Ensure Edge Exists
      Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(graph, course1));
      // Test Remove
      graph.RemoveEdge(course2, course1); // This isn't in the graph (should do nothing)
      Assert.AreEqual(1, GraphTestHelpers.GetOutgoingEdgeCount(graph, course1));
    }
    #endregion
    // UpdateVertex Testing
    #region UpdateVertexTest
    // NOTE: This isn't something that really makes sense given our graph it's really just a matter of breaking a link.
    // 4. A course can be updated to required or not required. Note that a course can only be set to not required if
    // it is not a pre- or co-requisite for a required course, and a course that is set to required must update its
    // direct and indirect pre- and co-requisites to required as well (cf Point 1).
    [TestMethod]
    public void NonDegreeThrows() {
      var graph = new CourseGraph.CourseGraph();
      var degreeCourse = GraphTestHelpers.CreateCourse("NonDegree", [], []);
      var course = GraphTestHelpers.CreateCourse("C1", [], []);
      graph.AddVertex(degreeCourse);
      graph.AddVertex(course);
      // This will throw because you can't update the link to a non degree course
      Assert.Throws<ArgumentException>(() => graph.UpdateVertex(course, degreeCourse));
    }
    [TestMethod]
    public void SetRequiredAddsToDegree() {
      var graph = new CourseGraph.CourseGraph();
      var degree = GraphTestHelpers.CreateDegree("Degree", []);
      var course = GraphTestHelpers.CreateCourse("C1", [], []);
      graph.AddVertex(degree);
      graph.AddVertex(course);
      graph.UpdateVertex(course, degree);
      Assert.Contains("C1", degree.PreRequisites);
    }
    [TestMethod]
    public void TogglesOffRemovesFromRequired() {
      var graph = new CourseGraph.CourseGraph();
      var degree = GraphTestHelpers.CreateDegree("Degree", []);
      var course = GraphTestHelpers.CreateCourse("C1", [], []);
      graph.AddVertex(degree);
      graph.AddVertex(course);
      // Enable
      graph.UpdateVertex(course, degree);
      Assert.Contains("C1", degree.PreRequisites);
      // Disable
      graph.UpdateVertex(course, degree);
      Assert.DoesNotContain("C1", degree.PreRequisites);
    }
    #endregion
    #endregion
    [TestMethod]
    public Task SnapshotMermaidTest() {
      // This is just a snapshot test to validate our mermaid output against a large graph
      var rawData = File.ReadAllText("./data/courseData.json");
      var data = JsonSerializer.Deserialize<CourseGraph.CourseData>(rawData);
      var graph = CourseGraph.CourseGraph.FromCourseData(data);
      // Snapshots
      return this.Verify(graph.ToMermaidString(), this.CreateSettings());
    }


    // NOTE: We don't test graph.GetCourseData as it's implicitly tested throughout our test suite
    // NOTE: We don't test graph.FromCourseData as it's implicitly tested throughout our test suite
  }
};
