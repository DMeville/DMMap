﻿
namespace DMMTriangleNet.Geometry
{
    using System;
    using System.Collections.Generic;
    using DMMTriangleNet.Topology;
    /// <summary>
    /// A polygon represented as a planar straight line graph.
    /// </summary>
    public class Polygon : IPolygon
    {
        List<Vertex> points;
        List<Point> holes;
        List<RegionPointer> regions;

        List<IEdge> segments;

        /// <inherit />
        public List<Vertex> Points
        {
            get { return points; }
        }

        /// <inherit />
        public List<Point> Holes
        {
            get { return holes; }
        }

        /// <inherit />
        public List<RegionPointer> Regions
        {
            get { return regions; }
        }

        /// <inherit />
        public List<IEdge> Segments
        {
            get { return segments; }
        }

        /// <inherit />
        public bool HasPointMarkers { get; set; }

        /// <inherit />
        public bool HasSegmentMarkers { get; set; }

        /// <inherit />
        public int Count
        {
            get { return points.Count; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon" /> class.
        /// </summary>
        public Polygon()
            : this(3, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon" /> class.
        /// </summary>
        /// <param name="capacity">The default capacity for the points list.</param>
        public Polygon(int capacity)
            : this(3, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon" /> class.
        /// </summary>
        /// <param name="capacity">The default capacity for the points list.</param>
        /// <param name="markers">Use point and segment markers.</param>
        public Polygon(int capacity, bool markers)
        {
            points = new List<Vertex>(capacity);
            holes = new List<Point>();
            regions = new List<RegionPointer>();

            segments = new List<IEdge>();

            HasPointMarkers = markers;
            HasSegmentMarkers = markers;
        }

        /// <inherit />
        public void AddContour(IEnumerable<Vertex> points, int marker = 0,
            bool hole = false, bool convex = false)
        {
            // Copy input to list.
            var contour = new List<Vertex>(points);

            int offset = this.points.Count;
            int count = contour.Count;

            // Check if first vertex equals last vertex.
            if (contour[0] == contour[count - 1])
            {
                count--;
                contour.RemoveAt(count);
            }

            // Add points to polygon.
            this.points.AddRange(contour);

            var centroid = new Point(0.0, 0.0);

            for (int i = 0; i < count; i++)
            {
                centroid.x += contour[i].x;
                centroid.y += contour[i].y;

                // Add segments to polygon.
                this.segments.Add(new Edge(offset + i, offset + ((i + 1) % count), marker));
            }

            if (hole)
            {
                if (convex)
                {
                    // If the hole is convex, use its centroid.
                    centroid.x /= count;
                    centroid.y /= count;

                    this.holes.Add(centroid);
                }
                else
                {
                    this.holes.Add(FindPointInPolygon(contour));
                }
            }
        }

        /// <inherit />
        public void AddContour(IEnumerable<Vertex> points, int marker, Point hole)
        {
            // Copy input to list.
            var contour = new List<Vertex>(points);

            int offset = this.points.Count;
            int count = contour.Count;

            // Check if first vertex equals last vertex.
            if (contour[0] == contour[count - 1])
            {
                count--;
                contour.RemoveAt(count);
            }

            // Add points to polygon.
            this.points.AddRange(contour);

            for (int i = 0; i < count; i++)
            {
                // Add segments to polygon.
                this.segments.Add(new Edge(offset + i, offset + ((i + 1) % count), marker));
            }

            // TODO: check if hole is actually inside contour?
            this.holes.Add(hole);
        }

        /// <inherit />
        public Rectangle Bounds()
        {
            Rectangle bounds = new Rectangle();
            bounds.Expand((IEnumerable<Point>)this.points);
           

            return bounds;
        }

        /// <summary>
        /// Add a vertex to the polygon.
        /// </summary>
        public void Add(Vertex vertex)
        {
            this.points.Add(vertex);
        }

        /// <summary>
        /// Add a vertex to the polygon.
        /// </summary>
        public void Add(Vertex vertex, double[] attributes)
        {
            // TODO: check attibutes

            vertex.attributes = attributes;

            this.points.Add(vertex);
        }

        /// <summary>
        /// Add a segment to the polygon.
        /// </summary>
        public void Add(Edge edge)
        {
            this.segments.Add(edge);
        }

        private Point FindPointInPolygon(List<Vertex> contour)
        {
            var bounds = new Rectangle();
            bounds.Expand(contour);

            int length = contour.Count;
            int limit = 8;
            if (length == 3) {
                var midPoint = new Point();
                midPoint.x = (contour[0].x + contour[1].x + contour[2].x) * 1.0f / 3.0f;
                midPoint.y = (contour[0].y + contour[1].y + contour[2].y) * 1.0f / 3.0f;
                return midPoint;
            }

            var test = new Point();

            Point a, b; // Current edge.
            double cx, cy; // Center of current edge.
            double dx, dy; // Direction perpendicular to edge.

            for (int i = 0; i < length; i++)
            {
                a = contour[i];
                b = contour[(i + 1) % length];

                cx = (a.x + b.x) / 2;
                cy = (a.y + b.y) / 2;

                dx = (b.y - a.y) / 1.374;
                dy = (a.x - b.x) / 1.374;

                for (int j = 1; j <= limit; j++)
                {
                    // Search to the right of the segment.
                    test.x = cx + dx / j;
                    test.y = cy + dy / j;

                    if (bounds.Contains(test) && IsPointInPolygon(test, contour))
                    {
                        return test;
                    }

                    // Search on the other side of the segment.
                    test.x = cx - dx / j;
                    test.y = cy - dy / j;

                    if (bounds.Contains(test) && IsPointInPolygon(test, contour))
                    {
                        return test;
                    }
                }
            }

            throw new Exception();
        }

        /// <summary>
        /// Return true if the given point is inside the polygon, or false if it is not.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <param name="poly">The polygon (list of contour points).</param>
        /// <returns></returns>
        /// <remarks>
        /// WARNING: If the point is exactly on the edge of the polygon, then the function
        /// may return true or false.
        /// 
        /// See http://alienryderflex.com/polygon/
        /// </remarks>
        private bool IsPointInPolygon(Point point, List<Vertex> poly)
        {
            bool inside = false;

            double x = point.x;
            double y = point.y;

            int count = poly.Count;

            for (int i = 0, j = count - 1; i < count; i++)
            {
                if (((poly[i].y < y && poly[j].y >= y) || (poly[j].y < y && poly[i].y >= y))
                    && (poly[i].x <= x || poly[j].x <= x))
                {
                    inside ^= (poly[i].x + (y - poly[i].y) / (poly[j].y - poly[i].y) * (poly[j].x - poly[i].x) < x);
                }

                j = i;
            }

            return inside;
        }
    }
}
