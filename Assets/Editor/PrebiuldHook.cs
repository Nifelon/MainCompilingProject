#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Game.CodeGen;  // <-- ÂÀÆÍÎ

public class PrebuildHook : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        ItemIdCodeGen.Generate();
    }
}
#endif