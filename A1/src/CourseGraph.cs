using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CourseGraph {
  /// <summary>Indicates the relationship between two courses.</summary>
  public enum CourseRelation {
    /// <summary>Indicates a course must be taken prior to the specified course.</summary>
    Prereq,
    /// <summary>Indicates a course must be taken prior or at the same time as the specified course.</summary>
    Coreq
  }
  /// <summary>Represents a link between two courses.</summary>
  public class CourseEdge {
    /// <summary>The adjacent vertex this edge points to.</summary>
    public CourseVertex AdjVertex { get; set; }
    /// <summary>The relation between the vertex and the adjacent vertex. </summary>
    public CourseRelation Relation { get; set; }
    /// <summary>Creates a new edge leading to `vertex`, with the specified relationship.</summary>
    /// <param name="vertex">The vertex this edge leads to</param>
    /// <param name="relation">The relationship with the vertex</param>
    public CourseEdge(CourseVertex vertex, CourseRelation relation) {
      this.AdjVertex = vertex;
      this.Relation = relation;
    }
  }
  /// <summary>Represents a course node.</summary>
  public class CourseVertex {
    /// <summary>The actual course this node represents.</summary>
    public Course Value { get; init; }
    /// <summary>Weather this vertex has been visited during a traversal.</summary>
    public bool Visited { get; set; }
    /// <summary>The outgoing edges from this vertex.</summary>
    public List<CourseEdge> Edges { get; set; }
    /// <summary>A priority that can be used when scheduling.</summary>
    public double Cost { get; set; }

    /// <summary>Creates a new vertex from the specified course.</summary>
    /// <param name="value">The course this vertex will represent</param>
    public CourseVertex(Course value) {
      this.Value = value;
      this.Visited = false;
      this.Edges = [];
      this.Cost = 0;
    }


    /// <summary>
    /// Finds the outgoing edge leading to a given course.
    /// Time complexity: O(e)
    /// </summary>
    /// <param name="course">The course the edge is leading to.</param>
    /// <returns>The edge to the given to course, otherwise null</returns>
#nullable enable
    public CourseEdge? FindEdge(Course course) {
      foreach (var edge in this.Edges) {
        if (edge.AdjVertex.Value.Equals(course))
          return edge;
      }
      return null;
    }
  }

  public interface IDirectedGraph<T, U> {
    void AddVertex(T name);
    void RemoveVertex(T name);
    void AddEdge(T name1, T name2, U cost);
    void RemoveEdge(T name1, T name2);
  }

  public class CourseGraph : IDirectedGraph<Course, CourseRelation> {
    /// <summary>
    /// The cost of a corequisite when doing the cost heuristic is set to this
    /// as we still want the scheduler to schedule courses who have corequisites
    /// before courses with the same number of pre-requisites but no corequisites.
    /// </summary>
    private static readonly double CoreqWeight = 0.05;
    /// <summary>
    /// The weight of a pre-requisite when doing the cost heuristic.
    /// </summary>
    private static readonly double PrereqWeight = 1.0;

    private List<CourseVertex> Vertices { get; set; }
    public CourseGraph() {
      this.Vertices = [];
    }
    /// <summary>
    /// Finds the vertex representing the given course.
    /// Worst case time complexity: O(v)
    /// </summary>
    /// <param name="course">The course we want to find</param>
    /// <returns>The index of the given vertex (if found); otherwise returns -1</returns>
    private CourseVertex? FindVertex(Course course) {
      foreach (var vert in this.Vertices) {
        if (vert.Value.Equals(course)) return vert;
      }
      return null;
    }

    /// <summary>
    /// Adds the given vertex to the graph
    /// Note: Duplicate vertices are not added
    /// Time complexity: O(v) due to FindVertex
    /// </summary>
    /// <param name="course">The course we want to add</param>
    public void AddVertex(Course course) {
      if (this.FindVertex(course) == null) {
        var courseVertex = new CourseVertex(course);
        this.Vertices.Add(courseVertex);
      }
    }

    /// <summary>
    /// Removes the given vertex and all incident edges from the graph
    /// Note: Nothing is done if the vertex does not exist
    /// Worst case time complexity: O(v + e)
    /// </summary>
    /// <param name="course">The courser we want to remove</param>
    public void RemoveVertex(Course course) {
      // If a course B is removed then its pre- and co-requisite courses
      // become the pre- and co-requisite courses for those course for which
      // B was a pre- and co-requisite.
      var courseVertex = this.FindVertex(course);
      if (courseVertex == null) return;
      foreach (var vert in this.Vertices) {
        var incomingEdge = vert.FindEdge(course);
        if (incomingEdge == null) continue;
        vert.Edges.Remove(incomingEdge);
        // Patch the relations
        foreach (var edge in courseVertex.Edges) {
          this.AddEdge(vert.Value, edge.AdjVertex.Value, edge.Relation);
        }
      }
      this.Vertices.Remove(courseVertex);
    }

    /// <summary>
    /// Determines weather there if there a cycle between vertices
    /// Time complexity: O(v + e)
    /// </summary>
    private bool IsCyclic(CourseVertex from, CourseVertex to) {
      // TODO: Document this function
      foreach (var vert in this.Vertices) vert.Visited = false;
      var stack = new Stack<CourseVertex>([from]);
      while (stack.Count > 0) {
        var current = stack.Pop();
        if (current.Equals(to)) return true;
        if (current.Visited) continue;
        current.Visited = true;
        foreach (var edge in current.Edges) {
          var adj = this.FindVertex(edge.AdjVertex.Value);
          if (adj != null) stack.Push(adj);
        }
      }
      return false;
    }

    /// <summary>
    /// Creates an edge from course1 to course2 with the specified relationship.
    /// Notes: Duplicate edges are not added
    ///        We don't add an edge if a cycle so it does not become a problem
    /// Worst case time complexity: O(n+m)
    /// </summary>
    /// <exception cref="ArgumentException">If the added edge creates a cycle</exception>
    /// <exception cref="ArgumentException">If the outgoing node is a degreeCourse</exception>
    public void AddEdge(Course course1, Course course2, CourseRelation relation) {
      if (course2.IsDegree) throw new ArgumentException("Degree courses must be root nodes");
      var course1Vertex = this.FindVertex(course1);
      var course2Vertex = this.FindVertex(course2);
      if (course1Vertex == null || course2Vertex == null) return; // Vertex does not exist 
      if (course1Vertex.FindEdge(course2) != null) return; // Duplicate Edge
      if (this.IsCyclic(course2Vertex, course1Vertex)) {
        throw new ArgumentException("CourseGraph cannot contain cycles");
      }
      course1Vertex.Edges.Add(new CourseEdge(course2Vertex, relation));
    }

    /// <summary>
    /// Removes the edge from course1 to course2.
    /// Note: If the edge does not exist we just return.
    /// </summary>
    public void RemoveEdge(Course course1, Course course2) {
      var course1Vertex = this.FindVertex(course1);
      var course2Vertex = this.FindVertex(course1);
      if (course1Vertex == null || course2Vertex == null) return; // Vertex does not exist 
      var edge = course1Vertex.FindEdge(course2);
      if (edge == null) return; // Edge does not exist
      course1Vertex.Edges.Remove(edge);
    }

    /// <summary>
    /// Toggle's weather a given course is required by a given degree.
    /// </summary>
    /// <param name="course">The course you want to update</param>
    /// <param name="degree">The degree to toggle requirement</param>
    /// <exception cref="ArgumentException">Degree was not a degree course.</exception>
    /// <exception cref="ArgumentException">Course was a degree course.</exception>
    public void UpdateVertex(Course course, Course degree) {
      // NOTE: Instead of storing required, a node is required if it is linked by a major
      if (!degree.IsDegree) throw new ArgumentException("Expected a degreeCourse for degree");
      if (course.IsDegree) throw new ArgumentException("Expected a course");
      var degreeVertex = this.FindVertex(degree);
      if (degreeVertex == null) return;
      var edge = degreeVertex.FindEdge(course);
      if (edge == null) { // Toggle it to required
        this.AddEdge(degree, course, CourseRelation.Prereq);
        if (!degree.PreRequisites.Contains(course.Name))
          degree.PreRequisites.Add(course.Name);
      } else { // Toggle it to un-required
        degreeVertex.Value.PreRequisites.Remove(course.Name);
        degreeVertex.Edges.Remove(edge);
      }
    }

    /// <summary>
    /// Computes TermMin and TermMax for each course based on the degree requirements.
    /// </summary>
    /// <param name="termSize">Number of courses per term</param>
    /// <param name="creditCount">Total number of credits required</param>
    /// <param name="degreeCourse">The course representing the degree</param>
    public Schedule.Schedule Schedule(int termSize, int creditCount, Course degreeCourse) {
      if (creditCount > this.Vertices.Count)
        throw new ArgumentException("Impossible to fill credit count, there aren't enough courses");
      var degreeVertex = this.FindVertex(degreeCourse);
      if (degreeVertex == null) throw new ArgumentException($"Degree: {degreeCourse.Name} course must be in the graph");
      // -------------- Find Roots --------------
      // Find all root vertices (vertices with no incoming edges)
      var roots = new List<CourseVertex>(this.Vertices);
      foreach (var vert in this.Vertices) {
        vert.Cost = 0;
        vert.Visited = false;
        foreach (var edge in vert.Edges) {
          // If the root has an incoming edge, it isn't a root
          roots.RemoveAll(r => r.Value.Equals(edge.AdjVertex.Value));
        }
      }
      roots.Remove(degreeVertex); // Don't put the degree in roots (handle it separately)
      // -------------- Compute Costs --------------
      this.ComputeCostHeuristic(degreeVertex);
      // -------------- Greedy Schedule --------------
      var schedule = new Schedule.Schedule(maxTermSize: termSize);
      // We begin by placing required course chains with the longest chain from the major.
      var requiredRootStack = new Stack<CourseVertex>(degreeVertex.Edges.Select(e => e.AdjVertex).OrderBy(c => c.Cost));
      this.PlaceCourseChains(schedule, requiredRootStack, creditCount);
      // -------------- Compute Costs --------------
      // TODO: Recompute costs from all other roots (Ensure that cost is valid and we are not overwriting the multiple roots)
      // -------------- Greedy Schedule --------------
      // After we place the required courses we fill the creditCount with filler courses
      // We still want to place using the highest cost so we don't run out of courses do to bad scheduling of pre-requirements.
      var fillerStack = new Stack<CourseVertex>(this.Vertices.Where(v => !v.Visited && !v.Value.IsDegree).OrderBy(v => v.Cost));
      this.PlaceCourseChains(schedule, fillerStack, creditCount, catchErrors: true);
      // Sanity check our scheduler actually hit the required count.
      if (schedule.CourseCount < creditCount)
        throw new Exception($"Impossible: Failed to hit credit count, CourseCount {schedule.CourseCount}, creditCount: {creditCount}");
      return schedule;
    }

    private void PlaceCourseChains(Schedule.Schedule schedule, Stack<CourseVertex> vertexStack, int creditCount, bool catchErrors = false) {
      while (vertexStack.Count > 0 && schedule.CourseCount < creditCount) {
        var vertex = vertexStack.Pop();
        if (vertex.Visited) continue; // We've already placed this course
        // We place these chains from the start to the end using a topological sort
        if (catchErrors) {
          // TODO: Remove the catchErrors idea, courses should be placeable no matter what
          try {
            foreach (var course in this.TopologicalSort(vertex)) {
              this.PlaceInScheduleData(schedule, course);
            }
          }
          catch (Exception) {
            continue;
          }
        } else {
          foreach (var course in this.TopologicalSort(vertex)) {
            this.PlaceInScheduleData(schedule, course);
          }
        }
        // This is impossible but we do the check anyway to ensure that we placed every node
        if (!vertex.Visited) // Visited means placed in this context
          throw new Exception("Impossible: Failed to place required course");
      }
    }

    private void PlaceInScheduleData(Schedule.Schedule schedule, CourseVertex courseVertex) {
      // The course was already placed
      if (courseVertex.Visited) return;
      // Determine earliest course placement based off terms of all pre-req and co-req
      var minCoreq = courseVertex.Edges
                      .Where(e => e.Relation == CourseRelation.Coreq)
                      .Select(e => schedule.GetCourseTerm(e.AdjVertex.Value))
                      .DefaultIfEmpty(0)
                      .Max();
      var minPrereq = courseVertex.Edges
                      .Where(e => e.Relation == CourseRelation.Prereq)
                      .Select(e => schedule.GetCourseTerm(e.AdjVertex.Value))
                      .DefaultIfEmpty(-1)
                      .Max() + 1;
      var courseMinimumTerm = Math.Max(minCoreq, minPrereq);

      // Place the actual course
      Course course = courseVertex.Value;
      int i = courseMinimumTerm - 1;
      while (true) {
        i++;
        if (schedule.IsTermFull(i)) continue; // Can't place in a full term.
        var termType = schedule.GetTermType(i);
        var possibleTimeSlots = course.TimeTableInfos.Where(slot => slot.OfferedTerm == termType);
        if (!possibleTimeSlots.Any()) continue; // No timeSlots exist this semester
        if (schedule.GetCourseValidTimeSlots(course, possibleTimeSlots.ToArray(), i).Count <= 0) {
          continue; // No available timeslot
        }
        // Finally add the course
        schedule.AddCourse(course: course, term: i);
        courseVertex.Visited = true;
        break;
      }
    }

    /// <summary>
    /// Provides nodes back in topological order (a node is always after its prereqs and coreqs).
    /// 
    /// Note: This is an enumerator which means that it can be used like an iterator
    /// Note: This cannot handle cyclic graphs (but it should never have to, do to addEdge)
    /// 
    /// https://www.geeksforgeeks.org/dsa/topological-sorting-indegree-based-solution/
    /// </summary>
    private IEnumerable<CourseVertex> TopologicalSort(CourseVertex root) {
      // An enumerator means that we can use this like an iterator despite it being a function.
      var visited = new HashSet<CourseVertex>();
      foreach (var v in this.Visit(root, visited))
        yield return v;
    }
    private IEnumerable<CourseVertex> Visit(CourseVertex vertex, HashSet<CourseVertex> visited) {
      if (visited.Contains(vertex)) yield break;
      foreach (var edge in vertex.Edges) {
        foreach (var v in this.Visit(edge.AdjVertex, visited))
          yield return v;
      }
      visited.Add(vertex);
      yield return vertex; // post-order yield
    }

    /// <summary>
    /// Computes the cost, which is considered to be how high up a chain we are.
    /// </summary>
    /// <param name="vertex">The vertex we are computing from (root).</param>
    private void ComputeCostHeuristic(CourseVertex vertex) {
      // TODO: Why are we only computing the cost from a degreeVertex?
      // TODO: This needs to be able to compute from any major
      foreach (var vert in this.Vertices) vert.Cost = 0;
      foreach (var vert in this.TopologicalSort(vertex)) {
        foreach (var edge in vert.Edges) {
          var dependencyVert = edge.AdjVertex;
          double weight = edge.Relation == CourseRelation.Prereq ? PrereqWeight : CoreqWeight;
          double newCost = dependencyVert.Cost + weight;
          vert.Cost = Math.Max(vert.Cost, newCost);
        }
      }
    }

    /// <summary>Extract the courses from the graph.</summary>
    /// <returns>A CourseData bundle containing the degrees and courses.</returns>
    public CourseData GetCourseData() {
      var degrees = new List<Course>();
      var courses = new List<Course>();
      foreach (var vertex in this.Vertices) {
        if (vertex.Value.IsDegree) degrees.Add(vertex.Value);
        else courses.Add(vertex.Value);
      }
      return new CourseData {
        Degrees = degrees,
        Courses = courses
      };
    }

    /// <summary>Constructs a CourseGraph from the given CourseData Bundle.</summary>
    /// <param name="data">The course data to generate a graph from.</param>
    /// <returns>A new courseGraph</returns>
    /// <exception cref="ArgumentException">If the data entry is invalid</exception>
    public static CourseGraph FromCourseData(CourseData data) {
      var courseGraph = new CourseGraph();
      foreach (var degree in data.Degrees) courseGraph.AddVertex(degree);
      foreach (var course in data.Courses) courseGraph.AddVertex(course);
      // Add edges for degree requirements
      foreach (var degree in data.Degrees) {
        if (degree.CoRequisites.Count > 0) throw new ArgumentException("Invalid Data Entry");
        foreach (var prereq in degree.PreRequisites) {
          var course = data.GetCourseByName(prereq);
          if (course == null) throw new ArgumentException("Invalid Data Entry");
          courseGraph.AddEdge(degree, course, CourseRelation.Prereq);
        }
      }
      // Add edges for course requirements
      foreach (var course in data.Courses) {
        foreach (var coreqName in course.CoRequisites) {
          var coreq = data.GetCourseByName(coreqName);
          if (coreq == null) throw new ArgumentException("Invalid Data Entry");
          courseGraph.AddEdge(course, coreq, CourseRelation.Coreq);
        }
        foreach (var prereqName in course.PreRequisites) {
          var prereq = data.GetCourseByName(prereqName);
          if (prereq == null) throw new ArgumentException("Invalid Data Entry");
          courseGraph.AddEdge(course, prereq, CourseRelation.Prereq);
        }
      }

      return courseGraph;
    }
    /// <summary>Builds a string representing the graph in markdown mermaid flowchart format.</summary>
    public string ToMermaidString() {
      string GetNodeID(CourseVertex vert) => $"n{vert.Value.Name}";
      // https://mermaid.js.org/
      var sb = new StringBuilder();
      sb.AppendLine("%%{init: {'flowchart': {'nodeSpacing': 80, 'rankSpacing': 80}}}%%");
      sb.AppendLine("flowchart TB");
      // Add all of the graph nodes
      foreach (var vert in this.Vertices) sb.AppendLine($"  {GetNodeID(vert)}[\"{vert.Value.Name}\"]");
      // Add all of the edges
      foreach (var vert in this.Vertices) {
        foreach (var edge in vert.Edges) {
          var str = edge.Relation == CourseRelation.Prereq ? "Prereq" : "Coreq";
          sb.AppendLine($"  {GetNodeID(vert)} -->|{str}| {GetNodeID(edge.AdjVertex)}");
        }
      }
      // Style the degree nodes
      foreach (var vert in this.Vertices) {
        if (!vert.Value.IsDegree) continue;
        sb.AppendLine($"  style {GetNodeID(vert)} stroke:#000,stroke-width:4px");
        sb.AppendLine($"  style {GetNodeID(vert)} stroke-dasharray: 10,5");
      }
      return sb.ToString();
    }

    public void WriteToFile(string filename) {
      File.WriteAllText(filename, $"# Course Graph\n\n```mermaid\n{this.ToMermaidString()}\n```");
    }
  }
}
