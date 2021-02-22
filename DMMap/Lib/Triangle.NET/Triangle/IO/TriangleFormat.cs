﻿// -----------------------------------------------------------------------
// <copyright file="TriangleFormat.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace DMMTriangleNet.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using DMMTriangleNet.Geometry;
    using DMMTriangleNet.Meshing;

    /// <summary>
    /// Implements geometry and mesh file formats of the the original Triangle code.
    /// </summary>
    public class TriangleFormat : IPolygonFormat, IMeshFormat
    {
        public bool IsSupported(string file)
        {
            string ext = Path.GetExtension(file);

            if (ext == ".node" || ext == ".poly" || ext == ".ele")
            {
                return true;
            }

            return false;
        }

        public IMesh Import(string filename)
        {
            string ext = Path.GetExtension(filename);

            if (ext == ".node" || ext == ".poly" || ext == ".ele")
            {
                List<ITriangle> triangles;
                Polygon geometry;

                TriangleReader.Read(filename, out geometry, out triangles);

                if (geometry != null && triangles != null)
                {
                    return Converter.ToMesh(geometry, triangles.ToArray());
                }
            }

            throw new NotSupportedException("Could not load '" + filename + "' file.");
        }

        public void Write(IMesh mesh, string filename)
        {
            TriangleWriter.WritePoly((Mesh)mesh, Path.ChangeExtension(filename, ".poly"));
            TriangleWriter.WriteElements((Mesh)mesh, Path.ChangeExtension(filename, ".ele"));
        }

        public void Write(IMesh mesh, StreamWriter stream)
        {
            throw new NotImplementedException();
        }

        public IPolygon Read(string filename)
        {
            string ext = Path.GetExtension(filename);

            if (ext == ".node")
            {
                return TriangleReader.ReadNodeFile(filename);
            }

            if (ext == ".poly")
            {
                return TriangleReader.ReadPolyFile(filename);
            }

            throw new NotSupportedException("File format '" + ext + "' not supported.");
        }


        public void Write(IPolygon polygon, string filename)
        {
            TriangleWriter.WritePoly(polygon, filename);
        }

        public void Write(IPolygon polygon, StreamWriter stream)
        {
            throw new NotImplementedException();
        }
    }
}
