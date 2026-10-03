using UnityEditor;

/// <summary>
/// Runs the CrossBound smoke tests from the menu or from CI.
/// </summary>
public static class CrossBoundProjectSetup
{
    [MenuItem("CrossBound/Setup Project")]
    public static void SetupProject()
    {
        CrossBoundSmokeTests.RunAll();
    }

    /// <summary>Entry point for CI/batchmode: Unity.exe -batchmode -executeMethod CrossBoundProjectSetup.BatchSetup -quit</summary>
    public static void BatchSetup()
    {
        CrossBoundSmokeTests.RunAll();
    }
}
