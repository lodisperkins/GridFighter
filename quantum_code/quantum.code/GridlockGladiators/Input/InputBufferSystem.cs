using Photon.Deterministic;
using Quantum;
using System.Diagnostics;
using System.Runtime.InteropServices;

public struct InputBuffer
{
    public FP TimeAdded;
    public FP BufferClearTime;
    public InputFlag InputFlag;
    public Condition Condition;
    public InputBufferAction Action;
    public bool Used;
    public object[] AdditionalArguments;

    public bool TryPerformAction(Frame f, EntityRef entity)
    {
        if (Used) return false;

        FP totalTime = f.Number * f.DeltaTime;

        Log.Info("Conditions checked");

        if (Condition.Invoke() && totalTime - TimeAdded <= BufferClearTime)
        {
            Used = true;
            Action?.Invoke(f, entity, AdditionalArguments);
            return true;
        }
        return false;
    }
}

public delegate void InputBufferAction(Frame f, EntityRef entity, params object[] args);

public unsafe class InputBufferSystem : SystemMainThreadFilter<InputBufferSystem.Filter>
{
    public struct Filter
    {
        public EntityRef Entity;
        public QPlayerInput InputComp;
    }

    private InputBuffer p1Buffer;
    private InputBuffer p2Buffer;

    public override void Update(Frame f, ref Filter filter)
    {
                Log.Info("Created Buffer");
        var input = filter.InputComp;
        if (input.needsBuffer)
        {
            if (input.playerNum == 0)
            {
                p1Buffer = CreateBuffer(f, filter.Entity, f.Number * f.DeltaTime, f.GetPlayerInput(input.playerNum)->inputFlag);
            }
            else if (input.playerNum == 1)
            {
                p2Buffer = CreateBuffer(f, filter.Entity, f.Number * f.DeltaTime, f.GetPlayerInput(input.playerNum)->inputFlag);
            }

            input.needsBuffer = false;
        }

        p1Buffer.TryPerformAction(f, filter.Entity);
        p2Buffer.TryPerformAction(f, filter.Entity);
    }

    private InputBuffer CreateBuffer(Frame f, EntityRef entity, FP time, InputFlag flag)
    {
        InputBuffer newBuffer = new InputBuffer();
        newBuffer.InputFlag = flag;
        newBuffer.TimeAdded = time;
        newBuffer.BufferClearTime = FP._0_20;

        var movement = f.Unsafe.GetPointer<GridMovement>(entity);
        switch (flag)
        {
            case InputFlag.Up:
                newBuffer.AdditionalArguments = [new FPVector2(0, 1)];
                newBuffer.Condition = () => !movement->IsMoving && movement->CanMove;
                newBuffer.Action = MoveEntity;
                break;
            case InputFlag.Down:
                newBuffer.AdditionalArguments = [new FPVector2(0, -1)];
                newBuffer.Condition = () => !movement->IsMoving && movement->CanMove;
                newBuffer.Action = MoveEntity;
                break;
            case InputFlag.Left:
                newBuffer.AdditionalArguments = [new FPVector2(-1, 0)];
                newBuffer.Condition = () => !movement->IsMoving && movement->CanMove;
                newBuffer.Action = MoveEntity;
                break;
            case InputFlag.Right:
                newBuffer.AdditionalArguments = [new FPVector2(0, 1)];
                newBuffer.Condition = () => !movement->IsMoving && movement->CanMove;
                newBuffer.Action = MoveEntity;
                break;
            //case InputFlag.Weak:
            //    UseAbility(f, entity, AbilityType.Weak);
            //    break;
            //case InputFlag.Special1:
            //    UseAbility(f, entity, AbilityType.Special1);
            //    break;
            //case InputFlag.Special2:
            //    UseAbility(f, entity, AbilityType.Special2);
            //    break;
            case InputFlag.Shuffle:
                UseShuffle(f, entity);
                break;
            case InputFlag.Burst:
                UseBurst(f, entity);
                break;
            default:
                break;
        }

        return newBuffer;
    }

    private void MoveEntity(Frame f, EntityRef entity, params object[] args)
    {
        Log.Info("Tried move");
        FPVector2 direction = (FPVector2)args[0];

        var movement = f.Unsafe.GetPointer<GridMovement>(entity);
        if (movement == null || movement->IsMoving) return;

        int newX = movement->X + (int)direction.X;
        int newY = movement->Y + (int)direction.Y;

        movement->X = newX;
        movement->Y = newY;
        movement->MoveDirection = direction;
        movement->IsMoving = true;
    }

    //private void UseAbility(Frame f, EntityRef entity, AbilityType abilityType)
    //{
    //    // Implement ability usage logic
    //}

    private void UseShuffle(Frame f, EntityRef entity)
    {
        // Implement shuffle logic
    }

    private void UseBurst(Frame f, EntityRef entity)
    {
        // Implement burst logic
    }
}
