﻿// -----------------------------------------------------------------------
// <copyright file="RegionIterator.cs" company="">
// Original Matlab code by John Burkardt, Florida State University
// Triangle.NET code by Christian Woltering, http://triangle.codeplex.com/
// </copyright>
// -----------------------------------------------------------------------

namespace DMMTriangleNet.Meshing.Iterators
{
    using System;
    using System.Collections.Generic;
    using DMMTriangleNet.Topology;

    /// <summary>
    /// Iterates the region a given triangle belongs to and applies an action
    /// to each connected trianlge in that region. Default action is to set the 
    /// region id.
    /// </summary>
    public class RegionIterator
    {
        //Mesh mesh;
        List<Triangle> viri;

        public RegionIterator(Mesh mesh)
        {
            //this.mesh = mesh;
            this.viri = new List<Triangle>();
        }

        /// <summary>
        /// Apply given action to each triangle of selected region.
        /// </summary>
        /// <param name="func"></param>
        void ProcessRegion(Action<Triangle> func)
        {
            Otri testtri = default(Otri);
            Otri neighbor = default(Otri);
            Osub neighborsubseg = default(Osub);

            //Behavior behavior = mesh.behavior;

            // Loop through all the infected triangles, spreading the attribute
            // and/or area constraint to their neighbors, then to their neighbors'
            // neighbors.
            for (int i = 0; i < viri.Count; i++)
            {
                // WARNING: Don't use foreach, viri list gets modified.

                testtri.triangle = viri[i];
                // A triangle is marked as infected by messing with one of its pointers
                // to subsegments, setting it to an illegal value.  Hence, we have to
                // temporarily uninfect this triangle so that we can examine its
                // adjacent subsegments.
                // TODO: Not true in the C# version (so we could skip this).
                testtri.Uninfect();

                // Apply function.
                func(testtri.triangle);

                // Check each of the triangle's three neighbors.
                for (testtri.orient = 0; testtri.orient < 3; testtri.orient++)
                {
                    // Find the neighbor.
                    testtri.Sym(ref neighbor);
                    // Check for a subsegment between the triangle and its neighbor.
                    testtri.SegPivot(ref neighborsubseg);
                    // Make sure the neighbor exists, is not already infected, and
                    // isn't protected by a subsegment.
                    if ((neighbor.triangle.id != Triangle.EmptyID) && !neighbor.IsInfected()
                        && (neighborsubseg.seg == Segment.Empty))
                    {
                        // Infect the neighbor.
                        neighbor.Infect();
                        // Ensure that the neighbor's neighbors will be infected.
                        viri.Add(neighbor.triangle);
                    }
                }
                // Remark the triangle as infected, so it doesn't get added to the
                // virus pool again.
                testtri.Infect();
            }

            // Uninfect all triangles.
            foreach (var virus in viri)
            {
                virus.infected = false;
            }

            // Empty the virus pool.
            viri.Clear();
        }

        /// <summary>
        /// Set the region attribute of all trianlges connected to given triangle.
        /// </summary>
        public void Process(Triangle triangle)
        {
            // Default action is to just set the region id for all trianlges.
            this.Process(triangle, (tri) => { tri.region = triangle.region; });
        }

        /// <summary>
        /// Process all trianlges connected to given triangle and apply given action.
        /// </summary>
        public void Process(Triangle triangle, Action<Triangle> func)
        {
            if (triangle.id != Triangle.EmptyID)
            {
                // Make sure the triangle under consideration still exists.
                // It may have been eaten by the virus.
                if (!Otri.IsDead(triangle))
                {
                    // Put one triangle in the virus pool.
                    triangle.infected = true;
                    viri.Add(triangle);
                    // Apply one region's attribute and/or area constraint.
                    ProcessRegion(func);
                    // The virus pool should be empty now.
                }
            }

            // Free up memory (virus pool should be empty anyway).
            viri.Clear();
        }
    }
}
