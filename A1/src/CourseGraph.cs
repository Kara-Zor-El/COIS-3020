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

    /// <summary>Minimum term this course can be taken in.</summary>
    public int TermMin { get; set; }
    /// <summary>Maximum term this course can be taken in (based on the degree).</summary>
    public int TermMax { get; set; }

    public CourseVertex(Course value) {
      this.Value = value;
      this.Visited = false;
      this.Edges = new List<CourseEdge>();
      this.TermMin = 0;
      this.TermMax = 0;
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

    public CourseGraph() {
      this.Vertices = new List<CourseVertex>();
    }

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
    /// NOTE: This also adds the prerequiste and corequsite courses.
    /// Time complexity: O(v) due to FindVertex
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
        foreach (var vertex in this.Vertices) {
          foreach (var edge in vertex.Edges) {
            if (edge.AdjVertex?.Value?.Equals(course) ?? false) { // Incident edge
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
      }
    }

    /// <summary>
    /// Returns true if there is a cycle between the given vertices
    /// TODO: Change it
    /// Time complexity: O(v + e)
    /// </summary>
    private bool IsCyclic(int fromIndex, int toIndex) {
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
          if (this.IsCyclic(course1Index, course2Index)) {
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
    /// Depth-First Search
    /// Performs a depth-first search (with re-start)
    /// Time complexity: O(max,(v,e))
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
    /// Time Complexity: O(max(v,e))
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
      CourseVertex degreeVertex = this.Vertices[degreeIndex];
      if (degreeIndex >= 0) {
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
    }

    /// <summary>
    /// Computes TermMin and TermMax for each course based on the degree requirements.
    /// Time complexity: O(v + e)
    /// </summary>
    /// <param name="termSize">Number of courses per term</param>
    /// <param name="creditCount">Total number of credits required</param>
    /// <param name="degreeCourse">The phantom course representing the degree</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="degreeCourse"/> is not found in graph
    /// or is not a root node (has incommming edges).
    /// </exception>
    public void Schedule(int termSize, int creditCount, Course degreeCourse) {
      // Find all root vertices (vertices with no incoming edges)
      List<CourseVertex> roots = new List<CourseVertex>(this.Vertices);

      foreach (var vert in this.Vertices) {
        // Clear TermMin and TermMax for all vertices
        // and set all vertices as unvisited
        vert.TermMin = 0;
        vert.TermMax = 0;
        vert.Visited = false;
        foreach (var edge in vert.Edges) {
          roots.RemoveAll(r => r.Value.Equals(edge.AdjVertex.Value));
        }
      }

      // Make sure degreeCourse is in roots
      int degreeIdx = this.FindVertexIndex(degreeCourse);
      if (degreeIdx < 0) throw new ArgumentException("Degree course must be in the graph");
      CourseVertex degreeVertex = this.Vertices[degreeIdx];
      if (!roots.Contains(degreeVertex)) {
        throw new ArgumentException("Degree course must be a root node");
      } else {
        roots.Remove(degreeVertex);
        roots.Add(degreeVertex);
      }

      // Compute TermMin and TermMax for each root
      foreach (var root in roots) {
        this.ComputeTermBoundsForDegree(root, termSize, creditCount, root == degreeVertex);
      }
    }

    /// <summary>
    /// DFS to compute TermMin and TermMax for courses of a given root.
    /// </summary>
    /// <param name="root">The course to start from</param>
    /// <param name="termSize">Number of courses per term</param>
    /// <param name="creditCount">Total number of credits required</param>
    /// <param name="isDegreeCourse">Whether this root is the degree course (phantom course)</param>
    private void ComputeTermBoundsForDegree(CourseVertex root, int termSize, int creditCount, bool isDegreeCourse) {
      // Stack stores: (vertex, depth, isReturning (returning from children))
      // TODO: Heavily test this
      var stack = new Stack<(CourseVertex vertex, int depth, bool isReturning)>();

      stack.Push((root, 0, false));

      while (stack.Count > 0) {
        var (vertex, depth, isReturning) = stack.Pop();

        if (isReturning) {
          if (!vertex.Value.IsPhantom) {
            int earliestTerm = 1;
            foreach (var edge in vertex.Edges) {
              if (edge.Relation == CourseRelation.Prereq) {
                // Prerequisites must be completed before this course
                earliestTerm = Math.Max(earliestTerm, edge.AdjVertex.TermMin + 1);
              } else {
                // Corequisites can be taken at the same time
                earliestTerm = Math.Max(earliestTerm, edge.AdjVertex.TermMin);
              }
            }
            vertex.TermMin = Math.Max(vertex.TermMin, earliestTerm);
          }

          if (!vertex.Value.IsPhantom) {
            // Compute TermMax: TermMax = termSize * creditCount - depth
            int newTMax = termSize * creditCount - depth;

            // Update TermMax
            if (isDegreeCourse || vertex.TermMax == 0) {
              vertex.TermMax = newTMax;
            } else {
              vertex.TermMax = Math.Max(vertex.TermMax, newTMax);
            }
          }

          continue;
        }

        if (vertex.Visited) continue;

        vertex.Visited = true;
        stack.Push((vertex, depth, true));

        // Push children onto stack
        foreach (var edge in vertex.Edges) {
          if (!edge.AdjVertex.Visited) {
            // For corequisites, don't increase depth
            var nextDepth = depth;
            if (edge.Relation == CourseRelation.Prereq) nextDepth++;
            stack.Push((edge.AdjVertex, nextDepth, false));
          }
        }
      }
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
        string id = "n" + vertex.Value.ID;
        sb.AppendLine($"  {id}[\"{vertex.Value.Name}\"]");
      }

      foreach (var vertex in this.Vertices) {
        string fromId = "n" + vertex.Value.ID;
        foreach (var edge in vertex.Edges) {
          string toId = "n" + edge.AdjVertex.Value.ID;
          if (edge.Relation == CourseRelation.Prereq)
            sb.AppendLine($"  {fromId} -->|Prereq| {toId}");
          else
            sb.AppendLine($"  {fromId} -->|Coreq| {toId}");
        }
      }

      foreach (var vertex in this.Vertices) {
        if (vertex.Value.IsPhantom) {
          sb.AppendLine($"  style n{vertex.Value.ID} stroke:#000,stroke-width:4px");
          sb.AppendLine($"  style n{vertex.Value.ID} stroke-dasharray: 10,5");
        }
      }
      return sb.ToString();
    }

    public void WriteToFile(string filename) {
      File.WriteAllText(filename, "# Course Graph\n\n```mermaid\n" + this.ToMermaidString() + "\n```");
    }
  }
}
