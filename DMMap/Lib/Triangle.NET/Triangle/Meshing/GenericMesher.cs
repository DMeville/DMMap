﻿
namespace DMMTriangleNet.Meshing
{
    using System;
    using System.Collections.Generic;
    using DMMTriangleNet.Geometry;
    using DMMTriangleNet.IO;
    using DMMTriangleNet.Meshing.Algorithm;

    /// <summary>
    /// Create meshes of point sets or polygons.
    /// </summary>
    public class GenericMesher : ITriangulator, IConstraintMesher, IQualityMesher
    {
        ITriangulator triangulator;

        public GenericMesher()
            : this(new Dwyer())
        {
        }

        public GenericMesher(ITriangulator triangulator)
        {
            this.triangulator = triangulator;
        }

        /// <inherit />
        public IMesh Triangulate(ICollection<Vertex> points)
        {
            return triangulator.Triangulate(points);
        }

        /// <inherit />
        public IMesh Triangulate(IPolygon polygon)
        {
            return Triangulate(polygon, null, null);
        }

        /// <inherit />
        public IMesh Triangulate(IPolygon polygon, ConstraintOptions options)
        {
            return Triangulate(polygon, options, null);
        }

        /// <inherit />
        public IMesh Triangulate(IPolygon polygon, QualityOptions quality)
        {
            return Triangulate(polygon, null, quality);
        }

        /// <inherit />
        public IMesh Triangulate(IPolygon polygon, ConstraintOptions options, QualityOptions quality)
        {
            var mesh = (Mesh)triangulator.Triangulate(polygon.Points);

            mesh.ApplyConstraints(polygon, options, quality);

            return mesh;
        }

        /// <summary>
        /// Generates a structured mesh with bounds [0, 0, width, height].
        /// </summary>
        /// <param name="width">Width of the mesh (must be > 0).</param>
        /// <param name="height">Height of the mesh (must be > 0).</param>
        /// <param name="nx">Number of segments in x direction.</param>
        /// <param name="ny">Number of segments in y direction.</param>
        /// <returns>Mesh</returns>
        public IMesh StructuredMesh(double width, double height, int nx, int ny)
        {
            if (width <= 0.0)
            {
                throw new ArgumentException("width");
            }

            if (height <= 0.0)
            {
                throw new ArgumentException("height");
            }

            return StructuredMesh(new Rectangle(0.0, 0.0, width, height), nx, ny);
        }

        /// <summary>
        /// Generates a structured mesh.
        /// </summary>
        /// <param name="bounds">Bounds of the mesh.</param>
        /// <param name="nx">Number of segments in x direction.</param>
        /// <param name="ny">Number of segments in y direction.</param>
        /// <returns>Mesh</returns>
        public IMesh StructuredMesh(Rectangle bounds, int nx, int ny)
        {
            var polygon = new Polygon((nx + 1) * (ny + 1));

            double x, y, dx, dy, left, bottom;

            dx = bounds.Width / nx;
            dy = bounds.Height / ny;

            left = bounds.Left;
            bottom = bounds.Bottom;

            int i, j, k, l, n;

            // Add vertices.
            var points = polygon.Points;

            for (i = 0; i <= nx; i++)
            {
                x = left + i * dx;

                for (j = 0; j <= ny; j++)
                {
                    y = bottom + j * dy;

                    points.Add(new Vertex(x, y));
                }
            }

            n = 0;

            // Set vertex hash and id.
            foreach (var v in points)
            {
                v.hash = v.id = n++;
            }

            // Add boundary segments.
            var segments = polygon.Segments;

            segments.Capacity = 2 * (nx + ny);

            for (j = 0; j < ny; j++)
            {
                // Left
                segments.Add(new Edge(j, j + 1));

                // Right
                segments.Add(new Edge(nx * (ny + 1) + j, nx * (ny + 1) + (j + 1)));
            }

            for (i = 0; i < nx; i++)
            {
                // Bottom
                segments.Add(new Edge(i * (ny + 1), (i + 1) * (ny + 1)));

                // Top
                segments.Add(new Edge(i * (ny + 1) + nx, (i + 1) * (ny + 1) + nx));
            }

            // Add triangles.
            var triangles = new InputTriangle[2 * nx * ny];

            n = 0;

            for (i = 0; i < nx; i++)
            {
                for (j = 0; j < ny; j++)
                {
                    k = j + (ny + 1) * i;
                    l = j + (ny + 1) * (i + 1);

                    // Create 2 triangles in rectangle [k, l, l + 1, k + 1].

                    if ((i + j) % 2 == 0)
                    {
                        // Diagonal from bottom left to top right.
                        triangles[n++] = new InputTriangle(k, l, l + 1);
                        triangles[n++] = new InputTriangle(k, l + 1, k + 1);
                    }
                    else
                    {
                        // Diagonal from top left to bottom right.
                        triangles[n++] = new InputTriangle(k, l, k + 1);
                        triangles[n++] = new InputTriangle(l, l + 1, k + 1);
                    }
                }
            }

            return Converter.ToMesh(polygon, triangles);
        }
    }
}
