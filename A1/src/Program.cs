using CourseGraph;
using System.Collections.Generic;
using System;

CourseGraph.CourseGraph graph = new CourseGraph.CourseGraph();


// Year 1
Course cois1010 = new Course(1010, "COIS-1010H: The Digital World", new List<Course>(), new List<Course>());
Course cois1020 = new Course(1020, "COIS-1020H: Programming for Computing Systems", new List<Course> { cois1010 }, new List<Course> { cois1010 });
Course cois1400 = new Course(1400, "COIS-1400H: Introduction to Data Science", new List<Course> { cois1020 }, new List<Course> { cois1020 });
Course cois1620 = new Course(1620, "COIS-1620H: Introduction to Information Systems", new List<Course>(), new List<Course> { cois1010 });
Course math1350 = new Course(1350, "MATH-1350H: Linear Algebra I", new List<Course>(), new List<Course>());
Course math1550 = new Course(1550, "MATH-1550H: Probability I", new List<Course>(), new List<Course>());
Course math1110 = new Course(1110, "MATH-1110H: Calculus I", new List<Course>(), new List<Course>());
graph.AddVertex(cois1010);
graph.AddVertex(cois1020);
graph.AddVertex(cois1400);
graph.AddVertex(cois1620);
graph.AddVertex(math1350);
graph.AddVertex(math1550);
graph.AddVertex(math1110);

// Year 2
Course cois2020 = new Course(2020, "COIS-2020H: Data Structures and Algorithms", new List<Course>(), new List<Course> { cois1020 });
Course cois2240 = new Course(2240, "COIS-2240H: Software Design and Modelling", new List<Course>(), new List<Course> { cois1020 });
Course cois2300 = new Course(2300, "COIS-2300H: Computer Organization", new List<Course>(), new List<Course> { cois1020 });
Course cois2430 = new Course(2430, "COIS-2430H: Web Development", new List<Course>(), new List<Course> { cois1020 });
Course math2600 = new Course(2600, "MATH-2600H: Discrete Structures", new List<Course>(), new List<Course> { math1350 });
graph.AddVertex(cois2020);
graph.AddVertex(cois2240);
graph.AddVertex(cois2300);
graph.AddVertex(cois2430);
graph.AddVertex(math2600);

// Year 3
Course cois3020 = new Course(3020, "COIS-3020H: Data Structures and Algorithms II", new List<Course>(), new List<Course> { cois2020, math2600 });
Course cois3380 = new Course(3380, "COIS-3380H: Systems Programming in C", new List<Course>(), new List<Course> { cois2300, cois2020 });
Course cois3400 = new Course(3400, "COIS-3400H: Database Management Systems", new List<Course>(), new List<Course> { cois1020 });
Course cois3030 = new Course(3030, "COIS-3030H: Software Specification and Development", new List<Course>(), new List<Course> { cois2240 });
Course cois3040 = new Course(3040, "COIS-3040H: Advanced Software Architecture", new List<Course>(), new List<Course> { cois2240 });
Course cois3320 = new Course(3320, "COIS-3320H: Operating Systems", new List<Course>(), new List<Course> { cois2300 });
Course cois3510 = new Course(3510, "COIS-3510H: Data Visualization", new List<Course>(), new List<Course> { cois1020 });
Course cois3850 = new Course(3850, "COIS-3850H: Fundamentals of Project Management", new List<Course>(), new List<Course> { cois1020 });
graph.AddVertex(cois3020);
graph.AddVertex(cois3380);
graph.AddVertex(cois3400);
graph.AddVertex(cois3030);
graph.AddVertex(cois3040);
graph.AddVertex(cois3320);
graph.AddVertex(cois3510);
graph.AddVertex(cois3850);

// Year 4
Course cois4000 = new Course(4000, "COIS-4000Y: Software Engineering Project", new List<Course> { cois3850 }, new List<Course> { cois3040, cois2430, cois3850 });
Course cois4050 = new Course(4050, "COIS-4050H: Advanced Algorithms", new List<Course>(), new List<Course> { cois3020 });
Course cois4310 = new Course(4310, "COIS-4310H: Computer Networks", new List<Course>(), new List<Course> { cois3320 });
Course cois4350 = new Course(4350, "COIS-4350H: High Performance Computing", new List<Course>(), new List<Course> { cois3320 });
Course cois4370 = new Course(4370, "COIS-4370H: Computer and Information Security", new List<Course>(), new List<Course> { cois3380 });
Course cois4400 = new Course(4400, "COIS-4400H: Data Mining", new List<Course>(), new List<Course> { cois2020 });
Course cois4450 = new Course(4450, "COIS-4450H: Big Data", new List<Course>(), new List<Course> { cois3380, cois3510 });
Course cois4550 = new Course(4550, "COIS-4550H: Artificial Intelligence", new List<Course>(), new List<Course> { cois2020, math2600 });
graph.AddVertex(cois4000);
graph.AddVertex(cois4050);
graph.AddVertex(cois4310);
graph.AddVertex(cois4350);
graph.AddVertex(cois4370);
graph.AddVertex(cois4400);
graph.AddVertex(cois4450);
graph.AddVertex(cois4550);

// Random courses
Course scifi = new Course(1000, "SCIFI-1000H: Science Fiction", new List<Course>(), new List<Course>(), true);
Course scifi2 = new Course(1001, "SCIFI-1001H: Science Fiction 2", new List<Course>(), new List<Course> { scifi });
Course scifi3 = new Course(1002, "SCIFI-1002H: Science Fiction 3", new List<Course>(), new List<Course> { scifi2 });
graph.AddVertex(scifi);
graph.AddVertex(scifi2);
graph.AddVertex(scifi3);

// Sitcom courses
Course sitcom = new Course(1003, "SITCOM-1003H: Sitcom", new List<Course>(), new List<Course>(), true);
Course sitcom2 = new Course(1004, "SITCOM-1004H: Sitcom 2", new List<Course>(), new List<Course> { sitcom });
Course sitcom3 = new Course(1005, "SITCOM-1005H: Sitcom 3", new List<Course>(), new List<Course> { sitcom2 });
graph.AddVertex(sitcom);
graph.AddVertex(sitcom2);
graph.AddVertex(sitcom3);

// Unrelated courses;
Course unrelated = new Course(1007, "UNRELATED-1003H: Unrelated", new List<Course>(), new List<Course>());
Course unrelated2 = new Course(1008, "UNRELATED-1004H: Unrelated 2", new List<Course>(), new List<Course>());
Course unrelated3 = new Course(1009, "UNRELATED-1005H: Unrelated 3", new List<Course>(), new List<Course>());
graph.AddVertex(unrelated);
graph.AddVertex(unrelated2);
graph.AddVertex(unrelated3);

List<Course> allRequiredCourses = new List<Course> {
  cois1010, math1350, math1110, math1110, cois2020, cois2240, math2600, cois4000, cois4550
};
// add phantom root node for the degree
Course degree = new Course(0, "Computer Science", new List<Course>(), allRequiredCourses, true);
graph.AddVertex(degree);

// print out the t_min and t_global_max per vertex
graph.Schedule(5, 2, degree);

graph.WriteToFile("coursegraph.md");
