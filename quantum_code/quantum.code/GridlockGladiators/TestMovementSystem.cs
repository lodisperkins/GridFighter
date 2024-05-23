using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quantum.GridlockGladiators
{
    public unsafe class TestMovementSystem : SystemMainThreadFilter<TestMovementSystem.Filter>
    {
        public struct Filter
        {
            public EntityRef Entity;
            public CharacterController3D* CharacterController;
        }

        public override void Update(Frame currentFrame, ref Filter filter)
        {
            Input input = *currentFrame.GetPlayerInput(0);

            if (input.Jump.WasPressed)
            {
                filter.CharacterController->Jump(currentFrame);
            }

            filter.CharacterController->Move(currentFrame, filter.Entity, default);
        }
    }
}
