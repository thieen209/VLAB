using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Unity.XR.CoreUtils;

namespace VLAB.MainMenu
{
    public static class VLabCanonicalRig
    {
        public static XROrigin Configure(Camera view, VLABMenuAssets assets)
        {
            // Close-wall pause placement must remain inside the rendered near volume.
            view.nearClipPlane=Mathf.Min(view.nearClipPlane,.08f);
            if(EventSystem.current!=null)
            {
                var events=EventSystem.current;
                foreach(var module in events.GetComponents<BaseInputModule>())module.enabled=false;
                var xr=events.GetComponent<XRUIInputModule>()??events.gameObject.AddComponent<XRUIInputModule>();
                xr.enabled=true;xr.enableMouseInput=true;xr.enableTouchInput=true;
            }
            // Register the scene owner before enabling prefab interactors.
            var manager=Object.FindAnyObjectByType<XRInteractionManager>();
            if(manager==null)manager=new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            var origin=view.GetComponentInParent<XROrigin>();
            if(origin==null)
            {
                if(assets?.playerRig==null)throw new System.InvalidOperationException("VLAB canonical Physics rig asset is missing.");
                // Adopt the existing calibrated view and locomotion parent without changing its pose.
                var host=Object.Instantiate(assets.playerRig,view.transform.parent);
                host.name="XR Origin (XR Rig)";
                host.transform.SetPositionAndRotation(new Vector3(view.transform.position.x,0,view.transform.position.z),Quaternion.identity);
                origin=host.GetComponent<XROrigin>();
                origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Floor;
                origin.CameraYOffset=0;
                origin.CameraFloorOffsetObject.transform.localPosition=Vector3.zero;
                var unusedView=origin.Camera;
                var sourceTracking=unusedView.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                if(sourceTracking!=null && view.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>()==null)
                {
                    var tracking=view.gameObject.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                    tracking.positionInput=sourceTracking.positionInput;tracking.rotationInput=sourceTracking.rotationInput;
                    tracking.trackingStateInput=sourceTracking.trackingStateInput;
                    tracking.enabled=UnityEngine.XR.XRSettings.isDeviceActive;
                }
                unusedView.gameObject.SetActive(false);
                view.transform.SetParent(origin.CameraFloorOffsetObject.transform,true);
                origin.Camera=view;
                if(!UnityEngine.XR.XRSettings.isDeviceActive || VLAB.Core.Input.VLabHeadPose.PhoneViewer)
                {
                    foreach(var provider in host.GetComponentsInChildren<LocomotionProvider>(true))provider.enabled=false;
                    foreach(var body in host.GetComponentsInChildren<CharacterController>(true))body.enabled=false;
                }
            }
            foreach(var group in origin.GetComponentsInChildren<XRInteractionGroup>(true))group.interactionManager=manager;
            foreach(var interactor in origin.GetComponentsInChildren<XRBaseInteractor>(true))interactor.interactionManager=manager;
            foreach(var ray in origin.GetComponentsInChildren<NearFarInteractor>(true))ray.enableUIInteraction=true;
            return origin;
        }
    }
}
