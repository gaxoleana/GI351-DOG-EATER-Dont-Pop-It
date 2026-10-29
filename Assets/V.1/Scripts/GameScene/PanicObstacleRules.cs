using UnityEngine;

// Shared spawn gate and collision policy for both panic-event variants.
public static class PanicObstacleRules
{
    private static PanicEventManagerVariantNoStop live;
    private static PanicEventManagerVariantTimeStop legacy;
    private static void Resolve()
    {
        if (live == null) live = Object.FindAnyObjectByType<PanicEventManagerVariantNoStop>();
        if (legacy == null) legacy = Object.FindAnyObjectByType<PanicEventManagerVariantTimeStop>();
    }
    public static bool EventRunning
    {
        get
        {
            Resolve();
            return (live != null && live.isActiveAndEnabled && live.currentState != PanicEventManagerVariantNoStop.EventState.Idle)
                || (legacy != null && legacy.isActiveAndEnabled && legacy.currentState != PanicEventManagerVariantTimeStop.EventState.Idle);
        }
    }
    public static bool InterruptForHit(PlayerController player)
    {
        Resolve();
        bool interrupted = false;
        if (live != null && live.isActiveAndEnabled && live.player == player && live.currentState != PanicEventManagerVariantNoStop.EventState.Idle)
        { live.CancelForObstacleHit(); interrupted = true; }
        if (legacy != null && legacy.isActiveAndEnabled && legacy.player == player && legacy.currentState != PanicEventManagerVariantTimeStop.EventState.Idle)
        { legacy.CancelForObstacleHit(); interrupted = true; }
        return interrupted;
    }
}
