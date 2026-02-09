using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyMSTest;
using VerifyTests;
using CourseGraph;

namespace A1Tests {
  [TestClass]
  public class PartBTest : VerifyBase {
    // NOTE: Every test is done as a snapshot test for testing simplification
    // NOTE: If we had a bit more time it would probably make sense to make more helpers and use unit tests more.
    private VerifySettings CreateSettings() {
      var settings = new VerifySettings();
      settings.UseDirectory(System.IO.Path.Combine("Snapshots", nameof(A1Tests)));
      return settings;
    }
    #region EdgeCases
    [TestMethod]
    public Task SnapshotSingleCourseTest() {
      var data = new CourseData {
        Degrees = [
          GraphTestHelpers.CreateDegree("COIS", ["1010"])
        ],
        Courses = [
          GraphTestHelpers.CreateCourse("1010", [], [])
        ]
      };
      var graph = CourseGraph.CourseGraph.FromCourseData(data);
      var schedule = graph.Schedule(termSize: 5, creditCount: data.Courses.Count, degreeCourse: data.GetDegreeByName("COIS"));
      // Snapshots
      return this.Verify(schedule.ToString(), this.CreateSettings());
    }
    [TestMethod]
    public Task SnapshotSingleCoursePreReqTest() {
      var data = new CourseData {
        Degrees = [
          GraphTestHelpers.CreateDegree("COIS", preRequisites: ["1020"])
        ],
        Courses = [
          GraphTestHelpers.CreateCourse("1010", preRequisites: [], coRequisites: []),
          GraphTestHelpers.CreateCourse("1020", preRequisites: ["1010"], coRequisites: [])
        ]
      };
      var graph = CourseGraph.CourseGraph.FromCourseData(data);
      var schedule = graph.Schedule(termSize: 5, creditCount: data.Courses.Count, degreeCourse: data.GetDegreeByName("COIS"));
      // Snapshots
      return this.Verify(schedule.ToString(), this.CreateSettings());
    }
    [TestMethod]
    public Task SnapshotSingleCourseCoReqTest() {
      var data = new CourseData {
        Degrees = [
          GraphTestHelpers.CreateDegree("COIS", preRequisites: ["1020"])
        ],
        Courses = [
          GraphTestHelpers.CreateCourse("1010", preRequisites: [], coRequisites: []),
          GraphTestHelpers.CreateCourse("1020", preRequisites: [], coRequisites: ["1010"])
        ]
      };
      var graph = CourseGraph.CourseGraph.FromCourseData(data);
      var schedule = graph.Schedule(termSize: 5, creditCount: data.Courses.Count, degreeCourse: data.GetDegreeByName("COIS"));
      // Snapshots
      return this.Verify(schedule.ToString(), this.CreateSettings());
    }
    // Validation Tests
    #endregion
    [TestMethod]
    public Task SnapshotTrentData() {
      // This is a stress test against real data from trent's course planner
      var rawData = File.ReadAllText("./data/courseData.json");
      var data = JsonSerializer.Deserialize<CourseGraph.CourseData>(rawData);
      var graph = CourseGraph.CourseGraph.FromCourseData(data);
      // This is going to build the optimal schedule for a COIS major
      // NOTE: A termSize of 5 is chosen as trent allows 5 courses per term
      // NOTE: A creditCount of 40 is chosen because trent requires 20 credits 
      //       but each course is worth 0.5 credits vs our system where they are worth 1 credit
      var schedule = graph.Schedule(termSize: 5, creditCount: 40, degreeCourse: data.GetDegreeByName("COIS"));
      // Snapshots
      return this.Verify(schedule.ToString(), this.CreateSettings());
    }
  }
};
