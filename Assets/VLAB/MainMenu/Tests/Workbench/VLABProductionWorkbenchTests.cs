using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using VLAB.Core.Input;

namespace VLAB.MainMenu.Tests
{
    public sealed class VLABProductionWorkbenchTests
    {
        private readonly List<GameObject> owned = new List<GameObject>();
        private VLabActivityWorkbench workbench;
        private Camera camera;
        private float previousTimeScale;
        private InputManager input;
        private IVLABInputProvider previousProvider;
        private bool previousInputBlock;

        [SetUp] public void Setup()
        {
            previousTimeScale=Time.timeScale; Time.timeScale=1;
            if(Object.FindAnyObjectByType<XRInteractionManager>()==null)
                Own(new GameObject("Workbench owned test XR manager")).AddComponent<XRInteractionManager>();
            camera=Camera.main;
            if(camera==null)
            {
                var cameraObject=Own(new GameObject("Workbench test camera"));
                cameraObject.tag="MainCamera";
                camera=cameraObject.AddComponent<Camera>();
                camera.transform.position=new Vector3(0,1.7f,-2.4f);
            }
            workbench=Own(new GameObject("Workbench test host")).AddComponent<VLabActivityWorkbench>();
        }

        private GameObject Own(GameObject value){owned.Add(value);return value;}

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(workbench!=null)workbench.Close();
            if(input!=null){input.SetProvider(previousProvider);input.BlockExperimentInput=previousInputBlock;}
            for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)Object.Destroy(owned[i]);
            owned.Clear();
            Time.timeScale=previousTimeScale;
            yield return null;
        }

        [UnityTest] public IEnumerator OpeningFreezesOriginalPhysicsAndCloseRestoresMotionAndRecovery()
        {
            var apparatus=Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            apparatus.transform.position=new Vector3(50,12,50);
            var body=apparatus.AddComponent<Rigidbody>();
            body.useGravity=true;
            body.linearVelocity=new Vector3(.3f,.5f,.2f);
            body.angularVelocity=new Vector3(.1f,.2f,.3f);
            apparatus.AddComponent<XRGrabInteractable>();
            var recovery=apparatus.AddComponent<VLAB.ChemistryLab.Interaction.LabGrabRecovery>();
            var startPosition=body.position;
            var startRotation=body.rotation;
            var startVelocity=body.linearVelocity;
            var startAngular=body.angularVelocity;
            workbench.Open(nameof(VLabMoleculeActivity));
            Assert.That(body.isKinematic,Is.True);
            Assert.That(apparatus.GetComponent<Collider>().enabled,Is.False);
            Assert.That(recovery.enabled,Is.False);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(body.position,Is.EqualTo(startPosition));
            workbench.Close();
            Assert.That(body.isKinematic,Is.False);
            Assert.That(body.position,Is.EqualTo(startPosition));
            Assert.That(body.rotation,Is.EqualTo(startRotation));
            Assert.That(body.linearVelocity,Is.EqualTo(startVelocity));
            Assert.That(body.angularVelocity,Is.EqualTo(startAngular));
            Assert.That(apparatus.GetComponent<Collider>().enabled,Is.True);
            Assert.That(recovery.enabled,Is.True);
        }

        [Test] public void RepeatedOpenClosePreservesControllerVisualsAndOriginallyDisabledObjects()
        {
            var controller=Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            controller.name="Controller visual under camera";
            controller.transform.SetParent(camera.transform,false);
            var original=Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            original.GetComponent<Renderer>().enabled=false;
            original.GetComponent<Collider>().enabled=false;
            var staticBody=original.AddComponent<Rigidbody>();
            staticBody.isKinematic=true;
            foreach(var activityType in new[]{nameof(VLabCellActivity),nameof(VLabMoleculeActivity),nameof(VLabQualitativeActivity)})
            {
                workbench.Open(activityType);
                Assert.That(controller.GetComponent<Renderer>().enabled,Is.True,"Tracked controller visuals must remain visible.");
                Assert.That(controller.GetComponent<Collider>().enabled,Is.True);
                workbench.Close();workbench.Close();
                Assert.That(workbench.Active,Is.Null);
                Assert.That(original.GetComponent<Renderer>().enabled,Is.False);
                Assert.That(original.GetComponent<Collider>().enabled,Is.False);
                Assert.That(staticBody.isKinematic,Is.True);
            }
        }

        private sealed class ResetProvider : IVLABInputProvider
        {
            public string ProviderName=>"Workbench reset test";
            public bool InteractionPressed=>false;
            public bool ResetPressed { get; set; }
            public VLABInputState ReadState()=>default;
        }

        [Test] public void SharedResetInputTargetsActiveActivityOnceAndStopsAfterClose()
        {
            input=Object.FindAnyObjectByType<InputManager>();
            if(input==null)input=Own(new GameObject("Workbench input test")).AddComponent<InputManager>();
            previousProvider=input.Provider;previousInputBlock=input.BlockExperimentInput;
            input.BlockExperimentInput=false;
            var provider=new ResetProvider();input.SetProvider(provider);
            workbench.Open(nameof(VLabMoleculeActivity));
            var water=(VLabMoleculeActivity)workbench.Active;
            water.SelectAtom(2);water.SelectSocket(0);
            int changes=0;water.Changed+=()=>changes++;
            provider.ResetPressed=true;input.RefreshInput();
            Assert.That(water.PlacedCount,Is.Zero);
            Assert.That(changes,Is.EqualTo(1));
            input.RefreshInput();
            Assert.That(changes,Is.EqualTo(1),"A held reset must not repeat every frame.");
            provider.ResetPressed=false;input.RefreshInput();
            workbench.Close();
            provider.ResetPressed=true;input.RefreshInput();
            Assert.That(changes,Is.EqualTo(1),"Closing must remove the shared reset subscription.");
        }

        [Test] public void VesselHitVolumesAreRegisteredWithTheNativeXrManager()
        {
            workbench.Open(nameof(VLabQualitativeActivity));
            foreach(var vessel in workbench.Active.GetComponentsInChildren<XRSimpleInteractable>())
            {
                var hit=vessel.GetComponent<BoxCollider>();
                Assert.That(hit,Is.Not.Null,vessel.name);
                Assert.That(hit.enabled,Is.True);
                Assert.That(vessel.colliders,Does.Contain(hit));
                Assert.That(vessel.interactionManager,Is.Not.Null);
                Assert.That(vessel.interactionManager.TryGetInteractableForCollider(hit,out var registered),Is.True);
                Assert.That(registered,Is.SameAs(vessel));
            }
        }

        [Test] public void CellBoardShowsShortSynopsisAndOffersFullTheory()
        {
            workbench.Open(nameof(VLabCellActivity));
            var board=workbench.transform.Find("VLAB Learning Workbench/VLAB Activity Instructions");
            Assert.That(board,Is.Not.Null);
            var synopsis=board.Find("Theory").GetComponent<Text>().text;
            Assert.That(synopsis.Length,Is.LessThan(155));
            Assert.That(synopsis,Does.Contain("quy ước"));
            Assert.That(board.Find("ActivityFullTheory").GetComponent<Button>(),Is.Not.Null);
            Assert.That(workbench.Active.Theory.Length,Is.GreaterThan(synopsis.Length));
        }

        [Test] public void PitchedEntryFramesBoardAndApparatusWithoutMovingCamera()
        {
            var savedPosition=camera.transform.position;var savedRotation=camera.transform.rotation;
            try
            {
                camera.transform.rotation=Quaternion.Euler(30,20,0);
                var entryRotation=camera.transform.rotation;
                workbench.Open(nameof(VLabCellActivity));
                Assert.That(camera.transform.position,Is.EqualTo(savedPosition));
                Assert.That(camera.transform.rotation,Is.EqualTo(entryRotation));
                var station=workbench.transform.Find("VLAB Learning Workbench");
                var board=station.Find("VLAB Activity Instructions").GetComponent<RectTransform>();
                var corners=new Vector3[4];board.GetWorldCorners(corners);
                foreach(var corner in corners)AssertInsideView(camera.WorldToViewportPoint(corner));
                foreach(var point in new[]{new Vector3(-.4f,0,-.4f),new Vector3(1.7f,1.02f,.4f)})
                    AssertInsideView(camera.WorldToViewportPoint(station.TransformPoint(point)));
            }
            finally { camera.transform.SetPositionAndRotation(savedPosition,savedRotation); }
        }

        private static void AssertInsideView(Vector3 point)
        {
            Assert.That(point.z,Is.GreaterThan(0));
            Assert.That(point.x,Is.InRange(.02f,.98f));
            Assert.That(point.y,Is.InRange(.02f,.98f));
        }
    }
}
