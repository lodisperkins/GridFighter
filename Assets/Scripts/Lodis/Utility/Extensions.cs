using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Utility
{
    public static class Extensions
    {
        public static int GetCombinedMask(this LayerMask[] masks)
        {
            int total = 0;

            for (int i = 0; i < masks.Length; i++)
                total += masks[i];

            return total;
        }
    }
}
