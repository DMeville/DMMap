﻿// -----------------------------------------------------------------------
// <copyright file="BoundedVoronoi.cs">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace DMMTriangleNet.Voronoi
{
    using System.Collections.Generic;
    using DMMTriangleNet.Geometry;
    using DMMTriangleNet.Topology.DCEL;

    using HVertex = DMMTriangleNet.Topology.DCEL.Vertex;
    using TVertex = DMMTriangleNet.Geometry.Vertex;

    public class BoundedVoronoi : VoronoiBase
    {
        int offset;

        public BoundedVoronoi(Mesh mesh)
            : base(mesh, true)
        {
            // We explicitly told the base constructor to call the Generate method, so
            // at this point the basic Voronoi diagram is already created.
            offset = base.vertices.Count;

            // Each vertex of the hull will be part of a Voronoi cell.
            base.vertices.Capacity = offset + mesh.hullsize;

            // Create bounded Voronoi diagram.
            PostProcess();

            ResolveBoundaryEdges();
        }

        /// <summary>
        /// Computes edge intersections with mesh boundary edges.
        /// </summary>
        private void PostProcess()
        {
            var infEdges = new List<HalfEdge>();

            // TODO: save the half-infinite boundary edge in base class
            // so we don't have to process the complete list here.
            foreach (var edge in base.edges)
            {
                if (edge.next == null)
                {
                    infEdges.Add(edge);
                }
            }

            foreach (var edge in infEdges)
            {
                var v1 = (TVertex)edge.face.generator;
                var v2 = (TVertex)edge.twin.face.generator;

                double dir = RobustPredicates.CounterClockwise(v1, v2, edge.origin);

                if (dir <= 0)
                {
                    HandleCase1(edge, v1, v2);
                }
                else
                {
                    HandleCase2(edge, v1, v2);
                }
            }
        }

        /// <summary>
        /// Case 1: edge origin lies inside the domain.
        /// </summary>
        private void HandleCase1(HalfEdge edge, TVertex v1, TVertex v2)
        {
            //int mark = GetBoundaryMark(v1);

            // The infinite vertex.
            var v = (Point)edge.twin.origin;

            // The half-edge is the bisector of v1 and v2, so the projection onto the
            // boundary segment is actually its midpoint.
            v.x = (v1.x + v2.x) / 2.0;
            v.y = (v1.y + v2.y) / 2.0;

            // Close the cell connected to edge.
            var gen = new HVertex(v1.x, v1.y);

            var h1 = new HalfEdge(edge.twin.origin, edge.face);
            var h2 = new HalfEdge(gen, edge.face);

            edge.next = h1;
            h1.next = h2;
            h2.next = edge.face.edge;

            gen.leaving = h2;

            // Let the face edge point to the edge leaving at generator.
            edge.face.edge = h2;

            base.edges.Add(h1);
            base.edges.Add(h2);

            int count = base.edges.Count;

            h1.id = count;
            h2.id = count + 1;

            gen.id = offset++;
            base.vertices.Add(gen);
        }

        /// <summary>
        /// Case 1: edge origin lies outside the domain.
        /// </summary>
        private void HandleCase2(HalfEdge edge, TVertex v1, TVertex v2)
        {
            // The vertices of the infinite edge.
            var p1 = (Point)edge.origin;
            var p2 = (Point)edge.twin.origin;

            // The two edges leaving p1, pointing into the mesh.
            var e1 = edge.twin.next;
            var e2 = e1.twin.next;

            // Find the two intersections with boundary edge.
            IntersectSegments(v1, v2, e1.origin, e1.twin.origin, ref p2);
            IntersectSegments(v1, v2, e2.origin, e2.twin.origin, ref p1);

            // The infinite edge will now lie on the boundary. Update pointers:
            e1.twin.next = edge.twin;
            edge.twin.next = e2;
            edge.twin.face = e2.face;

            e1.origin = edge.twin.origin;

            edge.twin.twin = null;
            edge.twin = null;

            // Close the cell.
            var gen = new HVertex(v1.x, v1.y);
            var he = new HalfEdge(gen, edge.face);

            edge.next = he;
            he.next = edge.face.edge;

            // Let the face edge point to the edge leaving at generator.
            edge.face.edge = he;

            base.edges.Add(he);

            he.id = base.edges.Count;

            gen.id = offset++;
            base.vertices.Add(gen);
        }

        /*
        private int GetBoundaryMark(Vertex v)
        {
            Otri tri = default(Otri);
            Otri next = default(Otri);
            Osub seg = default(Osub);

            // Get triangle connected to generator.
            v.tri.Copy(ref tri);
            v.tri.Copy(ref next);

            // Find boundary triangle.
            while (next.triangle.id != -1)
            {
                next.Copy(ref tri);
                next.OnextSelf();
            }

            // Find edge dual to current half-edge.
            tri.LnextSelf();
            tri.LnextSelf();

            tri.SegPivot(ref seg);

            return seg.seg.boundary;
        }
        //*/

        /// <summary>
        /// Compute intersection of two segments.
        /// </summary>
        /// <param name="p0">Segment 1 start point.</param>
        /// <param name="p1">Segment 1 end point.</param>
        /// <param name="q0">Segment 2 start point.</param>
        /// <param name="q1">Segment 2 end point.</param>
        /// <param name="i0">The intersection point.</param>
        /// <remarks>
        /// This is a special case of segment intersection. Since the calling algorithm assures
        /// that a valid intersection exists, there's no need to check for any special cases.
        /// </remarks>
        private static void IntersectSegments(Point p0, Point p1, Point q0, Point q1, ref Point i0)
        {
            double ux = p1.x - p0.x;
            double uy = p1.y - p0.y;
            double vx = q1.x - q0.x;
            double vy = q1.y - q0.y;
            double wx = p0.x - q0.x;
            double wy = p0.y - q0.y;

            double d = (ux * vy - uy * vx);
            double s = (vx * wy - vy * wx) / d;

            // Intersection point
            i0.x = p0.X + s * ux;
            i0.y = p0.Y + s * uy;
        }

        protected override IEnumerable<IEdge> EnumerateEdges()
        {
            var edges = new List<IEdge>(this.edges.Count / 2);

            foreach (var edge in this.edges)
            {
                var twin = edge.twin;

                // Report edge only once.
                if (twin == null)
                {
                    edges.Add(new Edge(edge.origin.id, edge.next.origin.id));
                }
                else if (edge.id < twin.id)
                {
                    edges.Add(new Edge(edge.origin.id, twin.origin.id));
                }
            }

            return edges;
        }
    }
}
