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
  /// Represents a single time occurrence (one day, start time, end time).
  /// </summary>
  public record TimeOccurrence(DayOfWeek Day, TimeOnly Start, TimeOnly End);

  /// <summary>
  /// Represents a time slot for a course. Each time slot can have multiple times (e.g. Mon/Wed/Fri 9-10).
  /// </summary>
  /// <param name="Times">The list of time occurrences for this slot.</param>
  public record TimeSlot(TimeOccurrence[] Times);
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
    /// Each time slot can have multiple times (e.g. recurring Mon/Wed/Fri).
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
    /// Marks weather a course is real or just an informative node.
    /// A phantom course may for instance be a degree field or root node.
    /// 
    /// NOTE: Simplification, if a course is a pre-requisite to the phantom degree course it is implicitly required
    /// </summary>
    public bool IsPhantom { get; }
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
    /// <param name="isPhantom">Weather the course is real or not.</param>
    /// <exception cref="ArgumentException">If a phantom course has timetable info.</exception>
    /// <exception cref="ArgumentException">If a phantom course has coRequisite info.</exception>
    /// <exception cref="ArgumentException">If a non-phantom course does not have time table info .</exception>
    /// <exception cref="ArgumentException">If any of the time slots have invalid times.</exception>
    public Course(
      string name,
      List<string> coRequisites,
      List<string> preRequisites,
      TimeTableInfo[] timeTableInfos,
      bool isPhantom = false
    ) {
      this.Name = name;
      this.IsPhantom = isPhantom;
      this.CoRequisites = coRequisites;
      this.PreRequisites = preRequisites;
      this.TimeTableInfos = timeTableInfos;
      // Validation
      if (isPhantom) {
        if (timeTableInfos.Length > 0)
          throw new ArgumentException("Phantom courses cannot have timetable info");
        if (coRequisites.Count > 0)
          throw new ArgumentException("Phantom courses cannot have co-requisites");
      } else {
        if (timeTableInfos.Length == 0)
          throw new ArgumentException("Non phantom courses must have timetable info");
      }
      foreach (var offering in timeTableInfos) {
        foreach (var timeSlot in offering.TimeSlots) {
          if (timeSlot.Times == null) throw new ArgumentException("TimeSlot.Times cannot be null");
          foreach (var t in timeSlot.Times) {
            if (t.Start >= t.End)
              throw new ArgumentException("Course time slot start time must be before end time");
            if (t.Start < TimeTableInfo.EarliestTime || t.End > TimeTableInfo.LatestTime)
              throw new ArgumentException("Course time slots must be between 8:00 and 22:00");
            if (t.Day == DayOfWeek.Sunday || t.Day == DayOfWeek.Saturday)
              throw new ArgumentException("Courses cannot appear on the weekend");
          }
        }
      }
    }

    public override string ToString() {
      return JsonSerializer.Serialize(this, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
    }
  }
}
