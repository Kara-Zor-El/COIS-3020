using System;
using System.Linq;
using Spectre.Console; // A library for pretty console output
using CourseGraph;
using System.Collections.Generic;
using System.IO;

namespace Schedule {
  public record struct TimeTableResult(
    List<(Course course, TimeTableInfo section)[]> TimeTables,
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

    /// <summary>Computes a stable hash for slot data for memoization.</summary>
    private static int HashSlotData((Course course, TimeTableInfo[] courseSections)?[] slotData) {
      var h = new HashCode();
      h.Add(slotData.Length);
      foreach (var slot in slotData) {
        if (slot is null) { h.Add(0); continue; }
        var (course, sections) = slot.Value;
        h.Add(course.Name);
        h.Add(sections.Length);
        foreach (var s in sections) h.Add(s);
      }
      return h.ToHashCode();
    }

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
      }
      // Narrow timeSlots to valid combinations
      var (_, narrowedTermData) = this.GetValidTimeTables(newTermData);
      this.TermData[term] = narrowedTermData;
      this.ScheduledCourses.Add(course.Name, term);
    }

    // --------------------------- Info Methods ----------------------------

    /// <summary>Cache for TimeTableResult: (hash of slot data, full output)</summary>
    private (int hash, TimeTableResult output) cache = (-1, default(TimeTableResult));

    /// <summary>
    /// Generates every valid permutation of section choices per slot such that no two chosen sections overlap.
    /// Time Complexity: O(n^m) where n is the number of possible sections and m is the number of slots.
    /// </summary>
    /// <param name="slotData">Per-slot (course, possible sections); null = empty slot.</param>
    /// <returns>TimeTableResult (TimeTables, slotData). Empty grid if no valid permutation.</returns>
    public TimeTableResult GetValidTimeTables((Course course, TimeTableInfo[] courseSections)?[] slotData) {
      // If input is empty we can return real quick
      if (slotData == null || slotData.Length == 0) return new TimeTableResult([], slotData ?? []);
      // Memorization
      // NOTE: This is a simple memorization because the call pattern is to call a 
      // bunch of times for a single set of courses rather than random access
      int hash = HashSlotData(slotData);
      if (this.cache.hash == hash) return this.cache.output;
      // Build list of non-null slots (course, sections)
      var slots = new List<(Course course, TimeTableInfo[] sections)>();
      foreach (var slot in slotData.Where(s => s != null)) {
        slots.Add(slot.Value);
      }

      var allPermutations = new List<(Course course, TimeTableInfo section)[]>();
      if (slots.Count == 0) {
        var result = new TimeTableResult(allPermutations, slotData);
        this.cache = (hash, result);
        return result;
      }

      // Section count per slot
      int[] counts = slots.Select(s => s.sections.Length).ToArray();
      long total = 1;
      foreach (int c in counts) total *= c;

      for (long combo = 0; combo < total; combo++) {
        var choice = new int[slots.Count];
        long n = combo;
        for (int i = 0; i < slots.Count; i++) {
          choice[i] = (int)(n % counts[i]);
          n /= counts[i];
        }

        // Build candidate
        var candidate = new (Course course, TimeTableInfo section)[slots.Count];
        for (int i = 0; i < slots.Count; i++) {
          var (course, sections) = slots[i];
          candidate[i] = (course, sections[choice[i]]);
        }

        // Check for overlaps
        bool valid = true;
        foreach (var (_, section) in candidate) {
          foreach (var (_, otherSection) in candidate) {
            if (TimeTableInfo.DoesOverlap(section, otherSection)) {
              valid = false;
              break;
            }
          }
        }

        if (valid)
          allPermutations.Add(candidate);
      }

      var output = new TimeTableResult(allPermutations, slotData);
      this.cache = (hash, output);
      return output;
    }

    /// <summary>
    /// Determines if the course can be placed in the specified term.
    /// Constraints:
    /// * IsTermFull
    /// * DoesCourseRunInTerm
    /// * TODO: IsValidTimeTable
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
      // NOTE: This is guaranteed to place because of our check in `CanPlaceCourse`
      // TODO: Validate this line
      var termData = this.TermData.Count <= term ? [] : this.TermData[term];
      (Course course, TimeTableInfo[] timeTableInfo)?[] newTermData = [.. termData];
      for (int i = 0; i < newTermData.Length; i++) {
        if (newTermData[i] != null) continue;
        newTermData[i] = (course, course.TimeTableInfos.Where(t => t.OfferedTerm == termType).ToArray());
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
      // TODO: Rewrite this to a simple linq statement
      if (this.TermData.Count <= term) return false;
      foreach (var slot in this.TermData[term]) if (slot == null) return false;
      return true;
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
        // Get TimeTable Information
        var (timeTableInfo, _) = this.GetValidTimeTables(termData);
        // Because we use `getValidTimeTables` to check if we can place in term,
        // we should never have a case where the result is empty if we have things
        // scheduled in the term.
        if (timeTableInfo.Count <= 0 && termData.Any(i => i != null)) {
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
          var timeTable = timeTableInfo[0];
          for (int i = 0; i < timeTable.Length; i++) {
            // NOTE: We always choose the first permutation because it doesn't matter 
            //       This could be changed to get a more desirable course layout.
            var (course, sessions) = timeTable[i];
            foreach (var courseSection in sessions.TimeSlots) {
              // NOTE: Because we can't partially cover cell we use > for the end
              if (courseSection.Start <= time && courseSection.End > time) {
                // NOTE: This is 1+ as we have the time take the first row
                row[1 + (int)courseSection.Day] = UsedCell(course.Name, i);
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
