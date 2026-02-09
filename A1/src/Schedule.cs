using System;
using System.Linq;
using Spectre.Console; // A library for pretty console output
using CourseGraph;
using System.Collections.Generic;
using System.IO;

namespace Schedule {
  public record struct TimeTableResult(
    List<(Course course, TimeTableInfo section)?[]> TimeTables,
    (Course course, TimeTableInfo[] courseSections)?[] SlotData
  );

  public class Schedule {
    /// <summary>The time increment used between blocks when rendering schedules.</summary>
    private readonly int TimeIncrement = 60;
    /// <summary>The maximum number of courses we can take in a given term.</summary>
    private readonly int MaxTermSize;
    /// <summary>The term in which we start (We assume terms are sequential based on enum ordering).</summary>
    private readonly Term StartingTerm;
    /// <summary>A list of terms and the courses taken within.</summary>
#nullable enable
    private List<(Course course, TimeTableInfo[] timeTableInfo)?[]> TermData;
    /// <summary>A quick mapping of course names to their scheduled term.</summary>
    private Dictionary<string, int> ScheduledCourses;

    /// <summary>Constructs a new schedule with the given options.</summary>
    public Schedule(int maxTermSize, Term startingTerm = Term.Fall) {
      this.MaxTermSize = maxTermSize;
      this.StartingTerm = startingTerm;
      this.TermData = [];
      this.ScheduledCourses = [];
    }

    // ------------------------- Internal Methods -------------------------

    // ------------------------- Mutation Methods -------------------------

    /// <summary>Adds a given course to the schedule in the desired term.</summary>
    /// <param name="course">The course to add to the schedule.</param>
    /// <param name="term">The term to add the course to</param>
    /// <exception cref="Exception">If the course cannot be placed in the term</exception>
    public void AddCourse(Course course, int term) {
      if (!this.CanPlaceCourse(course, term)) throw new Exception("Invalid Course Placement");
      // Grow the term table to fit
      while (this.TermData.Count <= term) {
        this.TermData.Add(new (Course course, TimeTableInfo[] timeTableInfo)?[this.MaxTermSize]);
      }
      // Generate the new term
      (Course course, TimeTableInfo[] timeTableInfo)?[] newTermData = [.. this.TermData[term]];
      // NOTE: This is guaranteed to place because of our check in `CanPlaceCourse`
      for (int i = 0; i < newTermData.Length; i++) {
        if (newTermData[i] != null) continue;
        var termType = this.GetTermType(term);
        newTermData[i] = (course, course.TimeTableInfos.Where(t => t.OfferedTerm == termType).ToArray());
        break;
      }
      // Narrow timeSlots to valid combinations
      var (permutations, narrowedTermData) = this.GetValidTimeTables(newTermData);
      if (permutations.Count <= 0)
        throw new Exception("Impossible: No valid timeSlot after validation");
      this.TermData[term] = narrowedTermData;
      this.ScheduledCourses.Add(course.Name, term);
    }

    // --------------------------- Info Methods ----------------------------

