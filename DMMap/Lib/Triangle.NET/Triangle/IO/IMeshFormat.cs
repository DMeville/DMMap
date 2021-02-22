﻿// -----------------------------------------------------------------------
// <copyright file="IMeshFormat.cs" company="">
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace DMMTriangleNet.IO
{
    using System.IO;
    using DMMTriangleNet.Meshing;

    /// <summary>
    /// Interface for mesh I/O.
    /// </summary>
    public interface IMeshFormat : IFileFormat
    {
        /// <summary>
        /// Read a file containing a mesh.
        /// </summary>
        /// <param name="filename">The path of the file to read.</param>
        /// <returns>An instance of the <see cref="IMesh" /> interface.</returns>
        IMesh Import(string filename);

        /// <summary>
        /// Save a mesh to disk.
        /// </summary>
        /// <param name="mesh">An instance of the <see cref="IMesh" /> interface.</param>
        /// <param name="filename">The path of the file to save.</param>
        void Write(IMesh mesh, string filename);

        /// <summary>
        /// Save a mesh to a <see cref="StreamWriter" />.
        /// </summary>
        /// <param name="mesh">An instance of the <see cref="IMesh" /> interface.</param>
        /// <param name="stream">The stream to save to.</param>
        void Write(IMesh mesh, StreamWriter stream);
    }
}
