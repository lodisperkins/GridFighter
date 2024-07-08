using Photon.Deterministic;
using Quantum.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quantum.GridlockGladiators.GridScripts
{
    public unsafe class GridMovementSystem : SystemMainThreadFilter<GridMovementSystem.Filter>
    {
        private FP TargetTolerance = FP._0_10;

        public struct Filter
        {
            public EntityRef Entity;
            public CharacterController3D* CharacterController;
            public GridMovement* Movement;
            public Transform3D* Transform;
        }

        public override void Update(Frame f, ref Filter filter)
        {
            GridMovement* movement = filter.Movement;

            int currentX = movement->X;
            int currentY = movement->Y;

            GridPanel currentPanel;

            Grid grid = f.GetSingleton<Grid>();

            grid.GetPanel(f, currentX, currentY, out currentPanel, false, movement->DefaultAlignment);


            if (FPVector3.Distance(currentPanel.WorldPosition, filter.Transform->Position) > TargetTolerance)
                LerpSystem.StartLerpRoutine(f, filter.Entity, filter.Transform->Position, currentPanel.WorldPosition, movement->Speed / 1);
        }
    }
}
