using System;
namespace SchoolRuleZero
{
    public static class JumpRules
    {
        // Step 3 cancel = release Space to shorten ascent; no extra air jump.
        public static float CancelAscent(float verticalSpeed, bool released, float multiplier)
        {
            if (multiplier < 0f || multiplier > 1f)
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            return released && verticalSpeed > 0f ? verticalSpeed * multiplier : verticalSpeed;
        }
    }
}
