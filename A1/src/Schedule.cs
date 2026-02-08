using System;
using System.Linq;
using Spectre.Console; // A library for pretty console output
using CourseGraph;
using System.Collections.Generic;
using System.IO;

namespace Schedule {
  public class Schedule {
    /// <summary>
    /// The default time increment when displaying schedules
    /// </summary>
    /// TODO: Set this to 30
    private readonly int TimeIncrement = 60;
    /// <summary>
    /// The degree the schedule is built for.
    /// </summary>
    public readonly Course Degree;
    /// <summary>
    /// The number of required credits.
    /// </summary>
    private readonly int MaxTermSize;
    /// <summary>
    /// The term the schedule starts in.
    /// This is important as it lets us determine which timeSlot to use,
    /// we assume that terms are sequential.
    /// </summary>
    private readonly Term StartingTerm;
    /// <summary>
    /// A matrix of every term and every bucket.
    /// </summary>
#nullable enable
    private List<(Course course, TimeTableInfo[] timeTableInfo)?[]> TermData;
    /// <summary>
    /// A mapping of course names to scheduled term.
    /// This allows us to quickly lookup if a course is scheduled or not.
    /// </summary>
    private Dictionary<string, int> ScheduledCourses;
    /// <summary>
    /// The number of courses in our schedule 
    /// </summary>
    public int CourseCount { get; private set; }

    /// <summary>
    /// Build's a new schedule with the set parameters.
    /// </summary>
    public Schedule(Course degree, int maxTermSize, Term startingTerm = Term.Fall) {
      this.Degree = degree;
      this.MaxTermSize = maxTermSize;
      this.StartingTerm = startingTerm;
      this.TermData = new List<(Course course, TimeTableInfo[] timeTableInfo)?[]>();
      this.ScheduledCourses = new Dictionary<string, int>();
      this.CourseCount = 0;
    }

    // ------------------------- Internal Methods -------------------------

    // ------------------------- Mutation Methods -------------------------

    /// <summary>
    /// Adds a course to the schedule in the desired term.
    /// </summary>
    /// <param name="course">The course to add to the schedule.</param>
    /// <param name="term">The term to add the course to</param>
    /// <exception cref="ArgumentException">If a degree course is being added</exception>
    /// <exception cref="ArgumentException">If a course is being added to a non existing term</exception>
    /// <exception cref="ArgumentException">If the given term is full.</exception>
    /// <exception cref="ArgumentException">If the course is not offered in the given term.</exception>
    /// <exception cref="ArgumentException">If the course overlaps with another course in the given term.</exception>
    public void AddCourse(Course course, int term) {
      // Input Validation
      if (course.IsDegree)
        throw new ArgumentException("Cannot add a degree course to a schedule");
      if (term < 0)
        throw new ArgumentException("Cannot add a course outside of the schedule");
      // Initial TimeSlot Validation (check if the course is offered in the term)
      Term termType = this.GetTermType(term);
      var possibleTimeSlots = course.TimeTableInfos.Where(t => t.OfferedTerm == termType);
      if (!possibleTimeSlots.Any())
        throw new ArgumentException($"Course {course.Name} is not offered in term {term}");
      var nonOverlappingTimeSlots = this.GetCourseValidTimeSlots(course, possibleTimeSlots.ToArray(), term);
      if (nonOverlappingTimeSlots.Count <= 0)
        throw new ArgumentException($"Course {course.Name}, can't be scheduled do to overlaps in term {term}");
      // Grow the term table to fit
      while (this.TermData.Count <= term) {
        this.TermData.Add(new (Course course, TimeTableInfo[] timeTableInfo)?[this.MaxTermSize]);
      }
      // Try to add the course to the first available slot in the term
      for (int slotIndex = 0; slotIndex < this.MaxTermSize; slotIndex++) {
        var currentSlot = this.TermData[term][slotIndex];
        if (currentSlot != null) continue;
        this.TermData[term][slotIndex] = (course, nonOverlappingTimeSlots.ToArray());
        this.ScheduledCourses.Add(course.Name, term);
        this.CourseCount++;
        return;
      }
      throw new ArgumentException("Cannot add a course to a full term");
    }