    /// <summary>
    /// Generates every valid permutation of section choices per slot such that no two chosen sections overlap.
    /// Time Complexity: O(n^m) where n is the number of possible sections and m is the number of slots.
    /// </summary>
    /// <param name="slotData">Per-slot (course, possible sections); null = empty slot.</param>
    /// <returns>TimeTableResult (TimeTables, slotData). Empty grid if no valid permutation.</returns>
    public TimeTableResult GetValidTimeTables((Course course, TimeTableInfo[] courseSections)?[] slotData) {
      // This may seem naive and it is however as we narrow as we select which means 
      // we don't grow to large. This may become an issue with an insanely massive graph 
      // though even with something as large as trent's course system this works in a negligible amount of time.

      // Calculate the total number of permutations to generate
      // For each slot, if its null, it contributes 1 to the product, otherwise it contributes the number of available sections
      long total = slotData.Aggregate(1, (acc, slot) => acc * (slot == null ? 1 : slot.Value.courseSections.Length));

      // Generate all permutations
      // Each permutation is represented as a unique combination index from 0 to total-1
      var allPermutations = new List<(Course course, TimeTableInfo section)?[]>();
      for (long combo = 0; combo < total; combo++) {
        // Convert the combination index into per-slot section choices
        // This works like converting a number to mixed-radix where each position has a different base
        var choice = new int[slotData.Length];
        long n = combo;
        for (int i = 0; i < slotData.Length; i++) {
          var slot = slotData[i];
          var c = slot == null ? 1 : slot.Value.courseSections.Length;  // Number of choices for this slot
          choice[i] = (int)(n % c); // Determine which section index to use
          n /= c; // Move to the next position
        }

        // Build candidate permutation based on choices
        var candidate = new (Course course, TimeTableInfo section)?[slotData.Length];
        for (int i = 0; i < slotData.Length; i++) {
          var slot = slotData[i];
          if (slot == null) continue; // Skip empty slots
          var (course, sections) = slot.Value;
          candidate[i] = (course, sections[choice[i]]);
        }
        // Check for overlaps (skip empty slots)
        bool valid = true;
        for (int i = 0; i < candidate.Length; i++) {
          for (int j = i + 1; j < candidate.Length; j++) {
            var candidateA = candidate[i];
            var candidateB = candidate[j];
            if (candidateA == null || candidateB == null) continue;
            if (TimeTableInfo.DoesOverlap(candidateA.Value.section, candidateB.Value.section)) {
              valid = false;
              break;
            }
          }
          if (!valid) break;
        }
        if (valid) allPermutations.Add(candidate);
      }

      // If we have a lot of combinations we filter out any sections that don't fit into a valid timeslot, 
      // this lets us reduce the problem size when we have large terms (it's expensive hence the heuristic)
      if (total > 1024) {
        // Filter out any sections from the slotData that are not used in a valid permutation
        var narrowedSlotData = new (Course course, TimeTableInfo[] courseSections)?[slotData.Length];
        for (int i = 0; i < slotData.Length; i++) {
          var slot = slotData[i];
          if (slot == null) continue;
          var (course, _) = slot.Value;
          var sectionsInValidPerms = allPermutations
            .Select(p => p[i]) // Lookup the permutation from the slot
            .Where(e => e.HasValue) // Ensure the permutation isn't null
            .Select(e => e!.Value.section) // Get the section data
            .Distinct() // Get only distinct items
            .ToArray(); // Convert back to an array
          narrowedSlotData[i] = (course, sectionsInValidPerms); // Write the data back
        }
        return new TimeTableResult(allPermutations, narrowedSlotData);
      } else {
        return new TimeTableResult(allPermutations, slotData);
      }
    }

    /// <summary>
    /// Determines if the course can be placed in the specified term.
    /// Constraints:
    /// * IsTermFull
    /// * DoesCourseRunInTerm
    /// * IsValidTimeTable
    /// </summary>
    /// <param name="course"></param>
    /// <param name="term"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool CanPlaceCourse(Course course, int term) {
      // Basic Input Validation
      if (course.IsDegree)
        throw new ArgumentException("Cannot add a degree course to a schedule");
      if (term < 0)
        throw new ArgumentException("Cannot add a course outside of the schedule");
      // Determine if the course can be placed in this term
      if (this.IsTermFull(term)) return false; // NOTE: We cannot place it into a full term
      Term termType = this.GetTermType(term);
      if (!course.TimeTableInfos.Any(t => t.OfferedTerm == termType)) return false; // The course doesn't run this term
      // Check There is a valid TimeTable Permutation
      var termData = this.TermData.Count <= term ? new (Course course, TimeTableInfo[] timeTableInfo)?[this.MaxTermSize] : this.TermData[term];
      (Course course, TimeTableInfo[] timeTableInfo)?[] newTermData = [.. termData];
      for (int i = 0; i < newTermData.Length; i++) {
        if (newTermData[i] != null) continue;
        newTermData[i] = (course, course.TimeTableInfos.Where(t => t.OfferedTerm == termType).ToArray());
        break;
      }
      // Narrow timeSlots to valid combinations
      var (permutations, _) = this.GetValidTimeTables(newTermData);
      if (permutations.Count < 1) return false;
      // The course can fit in the given term
      return true;
    }

    /// <summary>
    /// Determines which term type (Fall/Winter) a term index corresponds to based on the starting term.
    /// </summary>
    /// <param name="termIndex">The index of the term to get the type for.</param>
    /// <returns>The term type (Fall/Winter) for the given term index.</returns>
    public Term GetTermType(int termIndex) {
      return (Term)(((int)this.StartingTerm + termIndex) % Enum.GetValues(typeof(Term)).Length);
    }

    /// <summary>Checks if a given term is full or not.</summary>
    /// <param name="term">The term we want to check</param>
    /// <returns>`true` if the term is full, otherwise `false`</returns>
    public bool IsTermFull(int term) {
      if (this.TermData.Count <= term) return false;
      return this.TermData.Any(slot => slot == null);
    }

