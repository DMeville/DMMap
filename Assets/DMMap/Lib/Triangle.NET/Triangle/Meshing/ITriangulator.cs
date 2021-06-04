// -----------------------------------------------------------------------
// <copyright file="ITriangulator.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace DMMTriangleNet.Meshing
{
    using System.Collections.Generic;
    using DMMTriangleNet.Geometry;

    /// <summary>
    /// Interface for point set triangulation.
    /// </summary>
    public interface ITriangulator
    {
        /// <summary>
        /// Triangulates a point set.
        /// </summary>
        /// <param name="points">Collection of points.</param>
        /// <returns>Mesh</returns>
        IMesh Triangulate(ICollection<Vertex> points);
    }
}
