#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Vlab.Product;

public sealed partial class WorkbenchUpgradeProbe
{
    IEnumerator StudentTourChecks()
    {
        yield return Wait("home");
        // Developer result fixture tests overlay routing, not student completion or science.
        var record=new AttemptRecord{experimentId="P01",attemptId="tour-result-fixture",savedAtUtc=DateTime.UtcNow.ToString("o")};
        record.readings.Add(new Reading("period",1.556,"s"));
        App.Profile.attempts.Add(record);
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2340,1080)})
        {
            Check("Viewport "+size,SecondTestGameView.Configure(out _,size.x,size.y));
            Call("StartQuickStart");yield return new WaitForSecondsRealtime(.4f);
            Check("Tour appears on home",App.GetComponentsInChildren<Transform>().Any(t=>t.name=="Quick tour"));
            Call("ShowResults",record,0);yield return new WaitForSecondsRealtime(.4f);
            Check("Tour does not cover result actions",!App.GetComponentsInChildren<Transform>().Any(t=>t.name=="Quick tour"));
            yield return Capture("student-tour-results-"+size.x);
            yield return Tap("Open result journal");yield return Wait("notebook");
            Check("Tour does not cover notebook",!App.GetComponentsInChildren<Transform>().Any(t=>t.name=="Quick tour"));
            App.ShowHome();yield return new WaitForSecondsRealtime(.4f);
            Check("Tour resumes on home",App.GetComponentsInChildren<Transform>().Any(t=>t.name=="Quick tour"));
            yield return Tap("Quick tour exit");
        }
    }
}
#endif
