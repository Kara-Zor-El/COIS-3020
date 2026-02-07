using System;
using System.Linq;
using Spectre.Console; // A library for pretty console output
using CourseGraph;

namespace Schedule {
  public class Schedule {
    /// <summary>
    /// The degree the schedule is built for.
    /// </summary>
    public readonly Course Degree;
    /// <summary>
    /// The maximum number of credit's in a term.
    /// </summary>
    private readonly int TermCount;
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
    private (Course course, TimeTableInfo timeTableInfo)?[,] TermData;

    /// <summary>
    /// Build's a new schedule with the set parameters.
    /// </summary>
    public Schedule(Course degree, int termCount, int maxTermSize, Term startingTerm = Term.Fall) {
      this.Degree = degree;
      this.TermCount = termCount;
      this.MaxTermSize = maxTermSize;
      this.StartingTerm = startingTerm;
      this.TermData = new (Course course, TimeTableInfo timeTableInfo)?[termCount, maxTermSize];
    }

    // ------------------------- Internal Methods -------------------------

    /// <summary>
    /// Determines which term type (Fall/Winter) a term index corresponds to based on the starting term.
    /// Time Complexity: O(1)
    /// </summary>
    /// <param name="termIndex">The index of the term to get the type for.</param>
    /// <returns>The term type (Fall/Winter) for the given term index.</returns>
    private Term GetTermType(int termIndex) {
      return (Term)(((int)this.StartingTerm + termIndex) % Enum.GetValues(typeof(Term)).Length);
    }

    /// <summary>
    /// Determines if two time table infos have any overlapping time slots.
    /// Time Complexity: O(n*m) where n and m are the number of time slots for each course.
    /// </summary>
    /// <param name="timeTableInfo1">The first time table info to compare.</param>
    /// <param name="timeTableInfo2">The second time table info to compare.</param>
    /// <returns>True if the two time table infos have any overlapping time slots, false otherwise.</returns>
    private bool DoTimeSlotsOverlap(TimeTableInfo timeTableInfo1, TimeTableInfo timeTableInfo2) {
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

    // ------------------------- Mutation Methods -------------------------

    /// <summary>
    /// Adds a course to the schedule in the desired term.
    /// 
    /// Time Complexity: O(TermCount * MaxTermSize) in the worst case (when all terms are full and we have to check for overlaps with every course), but typically much better when there are empty slots.
    /// </summary>
    /// <param name="course">The course to add to the schedule.</param>
    /// <param name="term">The term to add the course to</param>
    /// <exception cref="ArgumentException">If a phantom course is being added</exception>
    /// <exception cref="ArgumentException">If a course is being added to a non existing term</exception>
    /// <exception cref="ArgumentException">If the given term is full.</exception>
    /// <exception cref="ArgumentException">If the course is not offered in the given term.</exception>
    /// <exception cref="ArgumentException">If the course overlaps with another course in the given term.</exception>
    public void AddCourse(Course course, TimeTableInfo timeTableInfo, int term) {
      // TODO: Lookup available timeTableInfo from course rather than take it in as a parameter
      // Input Validation
      if (course.IsPhantom)
        throw new ArgumentException("Cannot add a phantom course to a schedule");
      if (term > this.TermCount || term < 0)
        throw new ArgumentException("Cannot add a course outside of the schedule");
      // Initial TimeSlot Validation (check if the course is offered in the term)
      Term courseTermType = this.GetTermType(term);
      if (timeTableInfo.OfferedTerm != courseTermType)
        throw new ArgumentException($"Course {course.Name} is not offered in term {term}");
      // Try to add the course to the first available slot in the term
      for (int slotIndex = 0; slotIndex < this.MaxTermSize; slotIndex++) {
        var currentSlot = this.TermData[term, slotIndex];
        if (currentSlot != null) {
          if (this.DoTimeSlotsOverlap(currentSlot.Value.timeTableInfo, timeTableInfo)) {
            throw new ArgumentException($"Course {course.Name} overlaps with course {currentSlot.Value.course.Name} in term {term}");
          }
          continue;
        } else {
          this.TermData[term, slotIndex] = (course, timeTableInfo);
          return;
        }
      }
      throw new ArgumentException("Cannot add a course to a full term");
    }

    // ------------------------- Display Methods --------------------------

    /// <summary>
    /// Prints the schedule to the console as a table.
    /// 
    /// Time Complexity: O(TermCount * MaxTermSize)
    /// </summary>
    public void PrintSchedule() {
      // Helper function to create a colored markup for occupied cells
      Markup UsedCell(string name) => new Markup(name, new Style(foreground: null, background: Color.Blue));
      // Print a schedule table for each term
      for (int termIndex = 0; termIndex < this.TermData.GetLength(0); termIndex++) {
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
        for (TimeOnly time = TimeTableInfo.EarliestTime; time <= TimeTableInfo.LatestTime; time = time.AddMinutes(30)) {
          // Create an array to represent the schedule for the current time slot
          var scheduleRow = new Course?[Enum.GetValues(typeof(DayOfWeek)).Length];
          // Search all course entry's in the term
          for (int slotIndex = 0; slotIndex < this.TermData.GetLength(1); slotIndex++) {
            var slot = this.TermData[termIndex, slotIndex];
            if (slot == null) break; // Skip empty slots (Courses are added sequentially)
            var (course, timeTableInfo) = slot.Value;
            // Make a list of all the time slots that are at the current time.
            var overlappingTimeSlots = timeTableInfo.TimeSlots.Where(ts => ts.Start <= time && ts.End > time);
            if (!overlappingTimeSlots.Any()) continue;
            foreach (var timeSlot in overlappingTimeSlots) {
              // Mark the corresponding day in the schedule row as occupied
              scheduleRow[(int)timeSlot.Day] = course;
            }
          }
          // Add the row entry
          table.AddRow(
            new[] { new Markup(time.ToString("HH:mm")) }
              .Concat(scheduleRow.Select(course => course != null ? UsedCell(course.Name) : new Markup("")))
              .ToArray()
          );
        }

        AnsiConsole.Write(table);
      }
    }
  }
}
