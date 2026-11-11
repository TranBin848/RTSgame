using System;

public static class ProceduralMapRuntime
{
    public static bool IsActive { get; private set; }
    public static bool IsReady { get; private set; }
    public static bool HasFailed { get; private set; }
    public static string LastError { get; private set; }
    public static event Action Ready = delegate { };
    public static bool IsCompositionBlocked { get; private set; }
    public static void BeginComposition() { IsCompositionBlocked = true; }
    public static void CompleteComposition() { IsCompositionBlocked = false; }
    public static void FailComposition(string error) { IsCompositionBlocked = true; HasFailed = true; LastError = error; }
    public static void EndComposition() { IsCompositionBlocked = false; }

    public static void Begin()
    {
        IsActive = true;
        IsReady = false;
        HasFailed = false;
        LastError = null;
    }

    public static void Complete()
    {
        IsReady = true;
        HasFailed = false;
        Ready.Invoke();
    }

    public static void Fail(string error)
    {
        IsActive = false;
        IsReady = false;
        HasFailed = true;
        LastError = error;
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        IsCompositionBlocked = false;
        IsActive = false;
        IsReady = false;
        HasFailed = false;
        LastError = null;
        Ready = delegate { };
    }
}
