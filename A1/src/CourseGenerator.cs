using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CourseGraph;

public static class CourseGenerator {
    private class RawCoursesRoot {
        [JsonPropertyName("Sections")]
        public List<RawSection> Sections { get; set; }
    }

    private class RawSection {
        [JsonPropertyName("TermCode")]
        public string TermCode { get; set; }
        [JsonPropertyName("Subject")]
        public string Subject { get; set; }
        [JsonPropertyName("CourseName")]
        public string CourseName { get; set; }
        [JsonPropertyName("Title")]
        public string Title { get; set; }
        [JsonPropertyName("Meetings")]
        public List<RawMeeting> Meetings { get; set; }
    }

    private class RawMeeting {
        [JsonPropertyName("Days")]
        public int[] Days { get; set; }
        [JsonPropertyName("StartTime")]
        public string StartTime { get; set; }
        [JsonPropertyName("EndTime")]
        public string EndTime { get; set; }
    }

    private static readonly Dictionary<string, string> DegreeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        { "COIS", "Bachelor of Computer Science" },
        { "ADMN", "Bachelor of Business Admin" },
        { "PSYC", "Bachelor of Science (Psych)" },
        { "BIOL", "Bachelor of Science (Biology)" },
        { "CHEM", "Bachelor of Science (Chem)" },
        { "PHYS", "Bachelor of Science (Physics)" },
        { "MATH", "Bachelor of Science (Math)" },
        { "HIST", "Bachelor of Arts (History)" },
        { "ENGL", "Bachelor of Arts (English)" },
        { "SOCI", "Bachelor of Arts (Sociology)" },
        { "ANTH", "Bachelor of Arts (Anthro)" },
        { "PHIL", "Bachelor of Arts (Phil)" },
        { "ECON", "Bachelor of Economics" }
    };

    public static object GenerateFullCatalogData(string jsonContent) {
        var allCourses = new List<Course>();
        var degreeCourses = new List<Course>();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<RawCoursesRoot>(jsonContent, options);
        if (root?.Sections == null) throw new Exception("Input JSON has no Sections.");

        // Group sections by CourseName (each course name = one Course; each section = one TimeTableInfo)
        var sectionsByCourse = root.Sections
            .Where(s => !string.IsNullOrWhiteSpace(s.CourseName))
            .GroupBy(s => s.CourseName.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        // First pass: determine which course names we will actually add (have at least one valid section)
        var courseNamesWeAdd = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in sectionsByCourse) {
            var sections = kv.Value;
            foreach (var section in sections) {
                Term term = ParseTerm(section.TermCode);
                var slotCount = 0;
                if (section.Meetings != null) {
                    foreach (var meeting in section.Meetings) {
                        if (meeting.Days == null || meeting.Days.Length == 0) continue;
                        if (!TimeOnly.TryParse(meeting.StartTime, out var start) || !TimeOnly.TryParse(meeting.EndTime, out var end))
                            continue;
                        if (start >= end) continue;
                        if (start < TimeTableInfo.EarliestTime || end > TimeTableInfo.LatestTime) continue;
                        foreach (var dayNum in meeting.Days) {
                            if (dayNum >= 0 && dayNum <= 7) slotCount++;
                        }
                    }
                }
                if (slotCount > 0) { courseNamesWeAdd.Add(kv.Key); break; }
            }
        }

        var courseList = courseNamesWeAdd.ToList();
        courseList.Sort(StringComparer.OrdinalIgnoreCase);
        var deptGroups = courseList.GroupBy(c => c.Split('-')[0]).ToDictionary(g => g.Key, g => g.ToList());
        var rand = new Random(55);

        // Build one Course per CourseName with TimeTableInfos from each section
        foreach (var kv in sectionsByCourse) {
            string courseName = kv.Key;
            var sections = kv.Value;
            var timeTableInfos = new List<TimeTableInfo>();

            foreach (var section in sections) {
                Term term = ParseTerm(section.TermCode);
                var slots = new List<TimeSlot>();
                if (section.Meetings != null) {
                    foreach (var meeting in section.Meetings) {
                        if (meeting.Days == null || meeting.Days.Length == 0) continue;
                        if (!TimeOnly.TryParse(meeting.StartTime, out var start) || !TimeOnly.TryParse(meeting.EndTime, out var end))
                            continue;
                        if (start >= end) continue;
                        if (start < TimeTableInfo.EarliestTime || end > TimeTableInfo.LatestTime) continue;
                        var times = new List<TimeOccurrence>();
                        foreach (var dayNum in meeting.Days) {
                            if (dayNum < 0 || dayNum > 7) continue;
                            DayOfWeek day = dayNum == 7 ? DayOfWeek.Sunday : (DayOfWeek)dayNum;
                            times.Add(new TimeOccurrence(day, start, end));
                        }
                        if (times.Count > 0)
                            slots.Add(new TimeSlot(times.ToArray()));
                    }
                }
                if (slots.Count == 0) continue;
                timeTableInfos.Add(new TimeTableInfo { OfferedTerm = term, TimeSlots = slots.ToArray() });
            }

            if (timeTableInfos.Count == 0) continue;

            string dept = courseName.Split('-')[0];
            int level = GetLevel(courseName);
            var preReqs = new List<string>();
            var coReqs = new List<string>();

            if (deptGroups.TryGetValue(dept, out var peers)) {
                var lower = peers.Where(c => GetLevel(c) < level).ToList();
                var sameLevel = peers.Where(c => GetLevel(c) == level && c != courseName).ToList();
                foreach (var c in lower.OrderBy(_ => rand.Next()).Take(rand.Next(0, 4)))
                    if (c != courseName && !preReqs.Contains(c, StringComparer.OrdinalIgnoreCase)) preReqs.Add(c);
                foreach (var c in sameLevel.OrderBy(_ => rand.Next()).Take(rand.Next(0, 3)))
                    if (rand.NextDouble() > 0.5 && !preReqs.Contains(c, StringComparer.OrdinalIgnoreCase)) preReqs.Add(c);
                foreach (var c in sameLevel.OrderBy(_ => rand.Next()).Take(rand.Next(0, 2)))
                    if (!coReqs.Contains(c, StringComparer.OrdinalIgnoreCase) && !preReqs.Contains(c, StringComparer.OrdinalIgnoreCase)) coReqs.Add(c);
            }

            allCourses.Add(new Course(
                name: courseName,
                coRequisites: coReqs,
                preRequisites: preReqs,
                timeTableInfos: timeTableInfos.ToArray(),
                isPhantom: false
            ));
        }

        // Degrees: one phantom course per Subject (degree)
        var subjects = sectionsByCourse.Values.SelectMany(s => s.Select(x => x.Subject?.Trim())).Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var subject in subjects) {
            string degreeName = DegreeMap.TryGetValue(subject, out var mapped) ? mapped : $"Bachelor of {subject}";
            degreeCourses.Add(new Course(
                name: degreeName,
                coRequisites: new List<string>(),
                preRequisites: new List<string>(),
                timeTableInfos: Array.Empty<TimeTableInfo>(),
                isPhantom: true
            ));
            allCourses.Add(degreeCourses[degreeCourses.Count - 1]);
        }

        return new {
            Courses = allCourses,
            Degrees = degreeCourses
        };
    }

    private static Term ParseTerm(string termCode) {
        if (string.IsNullOrEmpty(termCode)) return Term.Fall;
        return termCode.EndsWith("WI", StringComparison.OrdinalIgnoreCase) ? Term.Winter : Term.Fall;
    }

    private static int GetLevel(string courseName) {
        var parts = courseName.Split('-');
        if (parts.Length < 2 || parts[1].Length == 0) return 1;
        return int.TryParse(parts[1].Substring(0, 1), out int l) ? l : 1;
    }
}
