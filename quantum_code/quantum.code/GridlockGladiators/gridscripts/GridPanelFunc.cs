using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Quantum
{
    public partial struct GridPanel
    {
        [FieldOffset(0)]
        public GridAlignment Alignment;
        
        public bool ComparePosition(int x, int y)
        {
            return X == x && Y == y;
        }

        public void AddPosition(int x, int y)
        {
            X += x;
            Y += y;
        }
    }
}
