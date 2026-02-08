/// <summary>
/// This file is used to extract data from the courses.json file which was obtained from the Trent system.
/// We have chosen to not upload courses.json due to licensing issues but we have uploaded courseData.json which contains limited
/// non copyrighted information and mock data about courses at trent.
/// NOTE: THIS IS NOT PART OF THE CORE SCHEDULING ALGORITHM!!!
/// </summary>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CourseGraph;

namespace CourseGenerator {
  /// <summary>
  /// Root model for the raw courses.json file.
  /// </summary>
  public class InputData {
    [JsonPropertyName("Sections")]
    public List<InputSection> Sections { get; set; }
  }

  /// <summary>
  /// Represents a single section entry from courses.json.
  /// </summary>
  public class InputSection {
    [JsonPropertyName("Subject")]
    public string Subject { get; set; }

    [JsonPropertyName("CourseName")]
    public string CourseName { get; set; }

    [JsonPropertyName("SectionDisplay")]
    public string SectionDisplay { get; set; }

    [JsonPropertyName("Meetings")]
    public List<InputMeeting> Meetings { get; set; }

    [JsonPropertyName("Description")]
    public string Description { get; set; }
  }

  /// <summary>
  /// Represents a single meeting time from a section in courses.json.
  /// </summary>
  public class InputMeeting {
    [JsonPropertyName("Days")]
    public List<int> Days { get; set; }

    [JsonPropertyName("StartTime")]
    public string StartTime { get; set; }

    [JsonPropertyName("EndTime")]
    public string EndTime { get; set; }

    [JsonPropertyName("IsOnline")]
    public bool IsOnline { get; set; }

    [JsonPropertyName("ShowTBD")]
    public bool ShowTBD { get; set; }
  }

  // -------------------- Requirement Assigner --------------------

  public static class RequirementAssigner {
    public static (List<string> PreReqs, List<string> CoReqs) GetRequirements(string courseName, HashSet<string> allValidCourses) {
      var pre = new List<string>();
      var co = new List<string>();

      var parts = courseName.Split('-');
      if (parts.Length < 2) return (pre, co);

      string subject = parts[0];
      string codeStr = parts[1];

      var numericMatch = Regex.Match(codeStr, @"\d+");
      if (!numericMatch.Success) return (pre, co);
      int currentCode = int.Parse(numericMatch.Value);
      string suffix = codeStr.Replace(numericMatch.Value, "");

      // 1. DYNAMIC OFFSET LOGIC (e.g., 3020 -> 2020)
      if (currentCode >= 2000) {
        string offsetPre = $"{subject}-{currentCode - 1000}{suffix}";
        if (allValidCourses.Contains(offsetPre)) {
          pre.Add(offsetPre);
        }
      }

      // 2. COMPUTER SCIENCE SPECIFIC LOGIC
      if (subject == "COIS" || subject == "AIST") {
        // Programming Gate: Most 2000+ technical courses need Programming
        if (currentCode >= 2000 && currentCode != 2600 && currentCode != 2750 && currentCode != 2830) {
          if (allValidCourses.Contains("COIS-1020H")) pre.Add("COIS-1020H");
        }

        // Advanced Systems/AI Gates: Need Data Structures
        if (currentCode >= 3000 && (currentCode == 3320 || currentCode == 4550 || currentCode == 4400)) {
          if (allValidCourses.Contains("COIS-2020H")) pre.Add("COIS-2020H");
        }

        // Software Stream
        if (codeStr == "3040H") pre.Add("COIS-2240H");
        if (codeStr == "3060H") pre.Add("COIS-2620H");

        // AI/Data Science Foundations
        if (subject == "AIST" || currentCode == 4400 || currentCode == 4450) {
          if (allValidCourses.Contains("COIS-1400H")) pre.Add("COIS-1400H");
          if (allValidCourses.Contains("MATH-1550H")) pre.Add("MATH-1550H");
        }
      }

      switch (subject) {
        case "ADMN":
          if (codeStr == "3200H") {
            if (allValidCourses.Contains("ADMN-1221H")) pre.Add("ADMN-1221H");
            if (allValidCourses.Contains("ECON-1010H")) pre.Add("ECON-1010H");
          }
          break;
        case "MATH":
          if (currentCode == 1120 && allValidCourses.Contains("MATH-1110H")) pre.Add("MATH-1110H");
          break;
        case "NURS":
          if (codeStr.Contains("20") || codeStr.Contains("21")) {
            string theory = codeStr.Replace("20", "00").Replace("21", "01");
            if (allValidCourses.Contains($"{subject}-{theory}")) co.Add($"{subject}-{theory}");
          }
          break;
      }

      return (pre.Distinct().ToList(), co.Distinct().ToList());
    }
  }

