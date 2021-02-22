﻿// -----------------------------------------------------------------------
// <copyright file="Mesh.cs">
// Original Triangle code by Jonathan Richard Shewchuk, http://www.cs.cmu.edu/~quake/triangle.html
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace DMMTriangleNet
{
    using System;
    using System.Collections.Generic;
    using DMMTriangleNet.Geometry;
    using DMMTriangleNet.Logging;
    using DMMTriangleNet.Meshing;
    using DMMTriangleNet.Meshing.Data;
    using DMMTriangleNet.Meshing.Iterators;
    using DMMTriangleNet.Tools;
    using DMMTriangleNet.Topology;

    /// <summary>
    /// Mesh data structure.
    /// </summary>
    public class Mesh : IMesh
    {
        #region Variables

        ILog<LogItem> logger;

        QualityMesher qualityMesher;

        // Stack that maintains a list of recently flipped triangles.
        Stack<Otri> flipstack;

        // TODO: Check if custom hashmap implementation could be faster.

        // Using hashsets for memory management should quite fast.
        internal Dictionary<int, Triangle> triangles;
        internal Dictionary<int, Segment> subsegs;
        internal Dictionary<int, Vertex> vertices;

        // Hash seeds (should belong to mesh instance)
        internal int hash_vtx = 0;
        internal int hash_seg = 0;
        internal int hash_tri = 0;

        internal List<Point> holes;
        internal List<RegionPointer> regions;

        // Other variables.
        internal Rectangle bounds; // x and y bounds.
        internal int invertices;     // Number of input vertices.
        internal int inelements;     // Number of input triangles.
        internal int insegments;     // Number of input segments.
        internal int undeads;        // Number of input vertices that don't appear in the mesh.
        internal int edges;          // Number of output edges.
        internal int mesh_dim;       // Dimension (ought to be 2).
        internal int nextras;        // Number of attributes per vertex.
        //internal int eextras;        // Number of attributes per triangle.
        internal int hullsize;       // Number of edges in convex hull.
        internal int steinerleft;    // Number of Steiner points not yet used.
        internal bool checksegments; // Are there segments in the triangulation yet?
        internal bool checkquality;  // Has quality triangulation begun yet?

        // Triangular bounding box vertices.
        internal Vertex infvertex1, infvertex2, infvertex3;

        internal TriangleLocator locator;

        // Controls the behavior of the mesh instance.
        internal Behavior behavior;

        // The current node numbering
        internal NodeNumbering numbering;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the mesh bounding box.
        /// </summary>
        public Rectangle Bounds
        {
            get { return this.bounds; }
        }

        /// <summary>
        /// Gets the mesh vertices.
        /// </summary>
        public ICollection<Vertex> Vertices
        {
            get { return this.vertices.Values; }
        }

        /// <summary>
        /// Gets the mesh holes.
        /// </summary>
        public IList<Point> Holes
        {
            get { return this.holes; }
        }

        /// <summary>
        /// Gets the mesh triangles.
        /// </summary>
        public ICollection<Triangle> Triangles
        {
            get { return this.triangles.Values; }
        }

        /// <summary>
        /// Gets the mesh segments.
        /// </summary>
        public ICollection<Segment> Segments
        {
            get { return this.subsegs.Values; }
        }

        /// <summary>
        /// Gets the mesh edges.
        /// </summary>
        public IEnumerable<Edge> Edges
        {
            get
            {
                var e = new EdgeIterator(this);
                while (e.MoveNext())
                {
                    yield return e.Current;
                }
            }
        }

        /// <summary>
        /// Gets the number of input vertices.
        /// </summary>
        public int NumberOfInputPoints { get { return invertices; } }

        /// <summary>
        /// Gets the number of mesh edges.
        /// </summary>
        public int NumberOfEdges { get { return this.edges; } }

        /// <summary>
        /// Indicates whether the input is a PSLG or a point set.
        /// </summary>
        public bool IsPolygon { get { return this.insegments > 0; } }

        /// <summary>
        /// Gets the current node numbering.
        /// </summary>
        public NodeNumbering CurrentNumbering
        {
            get { return numbering; }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh" /> class.
        /// </summary>
        public Mesh()
        {
            logger = Log.Instance;

            behavior = new Behavior();

            vertices = new Dictionary<int, Vertex>();
            triangles = new Dictionary<int, Triangle>();
            subsegs = new Dictionary<int, Segment>();

            flipstack = new Stack<Otri>();

            holes = new List<Point>();
            regions = new List<RegionPointer>();

            qualityMesher = new QualityMesher(this);

            locator = new TriangleLocator(this);

            RobustPredicates.ExactInit();
        }

        public void Refine(QualityOptions quality)
        {
            behavior.MinAngle = quality.MinimumAngle;
            behavior.MaxAngle = quality.MaximumAngle;

            behavior.MaxArea = quality.MaximumArea;
            // TODO: behavior.VarArea = quality.VariableArea;

            this.Refine();
        }

        /// <summary>
        /// Renumber vertex and triangle id's.
        /// </summary>
        public void Renumber()
        {
            this.Renumber(NodeNumbering.Linear);
        }

        /// <summary>
        /// Renumber vertex and triangle id's.
        /// </summary>
        public void Renumber(NodeNumbering num)
        {
            // Don't need to do anything if the nodes are already numbered.
            if (num == this.numbering)
            {
                return;
            }

            int id;

            if (num == NodeNumbering.Linear)
            {
                id = 0;
                foreach (var node in this.vertices.Values)
                {
                    node.id = id++;
                }
            }
            else if (num == NodeNumbering.CuthillMcKee)
            {
                CuthillMcKee rcm = new CuthillMcKee();
                int[] perm_inv = rcm.Renumber(this);

                // Permute the node indices.
                foreach (var node in this.vertices.Values)
                {
                    node.id = perm_inv[node.id];
                }
            }

            // Remember the current numbering.
            numbering = num;

            // Triangles will always be numbered from 0 to n-1
            id = 0;
            foreach (var item in this.triangles.Values)
            {
                item.id = id++;
            }
        }

        #region Misc

        /*
        /// <summary>
        /// Load a mesh from file (.node/poly and .ele).
        /// </summary>
        public void Load(string filename)
        {
            List<ITriangle> triangles;
            InputGeometry geometry;

            FileReader.Read(filename, out geometry, out triangles);

            if (geometry != null && triangles != null)
            {
                Load(geometry, triangles);
            }
        }

        /// <summary>
        /// Reconstructs a mesh from raw input data.
        /// </summary>
        public void Load(InputGeometry input, List<ITriangle> triangles)
        {
            if (input == null || triangles == null)
            {
                throw new ArgumentException("Invalid input (argument is null).");
            }

            // Clear all data structures / reset hash seeds
            this.ResetData();

            if (input.HasSegments)
            {
                behavior.Poly = true;

                this.holes.AddRange(input.Holes);
            }

            //if (input.EdgeMarkers != null)
            //{
            //    behavior.UseBoundaryMarkers = true;
            //}

            //if (input.TriangleAreas != null)
            //{
            //    behavior.VarArea = true;
            //}

            // TODO: remove
            if (!behavior.Poly)
            {
                // Be careful not to allocate space for element area constraints that
                // will never be assigned any value (other than the default -1.0).
                behavior.VarArea = false;

                // Be careful not to add an extra attribute to each element unless the
                // input supports it (PSLG in, but not refining a preexisting mesh).
                behavior.useRegions = false;
            }

            behavior.useRegions = input.Regions.Count > 0;

            TransferNodes(input.points);

            // Read and reconstruct a mesh.
            hullsize = DataReader.Reconstruct(this, input, triangles.ToArray());

            // Calculate the number of edges.
            edges = (3 * triangles.Count + hullsize) / 2;
        }
        */

        /// <summary>
        /// Triangulate given input data.
        /// </summary>
        /// <param name="input"></param>
        internal void ApplyConstraints(IPolygon input, ConstraintOptions options, QualityOptions quality)
        {
            behavior.Poly = input.Segments.Count > 0;

            // Copy constraint options
            if (options != null)
            {
                behavior.ConformingDelaunay = options.ConformingDelaunay;
                behavior.Convex = options.Convex;
                behavior.NoBisect = options.SegmentSplitting;

                if (behavior.ConformingDelaunay)
                {
                    behavior.Quality = true;
                }
            }

            // Copy quality options
            if (quality != null)
            {
                behavior.MinAngle = quality.MinimumAngle;
                behavior.MaxAngle = quality.MaximumAngle;
                behavior.MaxArea = quality.MaximumArea;
                behavior.UserTest = quality.UserTest;

                behavior.SteinerPoints = quality.SteinerPoints == 0 ? -1 : quality.SteinerPoints;

                behavior.Quality = true;
            }

            //if (input.EdgeMarkers != null)
            //{
            //    behavior.UseBoundaryMarkers = true;
            //}

            // TODO: remove
            if (!behavior.Poly)
            {
                // Be careful not to allocate space for element area constraints that
                // will never be assigned any value (other than the default -1.0).
                behavior.VarArea = false;
            }

            behavior.useRegions = input.Regions.Count > 0;

            steinerleft = behavior.SteinerPoints;

            // Ensure that no vertex can be mistaken for a triangular bounding
            // box vertex in insertvertex().
            infvertex1 = null;
            infvertex2 = null;
            infvertex3 = null;

            // Insert segments, carving holes.
            var mesher = new ConstraintMesher(this);

            if (behavior.useSegments)
            {
                // Segments will be introduced next.
                checksegments = true;

                // Insert PSLG segments and/or convex hull segments.
                mesher.FormSkeleton(input);
            }

            if (behavior.Poly && (triangles.Count > 0))
            {
                // Copy holes
                foreach (var item in input.Holes)
                {
                    holes.Add(item);
                }

                // Copy regions
                foreach (var item in input.Regions)
                {
                    regions.Add(item);
                }

                // Carve out holes and concavities.
                mesher.CarveHoles();
            }

            if (behavior.Quality && triangles.Count > 0)
            {
                // Enforce angle and area constraints.
                qualityMesher.EnforceQuality();
            }

            // Calculate the number of edges.
            edges = (3 * triangles.Count + hullsize) / 2;
        }

        /// <summary>
        /// Refines the current mesh.
        /// </summary>
        internal void Refine()
        {
            inelements = triangles.Count;
            invertices = vertices.Count;

            // TODO: Set all vertex types to input (i.e. NOT free)?

            if (behavior.Poly)
            {
                insegments = behavior.useSegments ? subsegs.Count : hullsize;
            }

            Reset();

            steinerleft = behavior.SteinerPoints;

            // Ensure that no vertex can be mistaken for a triangular bounding
            // box vertex in insertvertex().
            infvertex1 = infvertex2 = infvertex3 = null;

            if (behavior.useSegments)
            {
                checksegments = true;
            }

            if (triangles.Count > 0)
            {
                // Enforce angle and area constraints.
                qualityMesher.EnforceQuality();
            }

            // Calculate the number of edges.
            edges = (3 * triangles.Count + hullsize) / 2;
        }

        internal void CopyTo(Mesh target)
        {
            target.vertices = this.vertices;
            target.triangles = this.triangles;
            target.subsegs = this.subsegs;

            target.holes = this.holes;
            target.regions = this.regions;

            target.hash_vtx = this.hash_vtx;
            target.hash_seg = this.hash_seg;
            target.hash_tri = this.hash_tri;

            target.numbering = this.numbering;
            target.hullsize = this.hullsize;
            target.edges = this.edges;
        }

        /// <summary>
        /// Reset all the mesh data. This method will also wipe 
        /// out all mesh data.
        /// </summary>
        private void ResetData()
        {
            vertices.Clear();
            triangles.Clear();
            subsegs.Clear();

            holes.Clear();
            regions.Clear();

            this.hash_vtx = 0;
            this.hash_seg = 0;
            this.hash_tri = 0;

            flipstack.Clear();

            hullsize = 0;
            edges = 0;

            Reset();

            locator.Reset();
        }

        /// <summary>
        /// Reset the mesh triangulation state.
        /// </summary>
        private void Reset()
        {
            numbering = NodeNumbering.None;

            undeads = 0;               // No eliminated input vertices yet.
            checksegments = false;     // There are no segments in the triangulation yet.
            checkquality = false;      // The quality triangulation stage has not begun.

            Statistic.InCircleCount = 0;
            Statistic.CounterClockwiseCount = 0;
            Statistic.InCircleAdaptCount = 0;
            Statistic.CounterClockwiseAdaptCount = 0;
            Statistic.Orient3dCount = 0;
            Statistic.HyperbolaCount = 0;
            Statistic.CircleTopCount = 0;
            Statistic.CircumcenterCount = 0;
        }

        /// <summary>
        /// Read the vertices from memory.
        /// </summary>
        /// <param name="data">The input data.</param>
        internal void TransferNodes(ICollection<Vertex> points)
        {
            this.invertices = points.Count;
            this.mesh_dim = 2;
            this.bounds = new Rectangle();

            if (this.invertices < 3)
            {
                logger.Error("Input must have at least three input vertices.", "MeshReader.TransferNodes()");
                throw new Exception("Input must have at least three input vertices.");
            }

            bool first = true;

            foreach (Vertex p in points)
            {
                if (first)
                {
                    this.nextras = p.attributes == null ? 0 : p.attributes.Length;
                    first = false;
                }

                p.hash = p.id = this.hash_vtx++;

                this.vertices.Add(p.hash, p);
                this.bounds.Expand(p);
            }
        }

        /// <summary>
        /// Construct a mapping from vertices to triangles to improve the speed of 
        /// point location for segment insertion.
        /// </summary>
        /// <remarks>
        /// Traverses all the triangles, and provides each corner of each triangle
        /// with a pointer to that triangle. Of course, pointers will be overwritten
        /// by other pointers because (almost) each vertex is a corner of several
        /// triangles, but in the end every vertex will point to some triangle
        /// that contains it.
        /// </remarks>
        internal void MakeVertexMap()
        {
            Otri tri = default(Otri);
            Vertex triorg;

            foreach (var t in this.triangles.Values)
            {
                tri.triangle = t;
                // Check all three vertices of the triangle.
                for (tri.orient = 0; tri.orient < 3; tri.orient++)
                {
                    triorg = tri.Org();
                    triorg.tri = tri;
                }
            }
        }

        #endregion

        #region Factory

        /// <summary>
        /// Create a new triangle with orientation zero.
        /// </summary>
        /// <param name="newotri">Reference to the new triangle.</param>
        internal void MakeTriangle(ref Otri newotri)
        {
            Triangle tri = new Triangle();
            tri.hash = this.hash_tri++;
            tri.id = tri.hash;

            newotri.triangle = tri;
            newotri.orient = 0;

            triangles.Add(tri.hash, tri);
        }

        /// <summary>
        /// Create a new subsegment with orientation zero.
        /// </summary>
        /// <param name="newsubseg">Reference to the new subseg.</param>
        internal void MakeSegment(ref Osub newsubseg)
        {
            Segment seg = new Segment();
            seg.hash = this.hash_seg++;

            newsubseg.seg = seg;
            newsubseg.orient = 0;

            subsegs.Add(seg.hash, seg);
        }

        #endregion

        #region Manipulation

        /// <summary>
        /// Insert a vertex into a Delaunay triangulation, performing flips as necessary 
        /// to maintain the Delaunay property.
        /// </summary>
        /// <param name="newvertex">The point to be inserted.</param>
        /// <param name="searchtri">The triangle to start the search.</param>
        /// <param name="splitseg">Segment to split.</param>
        /// <param name="segmentflaws">Check for creation of encroached subsegments.</param>
        /// <param name="triflaws">Check for creation of bad quality triangles.</param>
        /// <returns>If a duplicate vertex or violated segment does not prevent the 
        /// vertex from being inserted, the return value will be ENCROACHINGVERTEX if 
        /// the vertex encroaches upon a subsegment (and checking is enabled), or
        /// SUCCESSFULVERTEX otherwise. In either case, 'searchtri' is set to a handle
        /// whose origin is the newly inserted vertex.</returns>
        /// <remarks>
        /// The point 'newvertex' is located. If 'searchtri.triangle' is not NULL,
        /// the search for the containing triangle begins from 'searchtri'.  If
        /// 'searchtri.triangle' is NULL, a full point location procedure is called.
        /// If 'insertvertex' is found inside a triangle, the triangle is split into
        /// three; if 'insertvertex' lies on an edge, the edge is split in two,
        /// thereby splitting the two adjacent triangles into four. Edge flips are
        /// used to restore the Delaunay property. If 'insertvertex' lies on an
        /// existing vertex, no action is taken, and the value DUPLICATEVERTEX is
        /// returned. On return, 'searchtri' is set to a handle whose origin is the
        /// existing vertex.
        ///
        /// InsertVertex() does not use flip() for reasons of speed; some
        /// information can be reused from edge flip to edge flip, like the
        /// locations of subsegments.
        /// 
        /// Param 'splitseg': Normally, the parameter 'splitseg' is set to NULL, 
        /// implying that no subsegment should be split. In this case, if 'insertvertex' 
        /// is found to lie on a segment, no action is taken, and the value VIOLATINGVERTEX 
        /// is returned. On return, 'searchtri' is set to a handle whose primary edge is the 
        /// violated subsegment.
        /// If the calling routine wishes to split a subsegment by inserting a vertex in it, 
        /// the parameter 'splitseg' should be that subsegment. In this case, 'searchtri' 
        /// MUST be the triangle handle reached by pivoting from that subsegment; no point 
        /// location is done.
        /// 
        /// Param 'segmentflaws': Flags that indicate whether or not there should
        /// be checks for the creation of encroached subsegments. If a newly inserted 
        /// vertex encroaches upon subsegments, these subsegments are added to the list 
        /// of subsegments to be split if 'segmentflaws' is set.
        /// 
        /// Param 'triflaws': Flags that indicate whether or not there should be
        /// checks for the creation of bad quality triangles. If bad triangles are 
        /// created, these are added to the queue if 'triflaws' is set.
        /// </remarks>
        internal InsertVertexResult InsertVertex(Vertex newvertex, ref Otri searchtri,
            ref Osub splitseg, bool segmentflaws, bool triflaws)
        {
            Otri horiz = default(Otri);
            Otri top = default(Otri);
            Otri botleft = default(Otri), botright = default(Otri);
            Otri topleft = default(Otri), topright = default(Otri);
            Otri newbotleft = default(Otri), newbotright = default(Otri);
            Otri newtopright = default(Otri);
            Otri botlcasing = default(Otri), botrcasing = default(Otri);
            Otri toplcasing = default(Otri), toprcasing = default(Otri);
            Otri testtri = default(Otri);
            Osub botlsubseg = default(Osub), botrsubseg = default(Osub);
            Osub toplsubseg = default(Osub), toprsubseg = default(Osub);
            Osub brokensubseg = default(Osub);
            Osub checksubseg = default(Osub);
            Osub rightsubseg = default(Osub);
            Osub newsubseg = default(Osub);
            BadSubseg encroached;
            //FlipStacker newflip;
            Vertex first;
            Vertex leftvertex, rightvertex, botvertex, topvertex, farvertex;
            Vertex segmentorg, segmentdest;
            int region;
            double area;
            InsertVertexResult success;
            LocateResult intersect;
            bool doflip;
            bool mirrorflag;
            bool enq;

            if (splitseg.seg == null)
            {
                // Find the location of the vertex to be inserted.  Check if a good
                // starting triangle has already been provided by the caller.
                if (searchtri.triangle.id == Triangle.EmptyID)
                {
                    // Find a boundary triangle.
                    horiz.triangle = Triangle.Empty;
                    horiz.orient = 0;
                    horiz.SymSelf();
                    // Search for a triangle containing 'newvertex'.
                    intersect = locator.Locate(newvertex, ref horiz);
                }
                else
                {
                    // Start searching from the triangle provided by the caller.
                    searchtri.Copy(ref horiz);
                    intersect = locator.PreciseLocate(newvertex, ref horiz, true);
                }
            }
            else
            {
                // The calling routine provides the subsegment in which
                // the vertex is inserted.
                searchtri.Copy(ref horiz);
                intersect = LocateResult.OnEdge;
            }

            if (intersect == LocateResult.OnVertex)
            {
                // There's already a vertex there.  Return in 'searchtri' a triangle
                // whose origin is the existing vertex.
                horiz.Copy(ref searchtri);
                locator.Update(ref horiz);
                return InsertVertexResult.Duplicate;
            }
            if ((intersect == LocateResult.OnEdge) || (intersect == LocateResult.Outside))
            {
                // The vertex falls on an edge or boundary.
                if (checksegments && (splitseg.seg == null))
                {
                    // Check whether the vertex falls on a subsegment.
                    horiz.SegPivot(ref brokensubseg);
                    if (brokensubseg.seg != Segment.Empty)
                    {
                        // The vertex falls on a subsegment, and hence will not be inserted.
                        if (segmentflaws)
                        {
                            enq = behavior.NoBisect != 2;
                            if (enq && (behavior.NoBisect == 1))
                            {
                                // This subsegment may be split only if it is an
                                // internal boundary.
                                horiz.Sym(ref testtri);
                                enq = testtri.triangle.id != Triangle.EmptyID;
                            }
                            if (enq)
                            {
                                // Add the subsegment to the list of encroached subsegments.
                                encroached = new BadSubseg();
                                encroached.subseg = brokensubseg;
                                encroached.org = brokensubseg.Org();
                                encroached.dest = brokensubseg.Dest();

                                qualityMesher.AddBadSubseg(encroached);
                            }
                        }
                        // Return a handle whose primary edge contains the vertex,
                        //   which has not been inserted.
                        horiz.Copy(ref searchtri);
                        locator.Update(ref horiz);
                        return InsertVertexResult.Violating;
                    }
                }

                // Insert the vertex on an edge, dividing one triangle into two (if
                // the edge lies on a boundary) or two triangles into four.
                horiz.Lprev(ref botright);
                botright.Sym(ref botrcasing);
                horiz.Sym(ref topright);
                // Is there a second triangle?  (Or does this edge lie on a boundary?)
                mirrorflag = topright.triangle.id != Triangle.EmptyID;
                if (mirrorflag)
                {
                    topright.LnextSelf();
                    topright.Sym(ref toprcasing);
                    MakeTriangle(ref newtopright);
                }
                else
                {
                    // Splitting a boundary edge increases the number of boundary edges.
                    hullsize++;
                }
                MakeTriangle(ref newbotright);

                // Set the vertices of changed and new triangles.
                rightvertex = horiz.Org();
                leftvertex = horiz.Dest();
                botvertex = horiz.Apex();
                newbotright.SetOrg(botvertex);
                newbotright.SetDest(rightvertex);
                newbotright.SetApex(newvertex);
                horiz.SetOrg(newvertex);

                // Set the region of a new triangle.
                newbotright.triangle.region = botright.triangle.region;

                if (behavior.VarArea)
                {
                    // Set the area constraint of a new triangle.
                    newbotright.triangle.area = botright.triangle.area;
                }

                if (mirrorflag)
                {
                    topvertex = topright.Dest();
                    newtopright.SetOrg(rightvertex);
                    newtopright.SetDest(topvertex);
                    newtopright.SetApex(newvertex);
                    topright.SetOrg(newvertex);

                    // Set the region of another new triangle.
                    newtopright.triangle.region = topright.triangle.region;

                    if (behavior.VarArea)
                    {
                        // Set the area constraint of another new triangle.
                        newtopright.triangle.area = topright.triangle.area;
                    }
                }

                // There may be subsegments that need to be bonded
                // to the new triangle(s).
                if (checksegments)
                {
                    botright.SegPivot(ref botrsubseg);

                    if (botrsubseg.seg != Segment.Empty)
                    {
                        botright.SegDissolve();
                        newbotright.SegBond(ref botrsubseg);
                    }

                    if (mirrorflag)
                    {
                        topright.SegPivot(ref toprsubseg);
                        if (toprsubseg.seg != Segment.Empty)
                        {
                            topright.SegDissolve();
                            newtopright.SegBond(ref toprsubseg);
                        }
                    }
                }

                // Bond the new triangle(s) to the surrounding triangles.
                newbotright.Bond(ref botrcasing);
                newbotright.LprevSelf();
                newbotright.Bond(ref botright);
                newbotright.LprevSelf();

                if (mirrorflag)
                {
                    newtopright.Bond(ref toprcasing);
                    newtopright.LnextSelf();
                    newtopright.Bond(ref topright);
                    newtopright.LnextSelf();
                    newtopright.Bond(ref newbotright);
                }

                if (splitseg.seg != null)
                {
                    // Split the subsegment into two.
                    splitseg.SetDest(newvertex);
                    segmentorg = splitseg.SegOrg();
                    segmentdest = splitseg.SegDest();
                    splitseg.SymSelf();
                    splitseg.Pivot(ref rightsubseg);
                    InsertSubseg(ref newbotright, splitseg.seg.boundary);
                    newbotright.SegPivot(ref newsubseg);
                    newsubseg.SetSegOrg(segmentorg);
                    newsubseg.SetSegDest(segmentdest);
                    splitseg.Bond(ref newsubseg);
                    newsubseg.SymSelf();
                    newsubseg.Bond(ref rightsubseg);
                    splitseg.SymSelf();

                    // Transfer the subsegment's boundary marker to the vertex if required.
                    if (newvertex.mark == 0)
                    {
                        newvertex.mark = splitseg.seg.boundary;
                    }
                }

                if (checkquality)
                {
                    flipstack.Clear();

                    flipstack.Push(default(Otri)); // Dummy flip (see UndoVertex)
                    flipstack.Push(horiz);
                }

                // Position 'horiz' on the first edge to check for
                // the Delaunay property.
                horiz.LnextSelf();
            }
            else
            {
                // Insert the vertex in a triangle, splitting it into three.
                horiz.Lnext(ref botleft);
                horiz.Lprev(ref botright);
                botleft.Sym(ref botlcasing);
                botright.Sym(ref botrcasing);
                MakeTriangle(ref newbotleft);
                MakeTriangle(ref newbotright);

                // Set the vertices of changed and new triangles.
                rightvertex = horiz.Org();
                leftvertex = horiz.Dest();
                botvertex = horiz.Apex();
                newbotleft.SetOrg(leftvertex);
                newbotleft.SetDest(botvertex);
                newbotleft.SetApex(newvertex);
                newbotright.SetOrg(botvertex);
                newbotright.SetDest(rightvertex);
                newbotright.SetApex(newvertex);
                horiz.SetApex(newvertex);

                // Set the region of the new triangles.
                newbotleft.triangle.region = horiz.triangle.region;
                newbotright.triangle.region = horiz.triangle.region;

                if (behavior.VarArea)
                {
                    // Set the area constraint of the new triangles.
                    area = horiz.triangle.area;
                    newbotleft.triangle.area = area;
                    newbotright.triangle.area = area;
                }

                // There may be subsegments that need to be bonded
                // to the new triangles.
                if (checksegments)
                {
                    botleft.SegPivot(ref botlsubseg);
                    if (botlsubseg.seg != Segment.Empty)
                    {
                        botleft.SegDissolve();
                        newbotleft.SegBond(ref botlsubseg);
                    }
                    botright.SegPivot(ref botrsubseg);
                    if (botrsubseg.seg != Segment.Empty)
                    {
                        botright.SegDissolve();
                        newbotright.SegBond(ref botrsubseg);
                    }
                }

                // Bond the new triangles to the surrounding triangles.
                newbotleft.Bond(ref botlcasing);
                newbotright.Bond(ref botrcasing);
                newbotleft.LnextSelf();
                newbotright.LprevSelf();
                newbotleft.Bond(ref newbotright);
                newbotleft.LnextSelf();
                botleft.Bond(ref newbotleft);
                newbotright.LprevSelf();
                botright.Bond(ref newbotright);

                if (checkquality)
                {
                    flipstack.Clear();
                    flipstack.Push(horiz);
                }
            }

            // The insertion is successful by default, unless an encroached
            // subsegment is found.
            success = InsertVertexResult.Successful;
            // Circle around the newly inserted vertex, checking each edge opposite it 
            // for the Delaunay property. Non-Delaunay edges are flipped. 'horiz' is 
            // always the edge being checked. 'first' marks where to stop circling.
            first = horiz.Org();
            rightvertex = first;
            leftvertex = horiz.Dest();
            // Circle until finished.
            while (true)
            {
                // By default, the edge will be flipped.
                doflip = true;

                if (checksegments)
                {
                    // Check for a subsegment, which cannot be flipped.
                    horiz.SegPivot(ref checksubseg);
                    if (checksubseg.seg != Segment.Empty)
                    {
                        // The edge is a subsegment and cannot be flipped.
                        doflip = false;

                        if (segmentflaws)
                        {
                            // Does the new vertex encroach upon this subsegment?
                            if (qualityMesher.CheckSeg4Encroach(ref checksubseg) > 0)
                            {
                                success = InsertVertexResult.Encroaching;
                            }
                        }
                    }
                }

                if (doflip)
                {
                    // Check if the edge is a boundary edge.
                    horiz.Sym(ref top);
                    if (top.triangle.id == Triangle.EmptyID)
                    {
                        // The edge is a boundary edge and cannot be flipped.
                        doflip = false;
                    }
                    else
                    {
                        // Find the vertex on the other side of the edge.
                        farvertex = top.Apex();
                        // In the incremental Delaunay triangulation algorithm, any of
                        // 'leftvertex', 'rightvertex', and 'farvertex' could be vertices
                        // of the triangular bounding box. These vertices must be
                        // treated as if they are infinitely distant, even though their
                        // "coordinates" are not.
                        if ((leftvertex == infvertex1) || (leftvertex == infvertex2) ||
                            (leftvertex == infvertex3))
                        {
                            // 'leftvertex' is infinitely distant. Check the convexity of
                            // the boundary of the triangulation. 'farvertex' might be
                            // infinite as well, but trust me, this same condition should
                            // be applied.
                            doflip = RobustPredicates.CounterClockwise(newvertex, rightvertex, farvertex) > 0.0;
                        }
                        else if ((rightvertex == infvertex1) ||
                                 (rightvertex == infvertex2) ||
                                 (rightvertex == infvertex3))
                        {
                            // 'rightvertex' is infinitely distant. Check the convexity of
                            // the boundary of the triangulation. 'farvertex' might be
                            // infinite as well, but trust me, this same condition should
                            // be applied.
                            doflip = RobustPredicates.CounterClockwise(farvertex, leftvertex, newvertex) > 0.0;
                        }
                        else if ((farvertex == infvertex1) ||
                                 (farvertex == infvertex2) ||
                                 (farvertex == infvertex3))
                        {
                            // 'farvertex' is infinitely distant and cannot be inside
                            // the circumcircle of the triangle 'horiz'.
                            doflip = false;
                        }
                        else
                        {
                            // Test whether the edge is locally Delaunay.
                            doflip = RobustPredicates.InCircle(leftvertex, newvertex, rightvertex, farvertex) > 0.0;
                        }
                        if (doflip)
                        {
                            // We made it! Flip the edge 'horiz' by rotating its containing
                            // quadrilateral (the two triangles adjacent to 'horiz').
                            // Identify the casing of the quadrilateral.
                            top.Lprev(ref topleft);
                            topleft.Sym(ref toplcasing);
                            top.Lnext(ref topright);
                            topright.Sym(ref toprcasing);
                            horiz.Lnext(ref botleft);
                            botleft.Sym(ref botlcasing);
                            horiz.Lprev(ref botright);
                            botright.Sym(ref botrcasing);
                            // Rotate the quadrilateral one-quarter turn counterclockwise.
                            topleft.Bond(ref botlcasing);
                            botleft.Bond(ref botrcasing);
                            botright.Bond(ref toprcasing);
                            topright.Bond(ref toplcasing);
                            if (checksegments)
                            {
                                // Check for subsegments and rebond them to the quadrilateral.
                                topleft.SegPivot(ref toplsubseg);
                                botleft.SegPivot(ref botlsubseg);
                                botright.SegPivot(ref botrsubseg);
                                topright.SegPivot(ref toprsubseg);
                                if (toplsubseg.seg == Segment.Empty)
                                {
                                    topright.SegDissolve();
                                }
                                else
                                {
                                    topright.SegBond(ref toplsubseg);
                                }
                                if (botlsubseg.seg == Segment.Empty)
                                {
                                    topleft.SegDissolve();
                                }
                                else
                                {
                                    topleft.SegBond(ref botlsubseg);
                                }
                                if (botrsubseg.seg == Segment.Empty)
                                {
                                    botleft.SegDissolve();
                                }
                                else
                                {
                                    botleft.SegBond(ref botrsubseg);
                                }
                                if (toprsubseg.seg == Segment.Empty)
                                {
                                    botright.SegDissolve();
                                }
                                else
                                {
                                    botright.SegBond(ref toprsubseg);
                                }
                            }
                            // New vertex assignments for the rotated quadrilateral.
                            horiz.SetOrg(farvertex);
                            horiz.SetDest(newvertex);
                            horiz.SetApex(rightvertex);
                            top.SetOrg(newvertex);
                            top.SetDest(farvertex);
                            top.SetApex(leftvertex);

                            // Assign region.
                            // TODO: check region ok (no Math.Min necessary)
                            region = Math.Min(top.triangle.region, horiz.triangle.region);
                            top.triangle.region = region;
                            horiz.triangle.region = region;

                            if (behavior.VarArea)
                            {
                                if ((top.triangle.area <= 0.0) || (horiz.triangle.area <= 0.0))
                                {
                                    area = -1.0;
                                }
                                else
                                {
                                    // Take the average of the two triangles' area constraints.
                                    // This prevents small area constraints from migrating a
                                    // long, long way from their original location due to flips.
                                    area = 0.5 * (top.triangle.area + horiz.triangle.area);
                                }

                                top.triangle.area = area;
                                horiz.triangle.area = area;
                            }

                            if (checkquality)
                            {
                                flipstack.Push(horiz);
                            }

                            // On the next iterations, consider the two edges that were exposed (this
                            // is, are now visible to the newly inserted vertex) by the edge flip.
                            horiz.LprevSelf();
                            leftvertex = farvertex;
                        }
                    }
                }
                if (!doflip)
                {
                    // The handle 'horiz' is accepted as locally Delaunay.
                    if (triflaws)
                    {
                        // Check the triangle 'horiz' for quality.
                        qualityMesher.TestTriangle(ref horiz);
                    }

                    // Look for the next edge around the newly inserted vertex.
                    horiz.LnextSelf();
                    horiz.Sym(ref testtri);
                    // Check for finishing a complete revolution about the new vertex, or
                    // falling outside of the triangulation. The latter will happen when
                    // a vertex is inserted at a boundary.
                    if ((leftvertex == first) || (testtri.triangle.id == Triangle.EmptyID))
                    {
                        // We're done. Return a triangle whose origin is the new vertex.
                        horiz.Lnext(ref searchtri);

                        Otri recenttri = default(Otri);
                        horiz.Lnext(ref recenttri);
                        locator.Update(ref recenttri);

                        return success;
                    }
                    // Finish finding the next edge around the newly inserted vertex.
                    testtri.Lnext(ref horiz);
                    rightvertex = leftvertex;
                    leftvertex = horiz.Dest();
                }
            }
        }

        /// <summary>
        /// Create a new subsegment and inserts it between two triangles. Its 
        /// vertices are properly initialized.
        /// </summary>
        /// <param name="tri">The new subsegment is inserted at the edge 
        /// described by this handle.</param>
        /// <param name="subsegmark">The marker 'subsegmark' is applied to the 
        /// subsegment and, if appropriate, its vertices.</param>
        internal void InsertSubseg(ref Otri tri, int subsegmark)
        {
            Otri oppotri = default(Otri);
            Osub newsubseg = default(Osub);
            Vertex triorg, tridest;

            triorg = tri.Org();
            tridest = tri.Dest();
            // Mark vertices if possible.
            if (triorg.mark == 0)
            {
                triorg.mark = subsegmark;
            }
            if (tridest.mark == 0)
            {
                tridest.mark = subsegmark;
            }
            // Check if there's already a subsegment here.
            tri.SegPivot(ref newsubseg);
            if (newsubseg.seg == Segment.Empty)
            {
                // Make new subsegment and initialize its vertices.
                MakeSegment(ref newsubseg);
                newsubseg.SetOrg(tridest);
                newsubseg.SetDest(triorg);
                newsubseg.SetSegOrg(tridest);
                newsubseg.SetSegDest(triorg);
                // Bond new subsegment to the two triangles it is sandwiched between.
                // Note that the facing triangle 'oppotri' might be equal to 'dummytri'
                // (outer space), but the new subsegment is bonded to it all the same.
                tri.SegBond(ref newsubseg);
                tri.Sym(ref oppotri);
                newsubseg.SymSelf();
                oppotri.SegBond(ref newsubseg);
                newsubseg.seg.boundary = subsegmark;
            }
            else if (newsubseg.seg.boundary == 0)
            {
                newsubseg.seg.boundary = subsegmark;
            }
        }

        /// <summary>
        /// Transform two triangles to two different triangles by flipping an edge 
        /// counterclockwise within a quadrilateral.
        /// </summary>
        /// <param name="flipedge">Handle to the edge that will be flipped.</param>
        /// <remarks>Imagine the original triangles, abc and bad, oriented so that the
        /// shared edge ab lies in a horizontal plane, with the vertex b on the left
        /// and the vertex a on the right. The vertex c lies below the edge, and
        /// the vertex d lies above the edge. The 'flipedge' handle holds the edge
        /// ab of triangle abc, and is directed left, from vertex a to vertex b.
        ///
        /// The triangles abc and bad are deleted and replaced by the triangles cdb
        /// and dca.  The triangles that represent abc and bad are NOT deallocated;
        /// they are reused for dca and cdb, respectively.  Hence, any handles that
        /// may have held the original triangles are still valid, although not
        /// directed as they were before.
        ///
        /// Upon completion of this routine, the 'flipedge' handle holds the edge
        /// dc of triangle dca, and is directed down, from vertex d to vertex c.
        /// (Hence, the two triangles have rotated counterclockwise.)
        ///
        /// WARNING:  This transformation is geometrically valid only if the
        /// quadrilateral adbc is convex.  Furthermore, this transformation is
        /// valid only if there is not a subsegment between the triangles abc and
        /// bad.  This routine does not check either of these preconditions, and
        /// it is the responsibility of the calling routine to ensure that they are
        /// met.  If they are not, the streets shall be filled with wailing and
        /// gnashing of teeth.
        /// 
        /// Terminology
        ///
        /// A "local transformation" replaces a small set of triangles with another
        /// set of triangles.  This may or may not involve inserting or deleting a
        /// vertex.
        ///
        /// The term "casing" is used to describe the set of triangles that are
        /// attached to the triangles being transformed, but are not transformed
        /// themselves.  Think of the casing as a fixed hollow structure inside
        /// which all the action happens.  A "casing" is only defined relative to
        /// a single transformation; each occurrence of a transformation will
        /// involve a different casing.
        /// </remarks>
        internal void Flip(ref Otri flipedge)
        {
            Otri botleft = default(Otri), botright = default(Otri);
            Otri topleft = default(Otri), topright = default(Otri);
            Otri top = default(Otri);
            Otri botlcasing = default(Otri), botrcasing = default(Otri);
            Otri toplcasing = default(Otri), toprcasing = default(Otri);
            Osub botlsubseg = default(Osub), botrsubseg = default(Osub);
            Osub toplsubseg = default(Osub), toprsubseg = default(Osub);
            Vertex leftvertex, rightvertex, botvertex;
            Vertex farvertex;

            // Identify the vertices of the quadrilateral.
            rightvertex = flipedge.Org();
            leftvertex = flipedge.Dest();
            botvertex = flipedge.Apex();
            flipedge.Sym(ref top);

            // SELF CHECK

            //if (top.triangle.id == Triangle.EmptyID)
            //{
            //    logger.Error("Attempt to flip on boundary.", "Mesh.Flip()");
            //    flipedge.LnextSelf();
            //    return;
            //}

            //if (checksegments)
            //{
            //    flipedge.SegPivot(ref toplsubseg);
            //    if (toplsubseg.ss != Segment.Empty)
            //    {
            //        logger.Error("Attempt to flip a segment.", "Mesh.Flip()");
            //        flipedge.LnextSelf();
            //        return;
            //    }
            //}

            farvertex = top.Apex();

            // Identify the casing of the quadrilateral.
            top.Lprev(ref topleft);
            topleft.Sym(ref toplcasing);
            top.Lnext(ref topright);
            topright.Sym(ref toprcasing);
            flipedge.Lnext(ref botleft);
            botleft.Sym(ref botlcasing);
            flipedge.Lprev(ref botright);
            botright.Sym(ref botrcasing);
            // Rotate the quadrilateral one-quarter turn counterclockwise.
            topleft.Bond(ref botlcasing);
            botleft.Bond(ref botrcasing);
            botright.Bond(ref toprcasing);
            topright.Bond(ref toplcasing);

            if (checksegments)
            {
                // Check for subsegments and rebond them to the quadrilateral.
                topleft.SegPivot(ref toplsubseg);
                botleft.SegPivot(ref botlsubseg);
                botright.SegPivot(ref botrsubseg);
                topright.SegPivot(ref toprsubseg);

                if (toplsubseg.seg == Segment.Empty)
                {
                    topright.SegDissolve();
                }
                else
                {
                    topright.SegBond(ref toplsubseg);
                }

                if (botlsubseg.seg == Segment.Empty)
                {
                    topleft.SegDissolve();
                }
                else
                {
                    topleft.SegBond(ref botlsubseg);
                }

                if (botrsubseg.seg == Segment.Empty)
                {
                    botleft.SegDissolve();
                }
                else
                {
                    botleft.SegBond(ref botrsubseg);
                }

                if (toprsubseg.seg == Segment.Empty)
                {
                    botright.SegDissolve();
                }
                else
                {
                    botright.SegBond(ref toprsubseg);
                }
            }

            // New vertex assignments for the rotated quadrilateral.
            flipedge.SetOrg(farvertex);
            flipedge.SetDest(botvertex);
            flipedge.SetApex(rightvertex);
            top.SetOrg(botvertex);
            top.SetDest(farvertex);
            top.SetApex(leftvertex);
        }

        /// <summary>
        /// Transform two triangles to two different triangles by flipping an edge 
        /// clockwise within a quadrilateral. Reverses the flip() operation so that 
        /// the data structures representing the triangles are back where they were 
        /// before the flip().
        /// </summary>
        /// <param name="flipedge"></param>
        /// <remarks>
        /// See above Flip() remarks for more information.
        ///
        /// Upon completion of this routine, the 'flipedge' handle holds the edge
        /// cd of triangle cdb, and is directed up, from vertex c to vertex d.
        /// (Hence, the two triangles have rotated clockwise.)
        /// </remarks>
        internal void Unflip(ref Otri flipedge)
        {
            Otri botleft = default(Otri), botright = default(Otri);
            Otri topleft = default(Otri), topright = default(Otri);
            Otri top = default(Otri);
            Otri botlcasing = default(Otri), botrcasing = default(Otri);
            Otri toplcasing = default(Otri), toprcasing = default(Otri);
            Osub botlsubseg = default(Osub), botrsubseg = default(Osub);
            Osub toplsubseg = default(Osub), toprsubseg = default(Osub);
            Vertex leftvertex, rightvertex, botvertex;
            Vertex farvertex;

            // Identify the vertices of the quadrilateral.
            rightvertex = flipedge.Org();
            leftvertex = flipedge.Dest();
            botvertex = flipedge.Apex();
            flipedge.Sym(ref top);

            farvertex = top.Apex();

            // Identify the casing of the quadrilateral.
            top.Lprev(ref topleft);
            topleft.Sym(ref toplcasing);
            top.Lnext(ref topright);
            topright.Sym(ref toprcasing);
            flipedge.Lnext(ref botleft);
            botleft.Sym(ref botlcasing);
            flipedge.Lprev(ref botright);
            botright.Sym(ref botrcasing);
            // Rotate the quadrilateral one-quarter turn clockwise.
            topleft.Bond(ref toprcasing);
            botleft.Bond(ref toplcasing);
            botright.Bond(ref botlcasing);
            topright.Bond(ref botrcasing);

            if (checksegments)
            {
                // Check for subsegments and rebond them to the quadrilateral.
                topleft.SegPivot(ref toplsubseg);
                botleft.SegPivot(ref botlsubseg);
                botright.SegPivot(ref botrsubseg);
                topright.SegPivot(ref toprsubseg);
                if (toplsubseg.seg == Segment.Empty)
                {
                    botleft.SegDissolve();
                }
                else
                {
                    botleft.SegBond(ref toplsubseg);
                }
                if (botlsubseg.seg == Segment.Empty)
                {
                    botright.SegDissolve();
                }
                else
                {
                    botright.SegBond(ref botlsubseg);
                }
                if (botrsubseg.seg == Segment.Empty)
                {
                    topright.SegDissolve();
                }
                else
                {
                    topright.SegBond(ref botrsubseg);
                }
                if (toprsubseg.seg == Segment.Empty)
                {
                    topleft.SegDissolve();
                }
                else
                {
                    topleft.SegBond(ref toprsubseg);
                }
            }

            // New vertex assignments for the rotated quadrilateral.
            flipedge.SetOrg(botvertex);
            flipedge.SetDest(farvertex);
            flipedge.SetApex(leftvertex);
            top.SetOrg(farvertex);
            top.SetDest(botvertex);
            top.SetApex(rightvertex);
        }

        /// <summary>
        /// Find the Delaunay triangulation of a polygon that has a certain "nice" shape. 
        /// This includes the polygons that result from deletion of a vertex or insertion 
        /// of a segment.
        /// </summary>
        /// <param name="firstedge">The primary edge of the first triangle.</param>
        /// <param name="lastedge">The primary edge of the last triangle.</param>
        /// <param name="edgecount">The number of sides of the polygon, including its 
        /// base.</param>
        /// <param name="doflip">A flag, wether to perform the last flip.</param>
        /// <param name="triflaws">A flag that determines whether the new triangles should 
        /// be tested for quality, and enqueued if they are bad.</param>
        /// <remarks>
        //  This is a conceptually difficult routine. The starting assumption is
        //  that we have a polygon with n sides. n - 1 of these sides are currently
        //  represented as edges in the mesh. One side, called the "base", need not
        //  be.
        //
        //  Inside the polygon is a structure I call a "fan", consisting of n - 1
        //  triangles that share a common origin. For each of these triangles, the
        //  edge opposite the origin is one of the sides of the polygon. The
        //  primary edge of each triangle is the edge directed from the origin to
        //  the destination; note that this is not the same edge that is a side of
        //  the polygon. 'firstedge' is the primary edge of the first triangle.
        //  From there, the triangles follow in counterclockwise order about the
        //  polygon, until 'lastedge', the primary edge of the last triangle.
        //  'firstedge' and 'lastedge' are probably connected to other triangles
        //  beyond the extremes of the fan, but their identity is not important, as
        //  long as the fan remains connected to them.
        //
        //  Imagine the polygon oriented so that its base is at the bottom.  This
        //  puts 'firstedge' on the far right, and 'lastedge' on the far left.
        //  The right vertex of the base is the destination of 'firstedge', and the
        //  left vertex of the base is the apex of 'lastedge'.
        //
        //  The challenge now is to find the right sequence of edge flips to
        //  transform the fan into a Delaunay triangulation of the polygon.  Each
        //  edge flip effectively removes one triangle from the fan, committing it
        //  to the polygon.  The resulting polygon has one fewer edge. If 'doflip'
        //  is set, the final flip will be performed, resulting in a fan of one
        //  (useless?) triangle. If 'doflip' is not set, the final flip is not
        //  performed, resulting in a fan of two triangles, and an unfinished
        //  triangular polygon that is not yet filled out with a single triangle.
        //  On completion of the routine, 'lastedge' is the last remaining triangle,
        //  or the leftmost of the last two.
        //
        //  Although the flips are performed in the order described above, the
        //  decisions about what flips to perform are made in precisely the reverse
        //  order. The recursive triangulatepolygon() procedure makes a decision,
        //  uses up to two recursive calls to triangulate the "subproblems"
        //  (polygons with fewer edges), and then performs an edge flip.
        //
        //  The "decision" it makes is which vertex of the polygon should be
        //  connected to the base. This decision is made by testing every possible
        //  vertex.  Once the best vertex is found, the two edges that connect this
        //  vertex to the base become the bases for two smaller polygons. These
        //  are triangulated recursively. Unfortunately, this approach can take
        //  O(n^2) time not only in the worst case, but in many common cases. It's
        //  rarely a big deal for vertex deletion, where n is rarely larger than
        //  ten, but it could be a big deal for segment insertion, especially if
        //  there's a lot of long segments that each cut many triangles. I ought to
        //  code a faster algorithm some day.
        /// </remarks>
        private void TriangulatePolygon(Otri firstedge, Otri lastedge,
                                int edgecount, bool doflip, bool triflaws)
        {
            Otri testtri = default(Otri);
            Otri besttri = default(Otri);
            Otri tempedge = default(Otri);
            Vertex leftbasevertex, rightbasevertex;
            Vertex testvertex;
            Vertex bestvertex;

            int bestnumber = 1;

            // Identify the base vertices.
            leftbasevertex = lastedge.Apex();
            rightbasevertex = firstedge.Dest();

            // Find the best vertex to connect the base to.
            firstedge.Onext(ref besttri);
            bestvertex = besttri.Dest();
            besttri.Copy(ref testtri);

            for (int i = 2; i <= edgecount - 2; i++)
            {
                testtri.OnextSelf();
                testvertex = testtri.Dest();
                // Is this a better vertex?
                if (RobustPredicates.InCircle(leftbasevertex, rightbasevertex, bestvertex, testvertex) > 0.0)
                {
                    testtri.Copy(ref besttri);
                    bestvertex = testvertex;
                    bestnumber = i;
                }
            }

            if (bestnumber > 1)
            {
                // Recursively triangulate the smaller polygon on the right.
                besttri.Oprev(ref tempedge);
                TriangulatePolygon(firstedge, tempedge, bestnumber + 1, true, triflaws);
            }

            if (bestnumber < edgecount - 2)
            {
                // Recursively triangulate the smaller polygon on the left.
                besttri.Sym(ref tempedge);
                TriangulatePolygon(besttri, lastedge, edgecount - bestnumber, true, triflaws);
                // Find 'besttri' again; it may have been lost to edge flips.
                tempedge.Sym(ref besttri);
            }

            if (doflip)
            {
                // Do one final edge flip.
                Flip(ref besttri);
                if (triflaws)
                {
                    // Check the quality of the newly committed triangle.
                    besttri.Sym(ref testtri);
                    qualityMesher.TestTriangle(ref testtri);
                }
            }
            // Return the base triangle.
            besttri.Copy(ref lastedge);
        }

        /// <summary>
        /// Delete a vertex from a Delaunay triangulation, ensuring that the 
        /// triangulation remains Delaunay.
        /// </summary>
        /// <param name="deltri"></param>
        /// <remarks>The origin of 'deltri' is deleted. The union of the triangles 
        /// adjacent to this vertex is a polygon, for which the Delaunay triangulation 
        /// is found. Two triangles are removed from the mesh.
        ///
        /// Only interior vertices that do not lie on segments or boundaries 
        /// may be deleted.
        /// </remarks>
        internal void DeleteVertex(ref Otri deltri)
        {
            Otri countingtri = default(Otri);
            Otri firstedge = default(Otri), lastedge = default(Otri);
            Otri deltriright = default(Otri);
            Otri lefttri = default(Otri), righttri = default(Otri);
            Otri leftcasing = default(Otri), rightcasing = default(Otri);
            Osub leftsubseg = default(Osub), rightsubseg = default(Osub);
            Vertex delvertex;
            Vertex neworg;
            int edgecount;

            delvertex = deltri.Org();

            VertexDealloc(delvertex);

            // Count the degree of the vertex being deleted.
            deltri.Onext(ref countingtri);
            edgecount = 1;
            while (!deltri.Equal(countingtri))
            {
                edgecount++;
                countingtri.OnextSelf();
            }

            if (edgecount > 3)
            {
                // Triangulate the polygon defined by the union of all triangles
                // adjacent to the vertex being deleted.  Check the quality of
                // the resulting triangles.
                deltri.Onext(ref firstedge);
                deltri.Oprev(ref lastedge);
                TriangulatePolygon(firstedge, lastedge, edgecount, false, behavior.NoBisect == 0);
            }
            // Splice out two triangles.
            deltri.Lprev(ref deltriright);
            deltri.Dnext(ref lefttri);
            lefttri.Sym(ref leftcasing);
            deltriright.Oprev(ref righttri);
            righttri.Sym(ref rightcasing);
            deltri.Bond(ref leftcasing);
            deltriright.Bond(ref rightcasing);
            lefttri.SegPivot(ref leftsubseg);
            if (leftsubseg.seg != Segment.Empty)
            {
                deltri.SegBond(ref leftsubseg);
            }
            righttri.SegPivot(ref rightsubseg);
            if (rightsubseg.seg != Segment.Empty)
            {
                deltriright.SegBond(ref rightsubseg);
            }

            // Set the new origin of 'deltri' and check its quality.
            neworg = lefttri.Org();
            deltri.SetOrg(neworg);
            if (behavior.NoBisect == 0)
            {
                qualityMesher.TestTriangle(ref deltri);
            }

            // Delete the two spliced-out triangles.
            TriangleDealloc(lefttri.triangle);
            TriangleDealloc(righttri.triangle);
        }

        /// <summary>
        /// Undo the most recent vertex insertion.
        /// </summary>
        /// <remarks>
        /// Walks through the list of transformations (flips and a vertex insertion)
        /// in the reverse of the order in which they were done, and undoes them.
        /// The inserted vertex is removed from the triangulation and deallocated.
        /// Two triangles (possibly just one) are also deallocated.
        /// </remarks>
        internal void UndoVertex()
        {
            Otri fliptri;

            Otri botleft = default(Otri), botright = default(Otri), topright = default(Otri);
            Otri botlcasing = default(Otri), botrcasing = default(Otri), toprcasing = default(Otri);
            Otri gluetri = default(Otri);
            Osub botlsubseg = default(Osub), botrsubseg = default(Osub), toprsubseg = default(Osub);
            Vertex botvertex, rightvertex;

            // Walk through the list of transformations (flips and a vertex insertion)
            // in the reverse of the order in which they were done, and undo them.
            while (flipstack.Count > 0)
            {
                // Find a triangle involved in the last unreversed transformation.
                fliptri = flipstack.Pop();

                // We are reversing one of three transformations:  a trisection of one
                // triangle into three (by inserting a vertex in the triangle), a
                // bisection of two triangles into four (by inserting a vertex in an
                // edge), or an edge flip.
                if (flipstack.Count == 0)
                {
                    // Restore a triangle that was split into three triangles,
                    // so it is again one triangle.
                    fliptri.Dprev(ref botleft);
                    botleft.LnextSelf();
                    fliptri.Onext(ref botright);
                    botright.LprevSelf();
                    botleft.Sym(ref botlcasing);
                    botright.Sym(ref botrcasing);
                    botvertex = botleft.Dest();

                    fliptri.SetApex(botvertex);
                    fliptri.LnextSelf();
                    fliptri.Bond(ref botlcasing);
                    botleft.SegPivot(ref botlsubseg);
                    fliptri.SegBond(ref botlsubseg);
                    fliptri.LnextSelf();
                    fliptri.Bond(ref botrcasing);
                    botright.SegPivot(ref botrsubseg);
                    fliptri.SegBond(ref botrsubseg);

                    // Delete the two spliced-out triangles.
                    TriangleDealloc(botleft.triangle);
                    TriangleDealloc(botright.triangle);
                }
                else if (flipstack.Peek().triangle == null) // Dummy flip
                {
                    // Restore two triangles that were split into four triangles,
                    // so they are again two triangles.
                    fliptri.Lprev(ref gluetri);
                    gluetri.Sym(ref botright);
                    botright.LnextSelf();
                    botright.Sym(ref botrcasing);
                    rightvertex = botright.Dest();

                    fliptri.SetOrg(rightvertex);
                    gluetri.Bond(ref botrcasing);
                    botright.SegPivot(ref botrsubseg);
                    gluetri.SegBond(ref botrsubseg);

                    // Delete the spliced-out triangle.
                    TriangleDealloc(botright.triangle);

                    fliptri.Sym(ref gluetri);
                    if (gluetri.triangle.id != Triangle.EmptyID)
                    {
                        gluetri.LnextSelf();
                        gluetri.Dnext(ref topright);
                        topright.Sym(ref toprcasing);

                        gluetri.SetOrg(rightvertex);
                        gluetri.Bond(ref toprcasing);
                        topright.SegPivot(ref toprsubseg);
                        gluetri.SegBond(ref toprsubseg);

                        // Delete the spliced-out triangle.
                        TriangleDealloc(topright.triangle);
                    }

                    flipstack.Clear();
                }
                else
                {
                    // Undo an edge flip.
                    Unflip(ref fliptri);
                }
            }
        }

        #endregion

        #region Dealloc

        /// <summary>
        /// Deallocate space for a triangle, marking it dead.
        /// </summary>
        /// <param name="dyingtriangle"></param>
        internal void TriangleDealloc(Triangle dyingtriangle)
        {
            // Mark the triangle as dead. This makes it possible to detect dead 
            // triangles when traversing the list of all triangles.
            Otri.Kill(dyingtriangle);
            triangles.Remove(dyingtriangle.hash);
        }

        /// <summary>
        /// Deallocate space for a vertex, marking it dead.
        /// </summary>
        /// <param name="dyingvertex"></param>
        internal void VertexDealloc(Vertex dyingvertex)
        {
            // Mark the vertex as dead. This makes it possible to detect dead 
            // vertices when traversing the list of all vertices.
            dyingvertex.type = VertexType.DeadVertex;
            vertices.Remove(dyingvertex.hash);
        }

        /// <summary>
        /// Deallocate space for a subsegment, marking it dead.
        /// </summary>
        /// <param name="dyingsubseg"></param>
        internal void SubsegDealloc(Segment dyingsubseg)
        {
            // Mark the subsegment as dead. This makes it possible to detect dead 
            // subsegments when traversing the list of all subsegments.
            Osub.Kill(dyingsubseg);
            subsegs.Remove(dyingsubseg.hash);
        }

        #endregion
    }
}
