using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.InputSystem;
using VLAB.DemoLabs;

namespace VLAB.MainMenu
{
    /// <summary>Original XRI hands submit the same guided-lesson actions as the fallback pointer.</summary>
    public sealed class VLabNativeLessonBridge : MonoBehaviour
    {
        private readonly List<XRSimpleInteractable> interactables=new List<XRSimpleInteractable>();
        private VLabInteractionDriver driver;
        private Transform pointingHand;
        private VLabInteractable selectedTarget;
        private InputAction rotation;
        private float nextRotation;
        private void Start()
        {
            driver=FindAnyObjectByType<VLabInteractionDriver>();
            if(driver==null)return;
            rotation=new InputAction("VLAB guided native rotation",InputActionType.Value,"<XRController>/primary2DAxis",expectedControlType:"Vector2");rotation.Enable();
            foreach(var target in FindObjectsByType<VLabInteractable>())
            {
                var xr=target.GetComponent<XRSimpleInteractable>()??target.gameObject.AddComponent<XRSimpleInteractable>();
                // Nested snap zones own their colliders; avoid parent interactables stealing them.
                xr.colliders.Clear();
                foreach(var collider in target.GetComponentsInChildren<Collider>())
                    if(collider.GetComponentInParent<VLabInteractable>()==target)xr.colliders.Add(collider);
                xr.selectEntered.AddListener(Select);xr.selectExited.AddListener(Release);interactables.Add(xr);
            }
        }
        private void Select(SelectEnterEventArgs args)
        {
            pointingHand=args.interactorObject is NearFarInteractor near && near.farInteractionCaster is CurveInteractionCaster caster?caster.castOrigin:args.interactorObject.transform;
            driver.NativePointer=pointingHand;
            selectedTarget=args.interactableObject.transform.GetComponent<VLabInteractable>();
            driver.SelectTarget(selectedTarget);
        }
        private void Release(SelectExitEventArgs args){selectedTarget=null;nextRotation=0;}
        private void Update()
        {
            if(driver==null || pointingHand==null)return;
            bool tracked=false;
            foreach(var device in UnityEngine.InputSystem.InputSystem.devices)
                if(device is UnityEngine.InputSystem.XR.XRController controller && controller.isTracked.isPressed)tracked=true;
            if(!tracked || driver.Input.HasRayProvider)
            {driver.NativePointer=null;pointingHand=null;driver.ReturnHeld();}
            if(selectedTarget!=null && !driver.Input.BlockExperimentInput && !driver.ExperimentInputSuspended && Time.timeScale>0)
            {
                float axis=rotation.ReadValue<Vector2>().x;
                if(Mathf.Abs(axis)<.35f)nextRotation=0;
                else if(Mathf.Abs(axis)>.65f && Time.unscaledTime>=nextRotation)
                {selectedTarget.Rotate(Mathf.Sign(axis));nextRotation=Time.unscaledTime+.3f;}
            }
        }
        private void OnDestroy()
        {
            foreach(var xr in interactables)if(xr!=null){xr.selectEntered.RemoveListener(Select);xr.selectExited.RemoveListener(Release);}
            rotation?.Dispose();
        }
    }
}
