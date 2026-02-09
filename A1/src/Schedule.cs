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
    /// <summary>The number of courses currently taken in our schedule.</summary>
    public int CourseCount { get; private set; }

    /// <summary>Constructs a new schedule with the given options.</summary>
    public Schedule(int maxTermSize, Term startingTerm = Term.Fall) {
      this.MaxTermSize = maxTermSize;
      this.StartingTerm = startingTerm;
      this.TermData = [];
      this.ScheduledCourses = [];
      this.CourseCount = 0;
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
    /// <exception cref="ArgumentException">If the course overlaps with another course in the given term.</exception>
    public void AddCourse(Course course, int term) {
      if (!this.CanPlaceCourse(course, term)) throw new Exception("Invalid Course Placement");
      // TODO: Create a new array representing the proposed `term`
      // TODO: Get the valid timeSlots
      // TODO: Place the timeSlot
      // Initial TimeSlot Validation (check if the course is offered in the term)
      Term termType = this.GetTermType(term);
      var possibleTimeSlots = course.TimeTableInfos.Where(t => t.OfferedTerm == termType);
      var nonOverlappingTimeSlots = this.GetCourseValidTimeSlots(course, possibleTimeSlots.ToArray(), term);
      if (nonOverlappingTimeSlots.Count <= 0)
        throw new ArgumentException($"Course {course.Name}, can't be scheduled do to overlaps in term {term}");
      // Grow the term table to fit
      while (this.TermData.Count <= term) {
        this.TermData.Add(new (Course course, TimeTableInfo[] timeTableInfo)?[this.MaxTermSize]);
      }
      // Try to add the course to the first available slot in the term
      for (int slotIndex = 0; slotIndex < this.TermData[term].Length; slotIndex++) {
        var currentSlot = this.TermData[term][slotIndex];
        if (currentSlot != null) continue;
        this.TermData[term][slotIndex] = (course, nonOverlappingTimeSlots.ToArray());
        this.ScheduledCourses.Add(course.Name, term);
        this.CourseCount++;
        return;
      }
      throw new Exception("Impossible: Course had to have been placed");
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
      // TODO: Do timeTableCheck
      return true;
    }

    /// <summary>
    /// Checks if the given time slot is already taken.
    /// </summary>
    /// <param name="timeTableInfo">The time slot to check against</param>
    /// <param name="term">which term we are checking in</param>
    // TODO: Remove this function
    public List<TimeTableInfo> GetCourseValidTimeSlots(Course course, TimeTableInfo[] timeTableInfo, int term) {
      var possibleSections = new List<TimeTableInfo>(timeTableInfo); // Clone the array
      if (this.TermData.Count <= term) return possibleSections;
      // Check if timeTableInfo has at least one item where the time doesn't overlap with anything else in this.TermData[term]
      for (int slotIndex = 0; slotIndex < this.MaxTermSize && possibleSections.Count > 0; slotIndex++) {
        var slotData = this.TermData[term][slotIndex];
        if (slotData == null) break;
        var (checkCourse, checkSections) = slotData.Value;
        if (checkCourse == course) continue;
        possibleSections.RemoveAll(t => checkSections.All(c => TimeTableInfo.DoesOverlap(c, t)));
      }
      return possibleSections;
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
        // TODO: Enable this once `GetValidTimeTables` is implemented.
        // if (timeTableInfo.Length < 0 && termData.Any(i => i != null)) {
        //   throw new Exception("Impossible: There are no valid timetables for a term");
        // }
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
          for (int i = 0; i < timeTableInfo.Length; i++) {
            // NOTE: We always choose the first permutation because it doesn't matter 
            //       This could be changed to get a more desirable course layout.
            var (course, sessions) = timeTableInfo[0, i];
            foreach (var courseSection in sessions.TimeSlots) {
              // NOTE: Because we can't partially cover cell we use > for the end
              if (courseSection.Start <= time && courseSection.End > time) {
                row[(int)courseSection.Day] = UsedCell(course.Name, i);
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
      // Render to a string
      var sw = new StringWriter();
      var console = AnsiConsole.Create(new AnsiConsoleSettings {
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
