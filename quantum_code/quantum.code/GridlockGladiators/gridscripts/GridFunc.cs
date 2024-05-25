using Photon.Deterministic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Quantum
{
    /// <summary>
    /// Used to label which side of the grid an object belongs to
    /// </summary>
    [System.Serializable]
    public enum GridAlignment
    {
        NONE,
        LEFT,
        RIGHT,
        ANY
    }

    public delegate bool Condition();

    public partial struct Grid
    {

        /// <summary>
        /// Finds and outputs the panel at the given location.
        /// </summary>
        /// <param name="x">The x position  of the panel.</param>
        /// <param name="y">The y position of the panel.</param>
        /// <param name="panel">The panel reference to output to.</param>
        /// <param name="canBeOccupied">If true, the function will return true even if the panel found is occupied.</param>
        /// <param name="alignment">Will return false if the panel found doesn't match this alignment.</param>
        /// <returns>Returns true if the panel is found in the list and the canBeOccupied condition is met.</returns>
        //public bool GetPanel(int x, int y, out GridPanel panel, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY)
        //{
        //    panel = default;

        //    //If the given position is in range or if the panel is occupied when it shouldn't be, return false.
        //    if (x < 0 || x >= Width || y < 0 || y >= Height)
        //        return false;
        //    else if (!canBeOccupied && Panels[ new FPVector2( x, y)].Occupied)
        //        return false;
        //    //else if (Panels[x, y].Alignment != alignment && alignment != GridAlignment.ANY)
        //    //    return false;

        //    panel = Panels[x, y];

        //    return true;
        //}


        ///// <summary>
        ///// Finds and outputs the panel at the given location.
        ///// </summary>
        ///// <param name="position">The position of the panel on the grid.</param>
        ///// <param name="panel">The panel reference to output to.</param>
        ///// <param name="canBeOccupied">If true, the function will return true even if the panel found is occupied.</param>
        ///// <param name="alignment">Will return false if the panel found doesn't match this alignment.</param>
        ///// <returns>Returns true if the panel is found in the list and the canBeOccupied condition is met.</returns>
        //public bool GetPanel(FPVector2 position, out GridPanel panel, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY)
        //{
        //    panel = default;

        //    if (Panels == null)
        //        return false;

        //    //If the given position is in range or if the panel is occupied when it shouldn't be, return false.
        //    if (position.X < 0 || position.X >= Width || position.Y < 0 || position.Y >= Height)
        //        return false;
        //    else if (!canBeOccupied && Panels[(int)position.X, (int)position.Y].Occupied)
        //        return false;
        //    else if (Panels[(int)position.X, (int)position.Y].Alignment != alignment && alignment != GridAlignment.ANY)
        //        return false;



        //    panel = Panels[FPMath.RoundToInt(position.X), FPMath.RoundToInt(position.Y)];

        //    return true;
        //}
    }
}
