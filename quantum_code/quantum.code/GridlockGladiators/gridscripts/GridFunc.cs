using Photon.Deterministic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Quantum
{

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
        public bool GetPanel(Frame f, int x, int y, out GridPanel panel, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY)
        {
            panel = default;

            var panels = f.ResolveDictionary(Panels);

            FPVector2 coord = new FPVector2(x, y);

            //If the given position is in range or if the panel is occupied when it shouldn't be, return false.
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return false;
            else if (!canBeOccupied && panels[coord].Occupied)
                return false;
            //else if (Panels[x, y].Alignment != alignment && alignment != GridAlignment.ANY)
            //    return false;

            panel = panels[coord];

            return true;
        }


        /// <summary>
        /// Finds and outputs the panel at the given location.
        /// </summary>
        /// <param name="position">The position of the panel on the grid.</param>
        /// <param name="panel">The panel reference to output to.</param>
        /// <param name="canBeOccupied">If true, the function will return true even if the panel found is occupied.</param>
        /// <param name="alignment">Will return false if the panel found doesn't match this alignment.</param>
        /// <returns>Returns true if the panel is found in the list and the canBeOccupied condition is met.</returns>
        public bool GetPanel(Frame f, FPVector2 position, out GridPanel panel, bool canBeOccupied = true, GridAlignment alignment = GridAlignment.ANY)
        {
            panel = default;

            var panels = f.ResolveDictionary(Panels);

            if (Panels == null)
                return false;

            //If the given position is in range or if the panel is occupied when it shouldn't be, return false.
            if (position.X < 0 || position.X >= Width || position.Y < 0 || position.Y >= Height)
                return false;
            else if (!canBeOccupied && panels[position].Occupied)
                return false;
            else if (panels[position].Alignment != alignment && alignment != GridAlignment.ANY)
                return false;



            panel = panels[position];

            return true;
        }
    }
}
