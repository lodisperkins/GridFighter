// GENERATED AUTOMATICALLY FROM 'Assets/Scripts/Lodis/Input/PlayerControls.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @PlayerControls : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @PlayerControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerControls"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""3a0b2e15-147d-4f3e-8590-9b37c7f6a33b"",
            ""actions"": [
                {
                    ""name"": ""MoveUp"",
                    ""type"": ""Button"",
                    ""id"": ""1b6e4686-58b7-4df6-9262-1b833e147595"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""MoveDown"",
                    ""type"": ""Button"",
                    ""id"": ""e0662632-1c37-43bb-ae1f-8574537a37e7"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""MoveLeft"",
                    ""type"": ""Button"",
                    ""id"": ""3541cc32-54af-4820-8cd7-fd1d0f118547"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""MoveRight"",
                    ""type"": ""Button"",
                    ""id"": ""76834e87-8c75-47eb-b6f4-4db68a36d750"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Attack"",
                    ""type"": ""Button"",
                    ""id"": ""6dfd1669-e92b-4eb6-831c-47fc79628e51"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=1)""
                },
                {
                    ""name"": ""AttackDirection"",
                    ""type"": ""Button"",
                    ""id"": ""598d27b6-e9e6-474f-8b35-b9db0ea692ca"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Parry"",
                    ""type"": ""Button"",
                    ""id"": ""c9d9664a-c346-4620-ada4-088ae01453e4"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press(pressPoint=0.1,behavior=1)""
                },
                {
                    ""name"": ""Special1"",
                    ""type"": ""Button"",
                    ""id"": ""2d8fb853-4e12-420a-a188-9ba4181f5c0d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Special2"",
                    ""type"": ""Button"",
                    ""id"": ""d7a42d2d-614c-42a4-ba4c-2572df6a6c0c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""UnblockableAttack"",
                    ""type"": ""Button"",
                    ""id"": ""99c04eff-a462-4dac-90a6-a4ddd5d66a8c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                },
                {
                    ""name"": ""Burst"",
                    ""type"": ""Button"",
                    ""id"": ""58a4ab25-5095-492a-9218-9d97b1719aff"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Press""
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""54d74de4-552f-4c30-8ccc-ae1516cfef88"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveUp"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""70391c83-0b6b-4ece-a239-07bf20dc091e"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/hat/up"",
                    ""interactions"": ""Press"",
                    ""processors"": ""AxisDeadzone(min=0.9,max=0.9)"",
                    ""groups"": """",
                    ""action"": ""MoveUp"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5379d112-60d1-4846-add0-244aed9365d7"",
                    ""path"": ""<Gamepad>/dpad/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveUp"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""cf5e8713-ecee-407a-891c-da91448b0177"",
                    ""path"": ""<Joystick>/stick/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveUp"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d1c2f7aa-5956-4f07-83f1-f41166389568"",
                    ""path"": ""<Gamepad>/leftStick/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveUp"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9581f419-8578-4a3f-8c04-a94e01a56934"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ee013db9-c0cf-4bd4-81f2-a718bfd6f5f3"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/hat/left"",
                    ""interactions"": ""Press"",
                    ""processors"": ""AxisDeadzone(min=0.9)"",
                    ""groups"": """",
                    ""action"": ""MoveLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c39f8694-1b2d-4f01-9983-40c041f2b2c0"",
                    ""path"": ""<Gamepad>/dpad/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5cac9ef5-f9b7-4df0-b187-839c626cf2ad"",
                    ""path"": ""<Gamepad>/leftStick/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""58e86f28-77cf-41a9-8afd-8c688c14aaf2"",
                    ""path"": ""<Joystick>/stick/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""cd83beeb-5073-440e-8949-df3785725846"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""927542c0-95b7-40c1-95ed-691504cbd4bc"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/hat/right"",
                    ""interactions"": ""Press"",
                    ""processors"": ""AxisDeadzone(min=0.9)"",
                    ""groups"": """",
                    ""action"": ""MoveRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a63290cd-2776-41a0-bb78-f73c13944207"",
                    ""path"": ""<Gamepad>/dpad/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""20656cb3-7618-46cc-9f9f-2a69004b5d86"",
                    ""path"": ""<Gamepad>/leftStick/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c5bfa505-c8c6-4d42-b5d2-9a99fd517955"",
                    ""path"": ""<Joystick>/stick/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b558f233-efec-4a40-acb0-feca593cf22b"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": ""Press"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveDown"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2cd5c613-df89-4ddc-a1b5-389e1c4e043d"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/hat/down"",
                    ""interactions"": ""Press"",
                    ""processors"": ""AxisDeadzone(min=0.9,max=0.925)"",
                    ""groups"": """",
                    ""action"": ""MoveDown"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""48d3a5f2-c50e-462c-9430-2c221cf80320"",
                    ""path"": ""<Gamepad>/dpad/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveDown"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d14e1269-b734-4885-81e3-54e9efdb2654"",
                    ""path"": ""<Joystick>/stick/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveDown"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""7e501495-6fc9-44ae-b973-1edc71894ebb"",
                    ""path"": ""<Gamepad>/leftStick/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MoveDown"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a2e7c22c-98ff-4910-93ed-65247fd241e2"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": ""Press(behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Attack"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""085619bd-d953-4c02-a2f2-7ee63886939b"",
                    ""path"": ""<Joystick>/trigger"",
                    ""interactions"": ""Press(pressPoint=0.1,behavior=1)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Attack"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""65134b81-1bbe-478b-99c1-ce60dfe4b1c9"",
                    ""path"": ""<Gamepad>/buttonWest"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Attack"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""WASD"",
                    ""id"": ""1482424c-507d-46ff-9b10-a0a5a24e05f7"",
                    ""path"": ""2DVector(mode=1)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""5061e9a9-b633-4f4a-a8e5-f4e72fa34bf5"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""0617920b-5462-4429-8f18-282858baca77"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""5584b3ff-a16b-4c75-a72d-38deecada672"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""93a725f3-579a-473f-8089-e4e3419c6329"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""DPAD(Hori)"",
                    ""id"": ""4f608611-efad-460c-9e3e-f4078cfcd330"",
                    ""path"": ""2DVector(mode=2)"",
                    ""interactions"": """",
                    ""processors"": ""AxisDeadzone(min=0.9)"",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""8c003466-607e-4688-9be2-35ee19417694"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/hat/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""5200b57d-c725-42da-ab05-5743a8c9e655"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/hat/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""f8591fff-1af5-4d77-833d-4fe1aa49983d"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/hat/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""b70b79e4-4f1f-424b-b3d4-d3ee48a52e2c"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/hat/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Analog(Joystick)"",
                    ""id"": ""87f24b97-6f94-4b54-8132-f74d1fd51371"",
                    ""path"": ""2DVector(mode=2)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""50cc6cd4-90de-4dce-9c5e-61a00e1f3d41"",
                    ""path"": ""<Joystick>/stick/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""1f5a1393-ff75-4971-80a1-3e2f1a91414f"",
                    ""path"": ""<Joystick>/stick/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""92056bd8-6fb3-440f-8102-047406be60be"",
                    ""path"": ""<Joystick>/stick/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""d12ddad8-e02b-463f-8cd2-79538580cf6e"",
                    ""path"": ""<Joystick>/stick/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""DPAD(GamePad)"",
                    ""id"": ""34a1cc84-0cda-4829-8585-4330ce565f97"",
                    ""path"": ""2DVector(mode=1)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""9fdcc639-9602-4664-b0d0-cc1226a5cdf5"",
                    ""path"": ""<Gamepad>/dpad/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""fe096a2e-3732-4203-8769-df33fa5a2a40"",
                    ""path"": ""<Gamepad>/dpad/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""db6ba339-b1e2-4735-ae6f-ba956691fa41"",
                    ""path"": ""<Gamepad>/dpad/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""c6ee99ba-0648-47b5-9bc9-917a117e77ac"",
                    ""path"": ""<Gamepad>/dpad/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""Analog(GamePad)"",
                    ""id"": ""28244e02-ad4a-4961-a96d-e7c046178686"",
                    ""path"": ""2DVector(mode=2)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""7b18669d-4bd2-48df-b9ea-3515f286fe15"",
                    ""path"": ""<Gamepad>/leftStick/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""c31c4097-7c8b-4508-91d5-d4cf802bab01"",
                    ""path"": ""<Gamepad>/leftStick/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""b21149b5-671d-49f0-a8b3-d4052ea6654d"",
                    ""path"": ""<Gamepad>/leftStick/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""78c8d0f7-9258-493b-94c5-29450cb6a51a"",
                    ""path"": ""<Gamepad>/leftStick/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AttackDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""30042522-a031-47cf-8a11-e35b01957120"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Parry"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""93102aea-a9a6-462a-bb9b-3c81a1345c52"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/button6"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Parry"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""54d16124-3354-4fa9-a58d-92eb242ac5e9"",
                    ""path"": ""<Gamepad>/rightShoulder"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Parry"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2954790e-2b7a-4c8e-a73d-1bb277bd3ac0"",
                    ""path"": ""<Keyboard>/q"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Special1"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""319afb83-5a85-4616-a94f-2b46963bbd7c"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/button4"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Special1"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d7bd8fd2-ecd2-42b6-ad26-f008749046cc"",
                    ""path"": ""<Gamepad>/buttonNorth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Special1"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5d65c825-aafe-4501-991f-3f7769b67fa9"",
                    ""path"": ""<Keyboard>/e"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Special2"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""6b3312ac-24b6-46c0-987f-125192abc25b"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/button3"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Special2"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e0114d7e-0c7e-4623-9023-c91fbe5268c8"",
                    ""path"": ""<Gamepad>/buttonEast"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Special2"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""63fd0488-1a11-4b87-b21c-63feab526109"",
                    ""path"": ""<Keyboard>/f"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""UnblockableAttack"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4252d983-f41f-4ebe-b549-0e6d4c38914c"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/button5"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""UnblockableAttack"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d8a33f86-4739-407c-bf2d-b144a69fdf5a"",
                    ""path"": ""<Gamepad>/leftShoulder"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""UnblockableAttack"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""466ada18-b06e-4bc3-a1ac-2b9ab570e586"",
                    ""path"": ""<Keyboard>/r"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Burst"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""35c4f1bb-b8b7-4717-8199-178d895db277"",
                    ""path"": ""<HID::HORI CO.,LTD. HORIPAD mini4>/button8"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Burst"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2425efcb-0c42-4e91-bad7-1fd73bd10562"",
                    ""path"": ""<Gamepad>/rightTrigger"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Burst"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Player
        m_Player = asset.FindActionMap("Player", throwIfNotFound: true);
        m_Player_MoveUp = m_Player.FindAction("MoveUp", throwIfNotFound: true);
        m_Player_MoveDown = m_Player.FindAction("MoveDown", throwIfNotFound: true);
        m_Player_MoveLeft = m_Player.FindAction("MoveLeft", throwIfNotFound: true);
        m_Player_MoveRight = m_Player.FindAction("MoveRight", throwIfNotFound: true);
        m_Player_Attack = m_Player.FindAction("Attack", throwIfNotFound: true);
        m_Player_AttackDirection = m_Player.FindAction("AttackDirection", throwIfNotFound: true);
        m_Player_Parry = m_Player.FindAction("Parry", throwIfNotFound: true);
        m_Player_Special1 = m_Player.FindAction("Special1", throwIfNotFound: true);
        m_Player_Special2 = m_Player.FindAction("Special2", throwIfNotFound: true);
        m_Player_UnblockableAttack = m_Player.FindAction("UnblockableAttack", throwIfNotFound: true);
        m_Player_Burst = m_Player.FindAction("Burst", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Player
    private readonly InputActionMap m_Player;
    private IPlayerActions m_PlayerActionsCallbackInterface;
    private readonly InputAction m_Player_MoveUp;
    private readonly InputAction m_Player_MoveDown;
    private readonly InputAction m_Player_MoveLeft;
    private readonly InputAction m_Player_MoveRight;
    private readonly InputAction m_Player_Attack;
    private readonly InputAction m_Player_AttackDirection;
    private readonly InputAction m_Player_Parry;
    private readonly InputAction m_Player_Special1;
    private readonly InputAction m_Player_Special2;
    private readonly InputAction m_Player_UnblockableAttack;
    private readonly InputAction m_Player_Burst;
    public struct PlayerActions
    {
        private @PlayerControls m_Wrapper;
        public PlayerActions(@PlayerControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @MoveUp => m_Wrapper.m_Player_MoveUp;
        public InputAction @MoveDown => m_Wrapper.m_Player_MoveDown;
        public InputAction @MoveLeft => m_Wrapper.m_Player_MoveLeft;
        public InputAction @MoveRight => m_Wrapper.m_Player_MoveRight;
        public InputAction @Attack => m_Wrapper.m_Player_Attack;
        public InputAction @AttackDirection => m_Wrapper.m_Player_AttackDirection;
        public InputAction @Parry => m_Wrapper.m_Player_Parry;
        public InputAction @Special1 => m_Wrapper.m_Player_Special1;
        public InputAction @Special2 => m_Wrapper.m_Player_Special2;
        public InputAction @UnblockableAttack => m_Wrapper.m_Player_UnblockableAttack;
        public InputAction @Burst => m_Wrapper.m_Player_Burst;
        public InputActionMap Get() { return m_Wrapper.m_Player; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlayerActions set) { return set.Get(); }
        public void SetCallbacks(IPlayerActions instance)
        {
            if (m_Wrapper.m_PlayerActionsCallbackInterface != null)
            {
                @MoveUp.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveUp;
                @MoveUp.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveUp;
                @MoveUp.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveUp;
                @MoveDown.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveDown;
                @MoveDown.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveDown;
                @MoveDown.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveDown;
                @MoveLeft.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveLeft;
                @MoveLeft.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveLeft;
                @MoveLeft.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveLeft;
                @MoveRight.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveRight;
                @MoveRight.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveRight;
                @MoveRight.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnMoveRight;
                @Attack.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnAttack;
                @Attack.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnAttack;
                @Attack.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnAttack;
                @AttackDirection.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnAttackDirection;
                @AttackDirection.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnAttackDirection;
                @AttackDirection.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnAttackDirection;
                @Parry.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnParry;
                @Parry.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnParry;
                @Parry.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnParry;
                @Special1.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSpecial1;
                @Special1.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSpecial1;
                @Special1.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSpecial1;
                @Special2.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSpecial2;
                @Special2.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSpecial2;
                @Special2.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnSpecial2;
                @UnblockableAttack.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnUnblockableAttack;
                @UnblockableAttack.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnUnblockableAttack;
                @UnblockableAttack.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnUnblockableAttack;
                @Burst.started -= m_Wrapper.m_PlayerActionsCallbackInterface.OnBurst;
                @Burst.performed -= m_Wrapper.m_PlayerActionsCallbackInterface.OnBurst;
                @Burst.canceled -= m_Wrapper.m_PlayerActionsCallbackInterface.OnBurst;
            }
            m_Wrapper.m_PlayerActionsCallbackInterface = instance;
            if (instance != null)
            {
                @MoveUp.started += instance.OnMoveUp;
                @MoveUp.performed += instance.OnMoveUp;
                @MoveUp.canceled += instance.OnMoveUp;
                @MoveDown.started += instance.OnMoveDown;
                @MoveDown.performed += instance.OnMoveDown;
                @MoveDown.canceled += instance.OnMoveDown;
                @MoveLeft.started += instance.OnMoveLeft;
                @MoveLeft.performed += instance.OnMoveLeft;
                @MoveLeft.canceled += instance.OnMoveLeft;
                @MoveRight.started += instance.OnMoveRight;
                @MoveRight.performed += instance.OnMoveRight;
                @MoveRight.canceled += instance.OnMoveRight;
                @Attack.started += instance.OnAttack;
                @Attack.performed += instance.OnAttack;
                @Attack.canceled += instance.OnAttack;
                @AttackDirection.started += instance.OnAttackDirection;
                @AttackDirection.performed += instance.OnAttackDirection;
                @AttackDirection.canceled += instance.OnAttackDirection;
                @Parry.started += instance.OnParry;
                @Parry.performed += instance.OnParry;
                @Parry.canceled += instance.OnParry;
                @Special1.started += instance.OnSpecial1;
                @Special1.performed += instance.OnSpecial1;
                @Special1.canceled += instance.OnSpecial1;
                @Special2.started += instance.OnSpecial2;
                @Special2.performed += instance.OnSpecial2;
                @Special2.canceled += instance.OnSpecial2;
                @UnblockableAttack.started += instance.OnUnblockableAttack;
                @UnblockableAttack.performed += instance.OnUnblockableAttack;
                @UnblockableAttack.canceled += instance.OnUnblockableAttack;
                @Burst.started += instance.OnBurst;
                @Burst.performed += instance.OnBurst;
                @Burst.canceled += instance.OnBurst;
            }
        }
    }
    public PlayerActions @Player => new PlayerActions(this);
    public interface IPlayerActions
    {
        void OnMoveUp(InputAction.CallbackContext context);
        void OnMoveDown(InputAction.CallbackContext context);
        void OnMoveLeft(InputAction.CallbackContext context);
        void OnMoveRight(InputAction.CallbackContext context);
        void OnAttack(InputAction.CallbackContext context);
        void OnAttackDirection(InputAction.CallbackContext context);
        void OnParry(InputAction.CallbackContext context);
        void OnSpecial1(InputAction.CallbackContext context);
        void OnSpecial2(InputAction.CallbackContext context);
        void OnUnblockableAttack(InputAction.CallbackContext context);
        void OnBurst(InputAction.CallbackContext context);
    }
}