    // --------------------------- Info Methods ----------------------------

    /// <summary>
    /// Determines if two time table infos have any overlapping time slots.
    /// Time Complexity: O(n*m) where n and m are the number of time slots for each course.
    /// </summary>
    /// <param name="timeTableInfo1">The first time table info to compare.</param>
    /// <param name="timeTableInfo2">The second time table info to compare.</param>
    /// <returns>True if the two time table infos have any overlapping time slots, false otherwise.</returns>
    public bool DoTimeSlotsOverlap(TimeTableInfo timeTableInfo1, TimeTableInfo timeTableInfo2) {
      foreach (var timeSlot1 in timeTableInfo1.TimeSlots) {
        foreach (var timeSlot2 in timeTableInfo2.TimeSlots) {
          if (timeSlot1.Day == timeSlot2.Day) {
            // Check for time overlap
            if (timeSlot1.Start < timeSlot2.End && timeSlot2.Start < timeSlot1.End) {
              return true;
            }
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Checks if the given time slot is already taken.
    /// </summary>
    /// <param name="timeTableInfo">The time slot to check against</param>
    /// <param name="term">which term we are checking in</param>
    /// <returns></returns>
    public List<TimeTableInfo> GetCourseValidTimeSlots(Course course, TimeTableInfo[] timeTableInfo, int term) {
      var possibleTimeSlots = new List<TimeTableInfo>(timeTableInfo); // Clone the array
      if (this.TermData.Count <= term) return possibleTimeSlots;
      // Check if timeTableInfo has at least one item where the time doesn't overlap with anything else in this.TermData[term]
      for (int slotIndex = 0; slotIndex < this.MaxTermSize && possibleTimeSlots.Count > 0; slotIndex++) {
        var slotData = this.TermData[term][slotIndex];
        if (slotData == null) break;
        var (checkCourse, checkTimeSlots) = slotData.Value;
        if (checkCourse == course) continue;
        possibleTimeSlots.RemoveAll(t => checkTimeSlots.All(c => this.DoTimeSlotsOverlap(c, t)));
      }
      return possibleTimeSlots;
    }

    /// <summary>
    /// Determines which term type (Fall/Winter) a term index corresponds to based on the starting term.
    /// Time Complexity: O(1)
    /// </summary>
    /// <param name="termIndex">The index of the term to get the type for.</param>
    /// <returns>The term type (Fall/Winter) for the given term index.</returns>
    public Term GetTermType(int termIndex) {
      return (Term)(((int)this.StartingTerm + termIndex) % Enum.GetValues(typeof(Term)).Length);
    }

    /// <summary>
    /// Checks if a given term is full or not.
    /// </summary>
    /// <param name="term">The term we want to check</param>
    /// <returns>`true` if the term is full, otherwise `false`</returns>
    public bool IsTermFull(int term) {
      if (this.TermData.Count <= term) return false;
      foreach (var slot in this.TermData[term]) if (slot == null) return false;
      return true;
    }

    /// <summary>
    /// Queries which term a course is scheduled for.
    /// </summary>
    /// <param name="course">The course you are looking for.</param>
    /// <returns>The term the course is scheduled in, or `-1` if not found.</returns>
    public int GetCourseTerm(Course course) {
      return this.ScheduledCourses.GetValueOrDefault(course.Name, -1);
    }

    // ------------------------- Display Methods --------------------------

    /// <summary>
    /// Generate the schedule as a table.
    /// 
    /// Time Complexity: O(TermCount * MaxTermSize)
    /// </summary>
    private Table[] GenerateSchedule() {
      // Generate the table
      var tables = new Table[this.TermData.Count];
      // Helper function to create a colored markup for occupied cells
      var colors = new[] { Color.Blue, Color.Red, Color.Yellow, Color.Green, Color.Violet };
      Markup UsedCell(string name, int slot) => new Markup(name, new Style(foreground: null, background: colors[slot % colors.Length]));
      // Print a schedule table for each term
      for (int termIndex = 0; termIndex < this.TermData.Count; termIndex++) {
        // Filter out any overlapping timeslots
        for (int slotIndex = 0; slotIndex < this.TermData[termIndex].Length; slotIndex++) {
          var slot = this.TermData[termIndex][slotIndex];
          if (slot == null) break; // Skip empty slots (Courses are added sequentially)
          var (course, timeTableInfo) = slot.Value;
          var validTimeSlots = this.GetCourseValidTimeSlots(course, timeTableInfo, termIndex).ToArray();
          if (!validTimeSlots.Any()) {
            throw new Exception($"Impossible: No valid timeslot available for course {course.Name} in term {termIndex}");
          }
          this.TermData[termIndex][slotIndex] = (course, validTimeSlots);
        }
        // Determine which term we are in based on the starting term and the term index
        Term currentTerm = this.GetTermType(termIndex);
        // Build the initial table with time slots
        var table = new Table().Border(TableBorder.Rounded).ShowRowSeparators();
        table.Title($"Term {termIndex} - {currentTerm} Schedule");
        // Add a column for each day of the week plus an initial column for the time slots
        table.AddColumns(
          new[] { "Time" }
            .Concat(Enum.GetValues(typeof(DayOfWeek)) // Get the names of the days of the week from the enum
            .Cast<DayOfWeek>() // Convert the enum values to the enum type
            .Select(d => d.ToString())) // Convert the enum values to their string representation
            .ToArray() // Convert the IEnumerable to an array for the AddColumns method
        );
        // Add a row for every 30 minutes from the start to the end
        for (TimeOnly time = TimeTableInfo.EarliestTime; time <= TimeTableInfo.LatestTime; time = time.AddMinutes(this.TimeIncrement)) {
          // Create an array to represent the schedule for the current time slot
          var scheduleRow = new (Course course, int slot)?[Enum.GetValues(typeof(DayOfWeek)).Length];
          // Search all course entry's in the term
          for (int slotIndex = 0; slotIndex < this.TermData[termIndex].Length; slotIndex++) {
            var slot = this.TermData[termIndex][slotIndex];
            if (slot == null) break; // Skip empty slots (Courses are added sequentially)
            var (course, timeTableInfo) = slot.Value;
            // Make a list of all the time slots that are at the current time.
            // NOTE: We could let the user input bias's which we could use to further select these
            var overlappingTimeSlots = timeTableInfo[0].TimeSlots.Where(ts => ts.Start <= time && ts.End > time);
            if (!overlappingTimeSlots.Any()) continue;
            foreach (var timeSlot in overlappingTimeSlots) {
              // Mark the corresponding day in the schedule row as occupied
              scheduleRow[(int)timeSlot.Day] = (course, slotIndex);
            }
          }
          // Add the row entry
          table.AddRow(
            new[] { new Markup(time.ToString("HH:mm")) }
              .Concat(scheduleRow.Select(e => e != null ? UsedCell(e.Value.course.Name, e.Value.slot) : new Markup("")))
              .ToArray()
          );
        }
        tables[termIndex] = table;
      }
      return tables;
    }

    /// <summary>
    /// Generate the schedule as a table.
    /// 
    /// Time Complexity: O(TermCount * MaxTermSize)
    /// </summary>
    public void PrintSchedule() {
      var tables = this.GenerateSchedule();
      foreach (var table in tables) {
        AnsiConsole.Write(table);
      }
    }

    /// <summary>
    /// Writes the schedule to a file.
    /// </summary>
    /// <param name="fileName">The file to write to</param>
    public void WriteScheduleToFile(string fileName) {
      var tables = this.GenerateSchedule();
      // Render to a string
      var sw = new StringWriter();
      var console = AnsiConsole.Create(new AnsiConsoleSettings {
        Ansi = AnsiSupport.No,
        ColorSystem = ColorSystemSupport.NoColors,
        Interactive = InteractionSupport.No,
        // Forward console data to StringWriter
        Out = new AnsiConsoleOutput(sw)
      });
      foreach (var table in tables) console.Write(table);

      File.WriteAllText(fileName, sw.ToString());
    }
  }
}
