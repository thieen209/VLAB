using System;
using UnityEngine;

namespace VLAB.Core.Input
{
    [Serializable]
    public struct VLABInputState
    {
        public Vector2 Move;
        public Vector2 Look;
        public float ScrollDelta;
        public bool PrimaryPressed;
        public bool SecondaryPressed;
        public bool LeftGrabPressed;
        public bool RightGrabPressed;
        public bool SprintPressed;
        public bool JumpPressed;
        public bool DropPressed;
        public bool PausePressed;
        public float LeftTriggerValue;
        public float RightTriggerValue;
    }
}
