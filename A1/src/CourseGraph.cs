using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CourseGraph {

  public enum CourseRelation {
    Prereq,
    Coreq
  }
  public class CourseEdge {
    public CourseVertex AdjVertex { get; set; }
    public CourseRelation Relation { get; set; }

    public CourseEdge(CourseVertex vertex, CourseRelation relation) {
      this.AdjVertex = vertex;
      this.Relation = relation;
    }


  }

  public class CourseVertex {
    public Course Value { get; set; }
    public bool Visited { get; set; }
    public List<CourseEdge> Edges { get; set; }     // List of adjacency vertices
    /// <summary>Minimum term this course can be taken in (at most N courses per term).</summary>
    public int TMin { get; set; }

    public CourseVertex(Course value) {
      this.Value = value;
      this.Visited = false;
      this.Edges = new List<CourseEdge>();
      this.TMin = 0;
    }


    /// <summary>
    /// Time complexity: O(n) where n is the number of vertices
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
    public List<CourseVertex> Vertices { get; private set; }
    public int TGlobalMax { get; private set; }
    public int NPerTerm { get; private set; }

    public CourseGraph(int nPerTerm) {
      this.Vertices = new List<CourseVertex>();
      this.TGlobalMax = 0;
      if (nPerTerm <= 0) throw new ArgumentException("N must be greater than 0");
      this.NPerTerm = nPerTerm;
    }

    /// <summary>
    /// Time complexity: O(n)
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
    /// NOTE: This also adds the prerequiste and corequsite courses.
    /// Time complexity: O(n) due to FindVertex
    /// </summary>
    /// <param name="course">The course we want to add</param>
    public void AddVertex(Course course) {
      if (this.FindVertexIndex(course) == -1) {
        CourseVertex courseVertex = new CourseVertex(course);
        this.Vertices.Add(courseVertex);
        // Add the relations
        foreach (var coRequisite in course.CoRequisites) {
          this.AddVertex(coRequisite);
          this.AddEdge(course, coRequisite, CourseRelation.Coreq);
        }
        foreach (var preRequisite in course.PreRequisites) {
          this.AddVertex(preRequisite);
          this.AddEdge(course, preRequisite, CourseRelation.Prereq);
        }
      }
      RecomputeTermBounds();
    }

    /// <summary>
    /// Removes the given vertex and all incident edges from the graph
    /// Note: Nothing is done if the vertex does not exist
    /// Time complexity: O(max(n,m)) where m is the number of edges 
    /// </summary>
    /// <param name="course">The courser we want to remove</param>
    public void RemoveVertex(Course course) {
      // If a course B is removed then its pre- and co-requisite courses
      // become the pre- and co-requisite courses for those course for which 
      // B was a pre- and co-requisite. 
      int i = this.FindVertexIndex(course);
      if (i > -1) {
        foreach (var vertex in this.Vertices) {
          foreach (var edge in vertex.Edges) {
            if (edge.AdjVertex?.Value?.Equals(course) ?? false) { // Incident edge
              // Vertex is the node this edge points to?
              vertex.Edges.Remove(edge);
              // Patch the relations
              foreach (var coRequisite in course.CoRequisites) {
                this.AddEdge(vertex.Value, coRequisite, CourseRelation.Coreq);
              }
              foreach (var preRequisite in course.PreRequisites) {
                this.AddEdge(vertex.Value, preRequisite, CourseRelation.Prereq);
              }
              break;  // Since there are no duplicate edges
            }
          }
        }
        this.Vertices.RemoveAt(i);
        RecomputeTermBounds();
      }
    }

    /// <summary>
    /// Returns true if the graph contains a directed cycle (DFS with recursion stack).
    /// Time complexity: O(n + m)
    /// </summary>
    private bool IsCyclic(Course course1, HashSet<CourseVertex> visited = null) {
      if (visited == null) visited = new HashSet<CourseVertex>();

      // Does the course exist?
      int course1Index = this.FindVertexIndex(course1);
      if (course1Index <= -1) return false;

      var course1Vertex = this.Vertices[course1Index];

      // Has this course been visited?
      if (visited.Contains(course1Vertex)) return true;

      // Add the course to the visited set
      visited.Add(course1Vertex);

      // Check the edges of the course
      foreach (var edge in course1Vertex.Edges) {
        if (this.IsCyclic(edge.AdjVertex.Value, visited)) return true;
      }
      visited.Remove(course1Vertex);
      return false;
    }

    /// <summary>
    /// Adds the given edge (name1, name2) to the graph
    /// Notes: Duplicate edges are not added
    ///        By default, the cost of the edge is 0
    /// Time complexity: O(n)
    /// </summary>
    public void AddEdge(Course course1, Course course2, CourseRelation relation) {
      int course1Index = this.FindVertexIndex(course1);
      int course2Index = this.FindVertexIndex(course2);

      // Do the vertices exist?
      if (course1Index > -1 && course2Index > -1) {
        // Does the edge not already exist?
        if (this.Vertices[course1Index].FindEdgeIndex(course2) == -1) {
          CourseEdge courseEdge = new CourseEdge(this.Vertices[course2Index], relation);
          this.Vertices[course1Index].Edges.Add(courseEdge);
        }
      }
      RecomputeTermBounds();
      if (this.IsCyclic(course1)) throw new ArgumentException("CourseGraph cannot contain cyclic relations.");
    }

    /// <summary>
    /// Removes the given edge (name1, name2) from the graph
    /// Note: Nothing is done if the edge does not exist
    /// Time complexity: O(n)
    /// </summary>
    public void RemoveEdge(Course course1, Course course2) {
      int course1Index = this.FindVertexIndex(course1);
      if (course1Index <= -1) return;
      var course1Vertex = this.Vertices[course1Index];
      int course2Index = course1Vertex.FindEdgeIndex(course2);
      if (course2Index <= -1) return;
      course1Vertex.Edges.RemoveAt(course2Index);
      RecomputeTermBounds();
    }

    /// <summary>
    /// Recomputes t_min (per vertex) and t_global_max using Kahn's algorithm
    /// See: https://www.geeksforgeeks.org/dsa/topological-sorting-indegree-based-solution/ for algorithm details
    /// Time complexity: O(n+m)
    /// </summary>
    private void RecomputeTermBounds() {
      // Handle empty graph
      if (this.Vertices.Count == 0) {
        this.TGlobalMax = 0;
        return;
      }

      // Reset global maximum
      this.TGlobalMax = 0;

      // Build reverse prereq map (maps each course to all courses that depend on it)
      var revPrereq = new Dictionary<CourseVertex, List<CourseVertex>>();
      foreach (var v in this.Vertices) {
        revPrereq[v] = new List<CourseVertex>();
      }

      foreach (var w in this.Vertices) {
        foreach (var e in w.Edges.Where(e => e.Relation == CourseRelation.Prereq)) {
          revPrereq[e.AdjVertex].Add(w);
        }
      }

      // Build in-degree map (count of prereq dependencies for each course)
      var inDegree = new Dictionary<CourseVertex, int>();
      foreach (var v in this.Vertices) {
        inDegree[v] = v.Edges.Count(e => e.Relation == CourseRelation.Prereq);
      }

      // Kahn's algorithm (builds a topological ordering)
      var order = new List<CourseVertex>();
      var kahnQ = new Queue<CourseVertex>();

      // Start with all vertices with no prereq dependencies
      foreach (var v in this.Vertices.Where(v => inDegree[v] == 0)) {
        kahnQ.Enqueue(v);
      }

      // Proces them in order
      while (kahnQ.Count > 0) {
        var v = kahnQ.Dequeue();
        order.Add(v);

        // For each course that depend on this one
        foreach (var w in revPrereq[v]) {
          inDegree[w]--;
          if (inDegree[w] == 0) {
            kahnQ.Enqueue(w);
          }
        }
      }

      // Verify that the sorting suceeded (if it has a cycle, the topological order will not equal the number of vertices)
      if (order.Count != this.Vertices.Count) {
        throw new InvalidOperationException("CourseGraph contains a cycle");
      }

      // Compute term assignments assuming 1 course per term for _tGlobalMax
      var termCapacityOne = new Dictionary<CourseVertex, int>();
      foreach (var v in order) {
        // Phantom courses don't occuy term space
        if (v.Value.IsPhantom) {
          termCapacityOne[v] = 0;
          continue;
        }

        // Start at term 1
        int earliestTerm = 1;

        // Must come after all prerequisites
        foreach (var e in v.Edges.Where(e => e.Relation == CourseRelation.Prereq)) {
          termCapacityOne.TryGetValue(e.AdjVertex, out int prereqTerm);
          if (prereqTerm > 0) {
            earliestTerm = Math.Max(earliestTerm, prereqTerm + 1);
          }
        }

        // Coreqs can be taken in the same term or earlier 
        // so they just need to be scheduled by this term at the latest
        foreach (var e in v.Edges.Where(e => e.Relation == CourseRelation.Coreq)) {
          termCapacityOne.TryGetValue(e.AdjVertex, out int coreqTerm);
          if (coreqTerm > 0) {
            earliestTerm = Math.Max(earliestTerm, coreqTerm);
          }
        }

        // Assign the earliest term
        termCapacityOne[v] = earliestTerm;
        this.TGlobalMax = Math.Max(this.TGlobalMax, earliestTerm);
      }

      if (this.NPerTerm <= 0) return;

      foreach (var v in order.Where(v => !v.Value.IsPhantom)) {
        v.TMin = termCapacityOne[v];
      }
    }

    /// <summary>
    /// Depth-First Search
    /// Performs a depth-first search (with re-start)
    /// Time complexity: O(max,(n,m))
    /// </summary>
    public void DepthFirstSearch() {
      foreach (var vertex in this.Vertices) {
        vertex.Visited = false; // Set all vertices as unvisited
      }
      foreach (var vertex in this.Vertices) {
        if (vertex.Visited) continue;
        // (Re)start with vertex i
        this.DepthFirstSearch(vertex);
        Console.WriteLine();
      }
    }

    private void DepthFirstSearch(CourseVertex vertex) {
      vertex.Visited = true;    // Output vertex when marked as visited
      Console.WriteLine(vertex.Value);

      foreach (var edge in vertex.Edges) { // Visit next adjacent vertex
        var adjacentVertex = edge.AdjVertex;  // Find adjacent vertex in edge
        if (!adjacentVertex.Visited) {
          this.DepthFirstSearch(adjacentVertex);
        }
      }
    }

    /// <summary>
    /// Breadth-First Search
    /// Performs a breadth-first search (with re-start)
    /// Time Complexity: O(max(n,m))
    /// </summary>
    public void BreadthFirstSearch() {
      foreach (var vertex in this.Vertices) {
        vertex.Visited = false; // Set all vertices as unvisited
      }

      foreach (var vertex in this.Vertices) {
        if (vertex.Visited) continue;
        this.BreadthFirstSearch(vertex);
        Console.WriteLine();
      }
    }

    private void BreadthFirstSearch(CourseVertex vertex) {
      Queue<CourseVertex> vertexQueue = new Queue<CourseVertex>();
      vertex.Visited = true;  // Mark vertex as visited when placed in the queue
      vertexQueue.Enqueue(vertex);
      while (vertexQueue.Count > 0) {
        var currVertex = vertexQueue.Dequeue(); // Output vertex when removed from the queue
        Console.WriteLine(currVertex.Value);
        // Enqueue unvisited adjacent vertices
        foreach (var edge in currVertex.Edges) {
          var adjacentVertex = edge.AdjVertex;
          if (adjacentVertex.Visited) continue;
          adjacentVertex.Visited = true;  // Mark vertex as visited
          vertexQueue.Enqueue(adjacentVertex);
        }
      }
    }
    /// <summary>
    /// Toggle's weather a given course is required by a given degree.
    /// </summary>
    /// <param name="course">The course you want to update</param>
    /// <param name="degree">The degree to toggle requirement</param>
    /// <exception cref="ArgumentException">Degree was not a phantom course.</exception>
    public void UpdateVertex(Course course, Course degree) {
      if (!degree.IsPhantom) {
        throw new ArgumentException("A degree is expected to be a phantom course");
      }
      // TODO: Test this fully
      bool foundLink = false;
      int degreeIndex = this.FindVertexIndex(degree);
      if (degreeIndex >= 0) {
        var degreeVertex = this.Vertices[degreeIndex];
        foreach (var edge in degreeVertex.Edges) {
          if (edge.AdjVertex?.Value?.Equals(course) ?? false) { // Incident edge
            // A phantom node indicates that it is a node representing a degree as such it is a root required node.
            degreeVertex.Edges.Remove(edge); // Remove the link
            degreeVertex.Value.CoRequisites.Remove(course); // Remove the Corequisite
            degreeVertex.Value.PreRequisites.Remove(course); // Remove the rerequisite
            foundLink = true;
            break; // Edges are de-duplicated
          }
        }
      }
      if (!foundLink) {
        if (!degree.PreRequisites.Contains(course))
          degree.PreRequisites.Add(course);
        this.AddEdge(degree, course, CourseRelation.Prereq);
      }
      RecomputeTermBounds();
    }

    /// <summary>
    /// Prints out all vertices of a graph
    /// Time complexity: O(n)
    /// </summary>
    public void PrintVertices() {
      foreach (var vertex in this.Vertices) {
        Console.WriteLine(vertex.Value);
      }
      Console.ReadLine();
    }

    /// <summary>
    /// Prints out all edges of the graph
    /// Time complexity: O(m)
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

      foreach (var v in this.Vertices) {
        string id = "n" + v.Value.ID;
        sb.AppendLine($"  {id}[\"{v.Value.Name}\"]");
      }

      foreach (var v in this.Vertices) {
        string fromId = "n" + v.Value.ID;
        foreach (var e in v.Edges) {
          string toId = "n" + e.AdjVertex.Value.ID;
          if (e.Relation == CourseRelation.Prereq)
            sb.AppendLine($"  {fromId} -->|Prereq| {toId}");
          else
            sb.AppendLine($"  {fromId} -->|Coreq| {toId}");
        }
      }

      foreach (var v in this.Vertices) {
        if (v.Value.IsPhantom) {
          sb.AppendLine($"  style n{v.Value.ID} stroke:#000,stroke-width:4px");
          sb.AppendLine($"  style n{v.Value.ID} stroke-dasharray: 10,5");
        }
      }
      return sb.ToString();
    }

    public void WriteToFile(string filename) {
      File.WriteAllText(filename, "# Course Graph\n\n```mermaid\n" + this.ToMermaidString() + "\n```");
    }
  }
}
