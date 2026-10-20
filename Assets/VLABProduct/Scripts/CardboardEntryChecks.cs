#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Vlab.Product;

// Synthetic lifecycle checks through the real VlabInput entry/scan coroutines.
// These do not load the Android SDK or certify stereo, optical fit, permissions or QR capture.
public static class CardboardEntryChecks
{
    sealed class Runtime:ICardboardEntryRuntime
    {
        public bool Supported{get;set;}=true;public bool HasGyroscope{get;set;}=true;public bool Initialized{get;private set;}
        public bool SavedProfile,Changed,StartSucceeds=true,Scanning;public int Initializes,Starts,Stops,Scans,Ticks,Reloads;
        public IEnumerator Initialize(){Initializes++;yield return null;Initialized=true;}
        public bool Start(){Starts++;return StartSucceeds;}
        public void StopAndDeinitialize(){Stops++;Initialized=false;}
        public void Scan(){Scans++;Scanning=true;}
        public void TickParameters(){Ticks++;if(Changed){Reloads++;Changed=false;}}
        public void CancelScan(){Scanning=false;}
        public void AcceptScan(){Scanning=false;SavedProfile=true;Changed=true;}
    }
    public static IEnumerator Run(Action<string,bool> check)
    {
        var go=new GameObject("Cardboard entry lifecycle check",typeof(Camera),typeof(VlabInput));var camera=go.GetComponent<Camera>();camera.enabled=false;var input=go.GetComponent<VlabInput>();var runtime=new Runtime();input.EntryRuntimeForChecks=runtime;input.Adopt(camera);
        try
        {
            string failure=null;input.ModeChanged+=(vr,message)=>failure=message;
            runtime.Supported=false;input.SetVr(true);yield return Settle();
            check("Unsupported platform stays flat without initializing XR",!input.VrMode&&runtime.Initializes==0&&!string.IsNullOrEmpty(failure));
            runtime.Supported=true;runtime.HasGyroscope=false;input.SetVr(true);yield return Settle();
            check("Missing gyro stays flat with truthful reason and no XR startup",!input.VrMode&&runtime.Initializes==0&&runtime.Starts==0&&failure==VlabInput.MissingVrGyroscope&&!go.GetComponent<TrackedPoseDriver>().enabled);
            runtime.HasGyroscope=true;
            input.SetVr(true);yield return Settle();
            check("Fresh one-tap entry starts stereo without any saved-profile or scan gate",input.VrMode&&runtime.Initializes==1&&runtime.Starts==1&&!runtime.SavedProfile&&runtime.Scans==0);
            check("Entry enables the real tracked-pose component",go.GetComponent<TrackedPoseDriver>().enabled);
            var hmd=InputSystem.AddDevice<XRHMD>();
            try
            {
                var head=Quaternion.Euler(18,35,-12);
                InputSystem.QueueDeltaStateEvent(hmd.centerEyeRotation,head);InputSystem.Update();
                check("Cardboard pose rotates camera in all axes even when HMD supplies no tracking-state flags",hmd.trackingState.ReadValue()==0&&Quaternion.Angle(camera.transform.localRotation,head)<.1f);
                head=Quaternion.Euler(-20,-40,15);
                InputSystem.QueueDeltaStateEvent(hmd.centerEyeRotation,head);InputSystem.Update();
                check("Successive head poses retain pitch yaw and roll without a second gyro transform",Quaternion.Angle(camera.transform.localRotation,head)<.1f);
            }
            finally{InputSystem.RemoveDevice(hmd);}
            input.SetVr(true);yield return Settle();check("Repeated Enter VR does not initialize or start a second time",runtime.Initializes==1&&runtime.Starts==1);
            input.SetVr(false);yield return Settle();check("Exit stops/deinitializes and returns to flat pose",!input.VrMode&&!runtime.Initialized&&runtime.Stops==1&&!go.GetComponent<TrackedPoseDriver>().enabled);
            input.SetVr(true);yield return Settle();check("Returning entry still requires no scan",input.VrMode&&runtime.Initializes==2&&runtime.Starts==2&&runtime.Scans==0);
            input.ScanViewer();yield return Settle();runtime.CancelScan();yield return Settle();check("Cancel optional scan while in VR retains entry and existing profile state",input.VrMode&&!runtime.SavedProfile&&runtime.Scans==1);
            input.SetVr(false);yield return Settle();int starts=runtime.Starts;
            input.ScanViewer();yield return Settle();check("Optional scan from flat Settings initializes SDK without starting stereo",runtime.Initialized&&runtime.Scans==2&&runtime.Starts==starts&&!input.VrMode);
            runtime.CancelScan();yield return Settle();check("Cancel optional flat scan does not enter VR or create saved parameters",!input.VrMode&&!runtime.SavedProfile);
            input.SetVr(true);yield return Settle();check("Enter after a canceled optional scan reuses initialized loader",input.VrMode&&runtime.Initializes==3&&runtime.Starts==starts+1&&runtime.Scans==2);
            input.ScanViewer();yield return Settle();runtime.AcceptScan();yield return Settle();check("New optional viewer parameters are reloaded by the per-frame SDK pump",input.VrMode&&runtime.SavedProfile&&runtime.Reloads==1&&!runtime.Changed&&runtime.Ticks>0);
            input.ScanViewer();yield return Settle();runtime.CancelScan();yield return Settle();check("Cancel later optional scan preserves an already saved viewer profile",input.VrMode&&runtime.SavedProfile&&runtime.Reloads==1);
            input.SetVr(false);yield return Settle();runtime.StartSucceeds=false;input.SetVr(true);yield return Settle();
            check("Failed stereo start cleans initialized subsystems and leaves flat mode",!input.VrMode&&!runtime.Initialized&&!go.GetComponent<TrackedPoseDriver>().enabled);
            runtime.StartSucceeds=true;input.SetVr(true);yield return Settle();check("A failed entry can be retried without a stuck switching lock",input.VrMode);
            input.SetVr(false);yield return Settle();
        }
        finally{UnityEngine.Object.Destroy(go);}
        yield return null;
    }
    static IEnumerator Settle(){for(int i=0;i<6;i++)yield return null;}
}
#endif
