using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CourseGraph {
  /// <summary>
  /// Represents a data bundle of course information.
  /// 
  /// This is just a convenience thing so we can load and store all information in a json file.
  /// </summary>
  public record CourseData {
    /// <summary>
    /// Any courses that are listed as degree's
    /// </summary>
    public List<Course> Degrees { get; init; }
    /// <summary>
    /// All course relations.
    /// </summary>
    public List<Course> Courses { get; init; }

    // Helpers

    /// <summary>
    /// Gets a degree by name from the course data.
    /// Time complexity: O(n)
    /// </summary>
    /// <param name="name">The degree name</param>
    /// <returns>The degree if it exists, otherwise null</returns>
#nullable enable
    public Course? GetDegreeByName(string name) {
      return this.Degrees.FirstOrDefault(c => c.Name == name);
    }

    /// <summary>
    /// Gets a course by name from the course data.
    /// Time complexity: O(n)
    /// </summary>
    /// <param name="name">The course name</param>
    /// <returns>The course if it exists, otherwise null</returns>
#nullable enable
    public Course? GetCourseByName(string name) {
      return this.Courses.FirstOrDefault(c => c.Name == name);
    }
  }

  /// <summary>
  /// Represents the relation between two courses in the graph.
  /// </summary>
  public enum CourseRelation {
    /// <summary>
    /// Indicates that the adjacent vertex is a prerequisite of the vertex.
    /// </summary>
    Prereq,
    /// <summary>
    /// Indicates that the adjacent vertex is a corequisite of the vertex.
    /// Corequisites can be taken at the same time as the vertex or any time before.
    /// </summary>
    Coreq
  }

  /// <summary>
  /// Represents an edge in the course graph.
  /// </summary>
  public class CourseEdge {
    /// <summary>
    /// The adjacent vertex this edge points to.
    /// </summary>
    public CourseVertex AdjVertex { get; set; }
    /// <summary> The relation between the vertex and the adjacent vertex. </summary>
    public CourseRelation Relation { get; set; }
    /// <summary>
    /// Initializes a new instance of the CourseEdge class with the specified adjacent vertex and relation.
    /// </summary>
    /// <param name="vertex">The adjacent vertex this edge points to.</param>
    /// <param name="relation">The relation between the vertex and the adjacent vertex.</param>
    public CourseEdge(CourseVertex vertex, CourseRelation relation) {
      this.AdjVertex = vertex;
      this.Relation = relation;
    }
  }
  /// <summary>
  /// Represents a vertex in the course graph.
  /// </summary>
  public class CourseVertex {
    /// <summary>
    /// The course this vertex represents.
    /// </summary>
    public Course Value { get; set; }
    /// <summary>
    /// Marks whether this vertex has been visited during a graph traversal.
    /// </summary>
    public bool Visited { get; set; }
    /// <summary>
    /// The edges of this vertex.
    /// </summary>
    public List<CourseEdge> Edges { get; set; }     // List of adjacency vertices

    /// <summary>Cost heuristic for scheduling.</summary>
    public double Cost { get; set; }

    /// <summary>
    /// Initializes a new instance of the CourseVertex class with the specified course value.
    /// </summary>
    /// <param name="value">The course value for this vertex.</param>
    public CourseVertex(Course value) {
      this.Value = value;
      this.Visited = false;
      this.Edges = new List<CourseEdge>();
    }


    /// <summary>
    /// Time complexity: O(v)
    /// </summary>
    /// <param name="course">The course we want to find</param>
    /// <returns>The index of the given adjacent vertex in E; otherwise returns -1</returns>
    public int FindEdgeIndex(Course course) {
      for (int i = 0; i < this.Edges.Count; i++) {
        if (this.Edges[i]?.AdjVertex?.Value?.Equals(course) ?? false)
          return i;
      }
      return -1;
    }
  }

  public interface IDirectedGraph<T, U> {
    void AddVertex(T name);
    void RemoveVertex(T name);
    void AddEdge(T name1, T name2, U cost);
    void RemoveEdge(T name1, T name2);
  }

  public class CourseGraph : IDirectedGraph<Course, CourseRelation> {
    private List<CourseVertex> Vertices { get; set; }

    public List<string> MissedOpportunities;

    public CourseGraph() {
      this.Vertices = new List<CourseVertex>();
      this.MissedOpportunities = new List<string>();
    }

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

    /// <summary>
    /// Worst case time complexity: O(v)
    /// </summary>
    /// <param name="course">The course we want to find</param>
    /// <returns>The index of the given vertex (if found); otherwise returns -1</returns>
    private int FindVertexIndex(Course course) {
      for (int i = 0; i < this.Vertices.Count; i++) {
        if (this.Vertices[i]?.Value?.Equals(course) ?? false)
          return i;
      }
      return -1;
    }

    /// <summary>
    /// Adds the given vertex to the graph
    /// Note: Duplicate vertices are not added
    /// Time complexity: O(v) due to FindVertex
    /// </summary>
    /// <param name="course">The course we want to add</param>
    public void AddVertex(Course course) {
      if (this.FindVertexIndex(course) == -1) {
        CourseVertex courseVertex = new CourseVertex(course);
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
      int i = this.FindVertexIndex(course);
      if (i > -1) {
        var courseVertex = this.Vertices[i];
        foreach (var vertex in this.Vertices) {
          foreach (var edge in vertex.Edges) {
            if (edge.AdjVertex?.Value?.Equals(course) ?? false) { // Incident edge
              vertex.Edges.Remove(edge);
              // Patch the relations
              foreach (var edge2 in courseVertex.Edges) {
                this.AddEdge(vertex.Value, edge2.AdjVertex.Value, edge2.Relation);
              }
              break;  // Since there are no duplicate edges
            }
          }
        }
        this.Vertices.RemoveAt(i);
      }
    }

    /// <summary>
    /// Returns true if there is a cycle between the given vertices
    /// Time complexity: O(v + e)
    /// </summary>
    private bool IsCyclic(int fromIndex, int toIndex) {
      for (int i = 0; i < this.Vertices.Count; i++) this.Vertices[i].Visited = false;
      var stack = new Stack<int>();
      stack.Push(fromIndex);

      while (stack.Count > 0) {
        int current = stack.Pop();
        if (current == toIndex) return true;
        if (this.Vertices[current].Visited) continue;
        this.Vertices[current].Visited = true;
        foreach (var edge in this.Vertices[current].Edges) {
          int adjacentIndex = this.FindVertexIndex(edge.AdjVertex.Value);
          if (adjacentIndex >= 0) stack.Push(adjacentIndex);
        }
      }
      return false;
    }

    /// <summary>
    /// Adds the given edge (name1, name2) to the graph
    /// Notes: Duplicate edges are not added
    ///        By default, the cost of the edge is 0
    ///        We don't add an edge if a cycle so it does not become a problem
    /// Worst case time complexity: O(n+m)
    /// </summary>
    public void AddEdge(Course course1, Course course2, CourseRelation relation) {
      int course1Index = this.FindVertexIndex(course1);
      int course2Index = this.FindVertexIndex(course2);

      // Do the vertices exist?
      if (course1Index > -1 && course2Index > -1) {
        // Does the edge not already exist?
        if (this.Vertices[course1Index].FindEdgeIndex(course2) == -1) {
          if (this.IsCyclic(course2Index, course1Index)) {
            throw new ArgumentException("CourseGraph cannot contain cycles");
          }

          CourseEdge courseEdge = new CourseEdge(this.Vertices[course2Index], relation);
          this.Vertices[course1Index].Edges.Add(courseEdge);
        }
      }
    }

    /// <summary>
    /// Removes the given edge (name1, name2) from the graph
    /// Note: Nothing is done if the edge does not exist
    /// Time complexity: O(e)
    /// </summary>
    public void RemoveEdge(Course course1, Course course2) {
      int course1Index = this.FindVertexIndex(course1);
      if (course1Index <= -1) return;
      var course1Vertex = this.Vertices[course1Index];
      int course2Index = course1Vertex.FindEdgeIndex(course2);
      if (course2Index <= -1) return;
      course1Vertex.Edges.RemoveAt(course2Index);
    }

    /// <summary>
    /// Toggle's weather a given course is required by a given degree.
    /// </summary>
    /// <param name="course">The course you want to update</param>
    /// <param name="degree">The degree to toggle requirement</param>
    /// <exception cref="ArgumentException">Degree was not a degree course.</exception>
    public void UpdateVertex(Course course, Course degree) {
      if (!degree.IsDegree) {
        throw new ArgumentException("Expected a degree course");
      }
      bool foundLink = false;
      int degreeIndex = this.FindVertexIndex(degree);
      CourseVertex degreeVertex = this.Vertices[degreeIndex];
      if (degreeIndex >= 0) {
        foreach (var edge in degreeVertex.Edges) {
          if (edge.AdjVertex?.Value?.Equals(course) ?? false) { // Incident edge
            // A degree node is a root required node.
            degreeVertex.Edges.Remove(edge); // Remove the link
            // Patch the course data
            degreeVertex.Value.CoRequisites.Remove(course.Name);
            degreeVertex.Value.PreRequisites.Remove(course.Name);
            foundLink = true;
            break; // Edges are de-duplicated
          }
        }
      }
      if (!foundLink) {
        if (!degree.PreRequisites.Contains(course.Name))
          degree.PreRequisites.Add(course.Name);
        this.AddEdge(degree, course, CourseRelation.Prereq);
      }
    }

    /// <summary>
    /// Computes TermMin and TermMax for each course based on the degree requirements.
    /// Time complexity: O(v + e)
    /// </summary>
    /// <param name="termSize">Number of courses per term</param>
    /// <param name="creditCount">Total number of credits required</param>
    /// <param name="degreeCourse">The course representing the degree</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="degreeCourse"/> is not found in graph
    /// or is not a root node (has incommming edges).
    /// </exception>
    public Schedule.Schedule Schedule(int termSize, int creditCount, Course degreeCourse) {
      /*
       * NOTE:
       *   The problem defined by the assignment is to find a schedule assuming there are no 
       *   overlapping timeSlots or term constraints. This is a much easier problem than the 
       *   general scheduling problem, which is NP-hard (https://en.wikipedia.org/wiki/NP-hardness),
       *   in other words there is no known polynomial time algorithm to solve the general scheduling problem.
       *   Instead this sort of problem is often solve using a constraint based approach where you encode all
       *   the constraints and then use a constraint solver to find a solution that satisfies all the constraints.
       *
       *   As we are avoiding libraries for the direct implementation of the problem and don't have access to a 
       *   constraint solver, we decided to use combined constraint and greedy placement approach, this is guaranteed to
       *   find the optimal solution to the problem defined by the assignment, but may not find the most optimal solution 
       *   to the general scheduling problem.
       *
       *   The way this algorithm works is we begin by computing `T_min` and `T_max` for each course:
       *     - `T_min` is defined at the minimum term that a course can be taken in, 
       *               this is computed by figuring out the longest path from to a leaf node.
       *     - `T_max` is defined as the maximum term that a course can be taken in based on the degree requirements,
       *               this is computed as `TermTypeCount*CreditCount - depth` where depth is the depth of the 
       *               course in the longest path from a root node. If the course is part of the given degree,
       *               depth is computed as the longest path form the degree node.
       *   These constraints tell us optimal placement restrictions for each course however we may need to shift courses around.
       *   This could be due to a course completely overlapping with another course or to many courses in one term.
       *
       *   Interesting Paper: https://www.sciencedirect.com/science/article/abs/pii/S0360835221007592#s0090
       *   While we didn't take inspiration from this paper as we already figured out an approach prior to reading it, 
       *   this paper describes how to solve the problem using a completely constraint based approach and also describes
       *   some additional constraints that could be used to make our scheduler more optimal and quicker.
       *
       *   A major strength of the approach we took is that additional constraints can be added and used as weights or filters
       *   while greedy scheduling to find more optimal or find tuned solutions, and we can greedy schedule things that would 
       *   make constraints more complex. For example if we wanted to factor in travel time constraints wouldn't adjust well 
       *   but we could just buffer our times in greedy scheduling.
       *
       *   Once we have these constraints, we begin a greedy scheduling approach,
       * TODO:
       */

      if (creditCount > this.Vertices.Count)
        throw new ArgumentException("Impossible to fill credit count, there aren't enough courses");
      // -------------- Compute Constraints --------------
      // Find all root vertices (vertices with no incoming edges)
      List<CourseVertex> roots = new List<CourseVertex>(this.Vertices);
      foreach (var vert in this.Vertices) {
        vert.Cost = 0;
        vert.Visited = false;
        foreach (var edge in vert.Edges) {
          // If the root has an incoming edge, it isn't a root
          roots.RemoveAll(r => r.Value.Equals(edge.AdjVertex.Value));
        }
      }
      // Make sure degreeCourse is in roots
      int degreeIdx = this.FindVertexIndex(degreeCourse);
      if (degreeIdx < 0) throw new ArgumentException("Degree: {} course must be in the graph");
      CourseVertex degreeVertex = this.Vertices[degreeIdx];
      if (!roots.Contains(degreeVertex)) {
        throw new ArgumentException("Degree course must be a root node");
      } else {
        roots.Remove(degreeVertex);
        roots.Add(degreeVertex);
      }

      this.ComputeCostHeuristic(degreeVertex);

      // -------------- Greedy Schedule --------------
      var schedule = new Schedule.Schedule(degree: degreeCourse, maxTermSize: termSize);
      // Order by cost heuristic (ascending so stack pops highest cost / deepest first).
      // First we schedule the required courses
      var coursesToPlace = new Stack<CourseVertex>(degreeVertex.Edges.Select(e => e.AdjVertex).OrderBy(c => c.Cost));
      while (coursesToPlace.Count > 0) {
        var courseToPlace = coursesToPlace.Pop();
        if (courseToPlace.Visited) continue; // We've already placed this course
        foreach (var course in this.TopologicalSort(courseToPlace)) {
          this.PlaceInScheduleData(schedule, course, creditCount);
        }
        if (!courseToPlace.Visited)
          throw new Exception("Impossible: Failed to place required course");
      }

      // Begin placing filler courses (Non required courses used to hit creditCount)
      var fillerCourses = new Stack<CourseVertex>(this.Vertices.Where(v => !v.Visited && !v.Value.IsDegree).OrderBy(v => v.Cost));
      while (fillerCourses.Count > 0 && schedule.CourseCount < creditCount) {
        var fillerCourse = fillerCourses.Pop();
        if (fillerCourse.Visited) continue; // We've already placed this course
        // NOTE: This isn't giving us optimal yet due to co-req spacing and such
        try {
          foreach (var course in this.TopologicalSort(fillerCourse)) {
            this.PlaceInScheduleData(schedule, course, creditCount);
          }
        }
        catch {
          continue;
        }
        if (!fillerCourse.Visited)
          throw new Exception("Impossible: Failed to place unrequired course");
      }

      // Ensure we hit the requirements and return
      if (schedule.CourseCount < creditCount)
        throw new Exception($"Impossible: Failed to hit credit count, CourseCount {schedule.CourseCount}, creditCount: {creditCount}");
      return schedule;
    }

    private void PlaceInScheduleData(
      Schedule.Schedule schedule,
      CourseVertex courseVertex,
      int creditCount
    ) {
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
      // We will never actually end up looping this many times unless all courses are on a 
      // tuesday at 2pm in the fall (or rather just worst case scenario).
      for (int i = courseMinimumTerm; i < Enum.GetValues(typeof(Term)).Length * creditCount; i++) {
        if (schedule.IsTermFull(i)) continue; // Can't place in a full term.
        var termType = schedule.GetTermType(i);
        var possibleTimeSlots = course.TimeTableInfos.Where(slot => slot.OfferedTerm == termType);
        if (!possibleTimeSlots.Any()) continue; // No timeSlots exist this semester
        if (schedule.GetCourseValidTimeSlots(course, possibleTimeSlots.ToArray(), i).Count <= 0) {
          this.MissedOpportunities.Add($"Couldn't schedule {course.Name} in earliest term {i} because of schedule conflict");
          continue; // No available timeslot
        }
        // Finally add the course
        schedule.AddCourse(course: course, term: i);
        courseVertex.Visited = true;
        break;
      }
      if (courseVertex.Visited == false) {
        throw new Exception($"Failed to place course: ${course.Name}");
      }
    }

    /// <summary>
    /// Provides nodes back in topological order (a node is always after its prereqs and coreqs).
    /// 
    /// https://www.geeksforgeeks.org/dsa/topological-sorting-indegree-based-solution/
    /// </summary>
    private IEnumerable<CourseVertex> TopologicalSort(CourseVertex root) {
      // An enumerator means that we can use this like an iterator despite it being a function.
      var visited = new HashSet<CourseVertex>();
      var tempMark = new HashSet<CourseVertex>();

      foreach (var v in this.Visit(root, visited, tempMark))
        yield return v;
    }

    private IEnumerable<CourseVertex> Visit(CourseVertex vertex, HashSet<CourseVertex> visited, HashSet<CourseVertex> tempMark) {
      if (visited.Contains(vertex))
        yield break;

      if (tempMark.Contains(vertex)) // NOTE: This is impossible because of constraints on AddEdge
        throw new InvalidOperationException("Graph is not a DAG! Cycle detected.");

      tempMark.Add(vertex);

      foreach (var edge in vertex.Edges) {
        if (edge?.AdjVertex != null) {
          foreach (var v in this.Visit(edge.AdjVertex, visited, tempMark))
            yield return v;
        }
      }

      tempMark.Remove(vertex);
      visited.Add(vertex);

      yield return vertex; // post-order yield
    }

    /// <summary>
    /// Cost = longest path from root
    /// Time complexity O(V+E)
    /// </summary>
    /// <param name="degreeVertex">The phantom vertex for the degree (root).</param>
    private void ComputeCostHeuristic(CourseVertex degreeVertex) {
      foreach (var vertex in this.Vertices) vertex.Cost = 0;
      degreeVertex.Cost = 0;

      var order = this.TopologicalSort(degreeVertex).ToList();
      foreach (var vertex in order) {
        foreach (var edge in vertex.Edges) {
          var prereqVertex = edge.AdjVertex;
          double weight = edge.Relation == CourseRelation.Prereq ? CourseGraph.PrereqWeight : CourseGraph.CoreqWeight;
          double newCost = prereqVertex.Cost + weight;
          if (newCost > vertex.Cost) vertex.Cost = newCost;
        }
      }
    }

    /// <summary>
    /// Extract the course data from the graph.
    /// </summary>
    /// <returns>All degrees and courses from the graph.</returns>
    public CourseData GetCourseData() {
      // Collect all degree nodes and count them as degree
      // Collect all non-degree nodes and count them as course
      var degrees = new List<Course>();
      var courses = new List<Course>();

      foreach (var vertex in this.Vertices) {
        if (vertex.Value.IsDegree) {
          degrees.Add(vertex.Value);
        } else {
          courses.Add(vertex.Value);
        }
      }

      return new CourseData {
        Degrees = degrees,
        Courses = courses
      };
    }

    /// <summary>
    /// Constructs a CourseGraph from the given CourseData.
    /// </summary>
    /// <param name="data">The course data to generate a graph from.</param>
    /// <returns>A new courseGraph</returns>
    /// <exception cref="ArgumentException">If the data entry is invalid</exception>
    public static CourseGraph FromCourseData(CourseData data) {
      var courseGraph = new CourseGraph();
      // Add all degrees as vertices
      foreach (var degree in data.Degrees) {
        courseGraph.AddVertex(degree);
      }
      // Add all courses as vertices
      foreach (var course in data.Courses) {
        courseGraph.AddVertex(course);
      }

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

    /// <summary>
    /// Prints out all vertices of a graph
    /// Time complexity: O(v)
    /// </summary>
    public void PrintVertices() {
      foreach (var vertex in this.Vertices) {
        Console.WriteLine(vertex.Value);
      }
      Console.ReadLine();
    }

    /// <summary>
    /// Prints out all edges of the graph
    /// Time complexity: O(e)
    /// </summary>
    public void PrintEdges() {
      foreach (var vertex in this.Vertices) {
        foreach (var edge in vertex.Edges) {
          Console.WriteLine("(" + vertex.Value + "," + edge.AdjVertex.Value + "," + edge.Relation + ")");
          Console.ReadLine();
        }
      }
    }

    /// <summary>
    /// Returns the graph in Mermaid flowchart format.
    /// </summary>
    public string ToMermaidString() {
      var sb = new StringBuilder();
      sb.AppendLine("%%{init: {'flowchart': {'nodeSpacing': 80, 'rankSpacing': 80}}}%%");
      sb.AppendLine("flowchart TB");

      foreach (var vertex in this.Vertices) {
        string id = "n" + vertex.Value.Name;
        sb.AppendLine($"  {id}[\"{vertex.Value.Name}\"]");
      }

      foreach (var vertex in this.Vertices) {
        string fromId = "n" + vertex.Value.Name;
        foreach (var edge in vertex.Edges) {
          string toId = "n" + edge.AdjVertex.Value.Name;
          if (edge.Relation == CourseRelation.Prereq)
            sb.AppendLine($"  {fromId} -->|Prereq| {toId}");
          else
            sb.AppendLine($"  {fromId} -->|Coreq| {toId}");
        }
      }

      foreach (var vertex in this.Vertices) {
        if (vertex.Value.IsDegree) {
          sb.AppendLine($"  style n{vertex.Value.Name} stroke:#000,stroke-width:4px");
          sb.AppendLine($"  style n{vertex.Value.Name} stroke-dasharray: 10,5");
        }
      }
      return sb.ToString();
    }

    public void WriteToFile(string filename) {
      File.WriteAllText(filename, "# Course Graph\n\n```mermaid\n" + this.ToMermaidString() + "\n```");
    }
  }
}