    /// <summary>Queries which term a course is scheduled for.</summary>
    /// <param name="course">The course you are looking for.</param>
    /// <returns>The term the course is scheduled in, or `-1` if not found.</returns>
    public int GetCourseTerm(Course course) => this.ScheduledCourses.GetValueOrDefault(course.Name, -1);

    /// <summary>Returns the number of scheduled course.</summary>
    public int GetScheduledCreditCount() => this.ScheduledCourses.Count;

    // ------------------------- Display Methods --------------------------

    /// <summary>Generate the schedule as a table.</summary>
    private Table?[] GenerateSchedule() {
      // A helper to generate colored filled cells
      var colors = new[] { Color.Blue, Color.Red, Color.Yellow, Color.Green, Color.Violet };
      Markup UsedCell(string name, int slot) => new Markup(name, new Style(background: colors[slot % colors.Length]));
      // Generate a table per term
      var termTables = new Table?[this.TermData.Count];
      for (int termIndex = 0; termIndex < this.TermData.Count; termIndex++) {
        var termData = this.TermData[termIndex];
        var termNotEmpty = termData.Any(i => i != null);
        // Get TimeTable Information
        var (timeTableInfo, _) = this.GetValidTimeTables(termData);
        // Because we use `getValidTimeTables` to check if we can place in term,
        // we should never have a case where the result is empty if we have things
        // scheduled in the term.
        if (timeTableInfo.Count <= 0 && termNotEmpty) {
          throw new Exception("Impossible: There are no valid timetables for a term");
        }
        // Build Term Table Header
        var table = new Table().Border(TableBorder.Rounded).ShowRowSeparators();
        table.Title($"Term {termIndex} - {this.GetTermType(termIndex)} Schedule");
        var header = new string[] { "Time" }
                        .Concat(Enum.GetNames(typeof(DayOfWeek))) // Get's the names of the week and appends to `Time` list
                        .ToArray();
        table.AddColumns(header); // Should look like: Time | Sunday | ... | Saturday
        // Build Term Table Body
        for ( // Loop from the beginning of the day to the end of the day in time increments
          var time = TimeTableInfo.EarliestTime;
          time <= TimeTableInfo.LatestTime;
          time = time.AddMinutes(this.TimeIncrement)
        ) {
          var row = new Markup?[header.Length];
          row[0] = new Markup(time.ToString("HH:mm")); // Set the first row the time
          if (termNotEmpty) {
            var timeTable = timeTableInfo[0];
            for (int courseEntry = 0; courseEntry < timeTable.Length; courseEntry++) {
              // NOTE: We always choose the first permutation because it doesn't matter 
              //       This could be changed to get a more desirable course layout.
              var entry = timeTable[courseEntry];
              if (!entry.HasValue) continue;
              var (course, sessions) = entry.Value;
              foreach (var courseSection in sessions.TimeSlots) {
                // NOTE: Because we can't partially cover cell we use > for the end
                if (courseSection.Start <= time && courseSection.End > time) {
                  // NOTE: This is 1+ as we have the time take the first row
                  row[1 + (int)courseSection.Day] = UsedCell(course.Name, courseEntry);
                }
              }
            }
          }
          table.AddRow(row.Select(r => r ?? new Markup(""))); // Add the row, fill in blanks with nothing
        }
        // Store the table
        termTables[termIndex] = table;
      }
      return termTables;
    }

    /// <summary>Prints the schedule to the console.</summary>
    public void PrintSchedule() {
      var tables = this.GenerateSchedule();
      foreach (var table in tables) {
        if (table == null) continue;
        AnsiConsole.Write(table);
      }
    }

    /// <summary>Stringifies the schedule.</summary>
    public override string ToString() {
      var tables = this.GenerateSchedule();
      var sw = new StringWriter();
      // The way we render to a string is by getting spectreConsole to write to a stringWriter
      var console = AnsiConsole.Create(new AnsiConsoleSettings {
        // String's don't support console coloring
        Ansi = AnsiSupport.No,
        ColorSystem = ColorSystemSupport.NoColors,
        Interactive = InteractionSupport.No,
        // Forward console data to StringWriter
        Out = new AnsiConsoleOutput(sw)
      });
      foreach (var table in tables) {
        if (table == null) continue;
        console.Write(table);
      }
      return sw.ToString();
    }

    /// <summary>Writes the schedule to a file.</summary>
    /// <param name="fileName">The file to write to</param>
    public void WriteScheduleToFile(string fileName) {
      File.WriteAllText(fileName, this.ToString());
    }
  }
}
