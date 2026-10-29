using System;
using UnityEngine;

namespace RTS.DOTS
{
    [CreateAssetMenu(menuName = "RTS/DOTS/Swarm Visual Library")]
    public sealed class SwarmVisualLibrary : ScriptableObject
    {
        [Serializable]
        public class Visual
        {
            public string Name;
            public Sprite[] Idle, Move, Attack, Dead;
            public float FramesPerSecond = 10;
            public float Scale = 1;
            public bool RotateWithVelocity;

            public Sprite Frame(SwarmState state, float age, int offset)
            {
                Sprite[] frames = state == SwarmState.Dead ? Dead : state == SwarmState.Attacking ? Attack : Move;
                if (frames == null || frames.Length == 0) frames = Idle;
                if (frames == null || frames.Length == 0) return null;
                int frame = Mathf.Max(0, (int)(age * FramesPerSecond));
                return frames[state == SwarmState.Dead ? Mathf.Min(frame, frames.Length - 1) : (frame + offset) % frames.Length];
            }
        }
        public Visual[] Visuals = Array.Empty<Visual>();
    }
}
