using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Cli;
using CourseGraph;

namespace Program {
  public class ProgramSettings : CommandSettings {
    // General User Settings
    [CommandArgument(0, "<course_data>")]
    [Description("The json data bundle to load.")]
    public required string CourseData { get; init; }

    [CommandOption("-o|--output", isRequired: true)]
    [Description("The output markdown file you would like your schedule in.")]
    public required string ScheduleOutput { get; init; }

    [CommandOption("--log", isRequired: false)]
    [Description("Weather or not to log the schedule to the console.")]
    [DefaultValue(false)]
    public bool ScheduleLog { get; init; }

    [CommandOption("--degree", isRequired: true)]
    [Description("The name of the degree you are in.")]
    public required string Degree { get; init; }

    [CommandOption("--credit_count", isRequired: true)]
    [Description("The number of credits required in your schedule.")]
    public required int CreditCount { get; init; }

    [CommandOption("--term_size", isRequired: true)]
    [Description("The number of ")]
    public required int TermSize { get; init; }
    [CommandOption("--graph_output", isRequired: false)]
    [Description("The file to write our graph to.")]
    [DefaultValue(null)]
#nullable enable
    public required string? GraphOutput { get; init; }
    // Advanced Settings
    [CommandOption("--debug", isRequired: false)]
    [Description("Weather to enable extra debug information.")]
    [DefaultValue(false)]
    public required bool DebugMode { get; init; }
  }

  public class ProgramCommand : Command<ProgramSettings> {
    public static void EmitError(string message) {
      AnsiConsole.MarkupLine($"[red bold]Error: {message}.[/]");
    }
    public static void EmitWarning(string message) {
      AnsiConsole.MarkupLine($"[MediumOrchid]Warning: {message}.[/]");
    }
    public override int Execute(CommandContext context, ProgramSettings settings, CancellationToken cancellation) {
      if (!File.Exists(settings.CourseData)) {
        EmitError($"Course data file `{Markup.Escape(settings.Degree)}` does not exist");
        return 1;
      }
      ;
      var jsonString = File.ReadAllText(settings.CourseData);
      // NOTE: These can throw exceptions
      var loadedCourseData = JsonSerializer.Deserialize<CourseData>(jsonString);
      if (loadedCourseData == null) {
        EmitError($"Invalid course data");
        return 1;
      }
      var courseGraph = CourseGraph.CourseGraph.FromCourseData(loadedCourseData);

      var desiredDegree = loadedCourseData.GetDegreeByName(settings.Degree);
      if (desiredDegree == null) {
        EmitError($"Degree `{Markup.Escape(settings.Degree)}` does not exist");
        return -1;
      }
      var creditCount = Math.Min(settings.CreditCount, loadedCourseData.Courses.Count);
      if (creditCount < settings.CreditCount) {
        EmitWarning($"Desired credit count is impossible as only `{creditCount}` courses exist in the graph");
      }
      // Run the scheduler
      var stopWatch = Stopwatch.StartNew(); // Track Performance
      var schedule = courseGraph.Schedule(
        termSize: settings.TermSize,
        creditCount: creditCount,
        degreeCourse: desiredDegree
      );
      stopWatch.Stop(); // End Performance Timing
      if (settings.ScheduleLog) {
        schedule.PrintSchedule();
      }
      // Debug Information
      if (settings.DebugMode) {
        AnsiConsole.MarkupLine($"Elapsed Time: {stopWatch.Elapsed.TotalMilliseconds} ms");
      }
      schedule.WriteScheduleToFile(settings.ScheduleOutput);
      AnsiConsole.MarkupLine($"[green] Wrote schedule information to `{Markup.Escape(settings.ScheduleOutput)}`.[/]");
      // Write outputs
      if (settings.GraphOutput != null) {
        courseGraph.WriteToFile(settings.GraphOutput);
        AnsiConsole.MarkupLine($"[green] Wrote graph information to `{Markup.Escape(settings.GraphOutput)}`.[/]");
      }
      AnsiConsole.MarkupLine($"[green] Successfully finished scheduling.[/]");
      return 0;
    }
  }
  class Program {
    static int Main(string[] args) {
      // Use SpectreConsole.CLI for our command line options
      var app = new CommandApp<ProgramCommand>();
      return app.Run(args);
    }
  }
}