  // -------------------- Generator --------------------

  public static class CourseGenerator {
    public static void GenerateAndWrite(string inputPath, string outputPath, int? seed = 42) {
      var courseData = Generate(inputPath, seed);

      var options = new JsonSerializerOptions {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
      };

      File.WriteAllText(outputPath, JsonSerializer.Serialize(courseData, options));
      Console.WriteLine($"Generated {courseData.Courses.Count} courses across {courseData.Degrees.Count} degrees");
      Console.WriteLine($"Output written to {outputPath}");
    }

    public static CourseData Generate(string inputPath, int? seed = 42) {
      var jsonString = File.ReadAllText(inputPath);
      var inputData = JsonSerializer.Deserialize<InputData>(jsonString);
      var random = seed.HasValue ? new Random(seed.Value) : new Random();

      var validCourseNames = inputData.Sections
                                .Select(s => s.CourseName)
                                .Distinct()
                                .ToHashSet();

      var courseGroups = inputData.Sections
        .GroupBy(s => s.CourseName)
        .ToDictionary(g => g.Key, g => g.ToList());

      var subjectCourses = new Dictionary<string, List<string>>();
      var courses = new List<Course>();

      // Build timetable per course once, so we know exactly which courses will be included.
      // Requirement assigner must only reference included courses so we don't add prereqs to missing courses.
      var courseTimeTableInfos = new Dictionary<string, TimeTableInfo[]>();
      foreach (var kvp in courseGroups) {
        var courseName = kvp.Key;
        var sections = kvp.Value;
        var subject = sections[0].Subject;

        var timeTableInfos = new List<TimeTableInfo>();
        foreach (var section in sections) {
          var info = TryBuildTimeTableInfo(section, random);
          if (info != null)
            timeTableInfos.Add(info);
        }
        if (timeTableInfos.Count > 0) {
          courseTimeTableInfos[courseName] = timeTableInfos.ToArray();
        }
      }

      var includedCourseNames = new HashSet<string>(courseTimeTableInfos.Keys);

      foreach (var kvp in courseGroups) {
        var courseName = kvp.Key;
        if (!courseTimeTableInfos.TryGetValue(courseName, out var timeTableInfos))
          continue;

        var sections = kvp.Value;
        var subject = sections[0].Subject;

        // Get realistic requirements using only courses that are actually in the output
        var (preReqs, coReqs) = RequirementAssigner.GetRequirements(courseName, includedCourseNames);

        var course = new Course(
          name: courseName,
          coRequisites: coReqs,
          preRequisites: preReqs,
          timeTableInfos: timeTableInfos
        );

        courses.Add(course);

        if (!subjectCourses.ContainsKey(subject))
          subjectCourses[subject] = new List<string>();
        subjectCourses[subject].Add(courseName);
      }

      var degrees = new List<Course>();
      foreach (var kvp in subjectCourses) {
        var degree = new Course(
          name: kvp.Key,
          coRequisites: new List<string>(),
          preRequisites: kvp.Value,
          timeTableInfos: Array.Empty<TimeTableInfo>(),
          isDegree: true
        );
        degrees.Add(degree);
      }

      return new CourseData { Degrees = degrees, Courses = courses };
    }

    private static TimeTableInfo TryBuildTimeTableInfo(InputSection section, Random random) {
      if (section.Meetings == null || section.Meetings.Count == 0)
        return null;

      var timeSlots = new List<TimeSlot>();

      foreach (var meeting in section.Meetings) {
        if (meeting.IsOnline || meeting.ShowTBD)
          continue;
        if (string.IsNullOrEmpty(meeting.StartTime) || string.IsNullOrEmpty(meeting.EndTime))
          continue;
        if (meeting.Days == null || meeting.Days.Count == 0)
          continue;

        if (!TimeOnly.TryParse(meeting.StartTime, out var startTime) ||
            !TimeOnly.TryParse(meeting.EndTime, out var endTime))
          return null;

        if (startTime < TimeTableInfo.EarliestTime || endTime > TimeTableInfo.LatestTime)
          return null;
        if (startTime >= endTime)
          return null;

        foreach (var day in meeting.Days) {
          var dayOfWeek = (DayOfWeek)day;
          if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
            return null;
          timeSlots.Add(new TimeSlot(dayOfWeek, startTime, endTime));
        }
      }

      if (timeSlots.Count == 0)
        return null;

      var term = random.Next(2) == 0 ? Term.Fall : Term.Winter;

      return new TimeTableInfo {
        OfferedTerm = term,
        TimeSlots = timeSlots.ToArray()
      };
    }
  }
}
