using System;
using UnityEngine;

/// <summary>
/// Global noise bus. Player sprint, doors, and knock-overs call Emit().
/// Enemies subscribe without needing a direct reference to the source.
/// </summary>
public static class NoiseEvent
{
    public static event Action<Vector3, float> OnEmitted;

    /// <param name="worldPosition">Where the sound happened.</param>
    /// <param name="loudness">0-1 typical. Sprint ~0.7, a slammed door ~1.</param>
    public static void Emit(Vector3 worldPosition, float loudness)
    {
        OnEmitted?.Invoke(worldPosition, Mathf.Clamp01(loudness));
    }
}
