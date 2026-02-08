using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CourseGraph {
  /// <summary>
  /// Represents which term a course is offered in.
  /// </summary>
  public enum Term {
    Fall,
    Winter,
  }

  /// <summary>
  /// Represents a time slot for a course. Each time slot has a day of the week, start time, and end time.
  /// </summary>
  /// <param name="Day">The day of the week the time slot is scheduled for.</param>
  /// <param name="Start">The start time of the time slot.</param>
  /// <param name="End">The end time of the time slot.</param>
  public record TimeSlot(DayOfWeek Day, TimeOnly Start, TimeOnly End);
  /// <summary>
  /// Represents the timetable information about a course.
  /// </summary>
  public record TimeTableInfo {
    /// <summary>
    /// The earliest time a course can be scheduled (24-hour time).
    /// </summary>
    public static TimeOnly EarliestTime = new TimeOnly(8, 0);
    /// <summary>
    /// The latest time a course can be scheduled (24-hour time).
    /// </summary>
    public static TimeOnly LatestTime = new TimeOnly(22, 0);
    /// <summary>
    /// The term the course is offered in.
    /// </summary>
    public Term OfferedTerm { get; init; }
    /// <summary>
    /// The time slots the course is offered in. 
    /// Each tuple represents a single time slot with the:
    /// day of the week, start time, and end time.
    /// </summary>
    public TimeSlot[] TimeSlots { get; init; }
  }
  /// <summary>
  /// Represents a course.
  /// </summary>
  public class Course {
    /// <summary>
    /// The name of the course.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Marks weather a course represents a degree.
    /// 
    /// NOTE: Simplification, if a course is a pre-requisite to the degree course it is implicitly required
    /// </summary>
    public bool IsDegree { get; }
    /// <summary>
    /// The co-requisites of the course.
    /// These are courses that must be taken in the same term or prior to the course.
    /// </summary>
    public List<string> CoRequisites { get; }
    /// <summary>
    /// The pre-requisites of the course.
    /// These are courses that must be taken prior to the course.
    /// </summary>
    public List<string> PreRequisites { get; }
    /// <summary>
    /// The timetable information of the course.
    /// </summary>
    public TimeTableInfo[] TimeTableInfos { get; }

    /// <summary>
    /// Builds a new course with the set parameters.
    /// </summary>
    /// <param name="name">The name of the course.</param>
    /// <param name="coRequisites">The coRequisite course names.</param>
    /// <param name="preRequisites">The preRequisite course names.</param>
    /// <param name="timeTableInfo">The timeTableInfo.</param>
    /// <param name="isDegree">Weather the course is real or not.</param>
    /// <exception cref="ArgumentException">If a degree course has timetable info.</exception>
    /// <exception cref="ArgumentException">If a degree course has coRequisite info.</exception>
    /// <exception cref="ArgumentException">If a non-degree course does not have time table info .</exception>
    /// <exception cref="ArgumentException">If any of the time slots have invalid times.</exception>
    public Course(
      string name,
      List<string> coRequisites,
      List<string> preRequisites,
      TimeTableInfo[] timeTableInfos,
      bool isDegree = false
    ) {
      this.Name = name;
      this.IsDegree = isDegree;
      this.CoRequisites = coRequisites;
      this.PreRequisites = preRequisites;
      this.TimeTableInfos = timeTableInfos;
      // Validation
      if (isDegree) {
        if (timeTableInfos.Length > 0)
          throw new ArgumentException("Degree courses cannot have timetable info");
        if (coRequisites.Count > 0)
          throw new ArgumentException("Degree courses cannot have co-requisites");
      } else {
        if (timeTableInfos.Length == 0)
          throw new ArgumentException("Non Degree courses must have timetable info");
      }
      foreach (var offering in timeTableInfos) {
        foreach (var timeSlot in offering.TimeSlots) {
          if (timeSlot.Start >= timeSlot.End)
            throw new ArgumentException("Course time slot start time must be before end time");
          if (timeSlot.Start < TimeTableInfo.EarliestTime || timeSlot.End > TimeTableInfo.LatestTime)
            throw new ArgumentException("Course time slots must be between 8:00 and 22:00");
          if (timeSlot.Day == DayOfWeek.Sunday || timeSlot.Day == DayOfWeek.Saturday)
            throw new ArgumentException("Courses cannot appear on the weekend");
        }
      }
    }

    public override string ToString() {
      return JsonSerializer.Serialize(this, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
    }
  }
}
