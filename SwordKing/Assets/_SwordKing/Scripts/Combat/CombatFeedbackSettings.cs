using System;
using UnityEngine;

namespace SwordKing
{
    [Serializable]
    public sealed class CombatFeedbackSettings
    {
        public bool hitStop = true, cameraShake = true, sparks = true;
        [Range(0, .08f)] public float lightHitPause = .018f, heavyHitPause = .045f;
        [Range(0, 2)] public float shakeStrength = 1f;
    }
}
