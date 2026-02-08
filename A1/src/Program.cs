using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using CourseGraph;

// Load Data
var jsonString = File.ReadAllText("./courseData.json");
var loadedCourseData = JsonSerializer.Deserialize<CourseData>(jsonString);
var courseGraph = CourseGraph.CourseGraph.FromCourseData(loadedCourseData);
// Test Scheduling
var coisDegree = loadedCourseData.GetDegreeByName("COIS");
var sw = Stopwatch.StartNew();
var schedule = courseGraph.Schedule(termSize: 5, creditCount: Math.Min(40, loadedCourseData.Courses.Count), degreeCourse: coisDegree);
sw.Stop();
schedule.PrintSchedule();
foreach (var msg in courseGraph.MissedOpportunities) {
  Console.WriteLine(msg);
}
Console.WriteLine($"Failed Placements: {courseGraph.MissedOpportunities.Count}");
Console.WriteLine($"Elapsed time: {sw.Elapsed.TotalMilliseconds} ms");
schedule.WriteScheduleToFile("./schedule.md");
// Write outputs
var outputCourseData = courseGraph.GetCourseData();
File.WriteAllText(
  "./courseData.json",
  JsonSerializer.Serialize(
    outputCourseData,
    new JsonSerializerOptions {
      Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
      WriteIndented = true
    }
  )
);
courseGraph.WriteToFile("./courseGraph.md");

// // Build Schedule
// var schedule = new Schedule.Schedule(degree: coisDegree, termCount: 1, maxTermSize: 5, startingTerm: Term.Fall);
// // Build First Term
// schedule.AddCourse(cois1010, cois1010.TimeTableInfos[0], 0);
// schedule.AddCourse(cois1020, cois1020.TimeTableInfos[0], 0);
// // Print Schedule
// schedule.PrintSchedule();
