#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using Vlab.Product;

public static class ControllerBindingChecks
{
    [Serializable] sealed class Envelope {public int format=1;public string payload,sha256;}
    static readonly List<string> receipt=new List<string>();
    static void Verify(Action<string,bool> check,string name,bool passed){receipt.Add((passed?"PASS ":"FAIL ")+name);check(name,passed);if(!passed)throw new InvalidOperationException(name);}
    static GameSettings Custom()=>new GameSettings{controllerSelect=6,controllerGrip=10,controllerMenu=7,controllerRecenter=12,controllerBack=5};
    static void Write(string path,string payload){Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"profile.json"),JsonUtility.ToJson(new Envelope{payload=payload,sha256=ProfileStore.Hash(payload)}));}
    static void SaveReceipt(string file){Directory.CreateDirectory("Evidence/Input");File.WriteAllLines("Evidence/Input/"+file,new[]{"Standard HID mappings, native virtual-device/control state and isolated persistence only. No paired hardware, custom BLE packets, 6DoF or firmware calibration is certified."}.Concat(receipt));}
    public static void Core(LocalProfile original,Action<string,bool> check)
    {
        receipt.Clear();string originalJson=JsonUtility.ToJson(original);var defaults=new GameSettings();
        string folder=Path.Combine(Path.GetTempPath(),"VlabControllerBindings-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            defaults.Validate();Verify(check,"Default HID mapping preserves A/RT, shoulders, Start, Select and B",defaults.controllerSelect==0&&defaults.controllerGrip==1&&defaults.controllerMenu==2&&defaults.controllerRecenter==3&&defaults.controllerBack==5);
            Verify(check,"Physical overlap rejects A or RT reserved by composite Select",!ControllerBindings.CanAssign(defaults,ControllerAction.Menu,4)&&!ControllerBindings.CanAssign(defaults,ControllerAction.Menu,11));
            Verify(check,"Physical overlap rejects either shoulder reserved by composite Grip",!ControllerBindings.CanAssign(defaults,ControllerAction.Menu,8)&&!ControllerBindings.CanAssign(defaults,ControllerAction.Menu,9));
            Verify(check,"Back cannot share its B button with Select",!ControllerBindings.CanAssign(defaults,ControllerAction.Select,5));
            foreach(ControllerAction action in Enum.GetValues(typeof(ControllerAction)))
            {
                var settings=defaults.Copy();for(int i=0;i<ControllerBindings.OptionCount*2;i++){ControllerBindings.Cycle(settings,action);settings.Validate();}
                Verify(check,"Cycling skips conflicts and retains the other four actions: "+action,Enum.GetValues(typeof(ControllerAction)).Cast<ControllerAction>().Where(other=>other!=action).All(other=>ControllerBindings.Get(settings,other)==ControllerBindings.Get(defaults,other)));
                Verify(check,"Semantic binding label exists in Vietnamese and English: "+action,!string.IsNullOrWhiteSpace(ControllerBindings.ActionLabel(action,true))&&!string.IsNullOrWhiteSpace(ControllerBindings.ActionLabel(action,false)));
            }
            Verify(check,"Every allowed physical binding has bilingual text",Enumerable.Range(0,ControllerBindings.OptionCount).All(option=>!string.IsNullOrWhiteSpace(ControllerBindings.Label(option,true))&&!string.IsNullOrWhiteSpace(ControllerBindings.Label(option,false))));
            foreach(int invalid in new[]{-1,ControllerBindings.OptionCount,int.MaxValue}){var settings=defaults.Copy();settings.controllerSelect=invalid;bool rejected=false;try{settings.Validate();}catch(InvalidDataException){rejected=true;}Verify(check,"Out-of-range saved binding is rejected: "+invalid,rejected);}
            var overlapping=defaults.Copy();overlapping.controllerMenu=4;bool overlapRejected=false;try{overlapping.Validate();}catch(InvalidDataException){overlapRejected=true;}Verify(check,"Explicit overlapping saved binding is rejected",overlapRejected);
            var profile=JsonUtility.FromJson<LocalProfile>(originalJson);profile.settings=Custom();var store=new ProfileStore(folder);Verify(check,"Custom HID bindings save atomically",store.Save(profile,out _));
            var reopened=store.Load();Verify(check,"All five custom HID mappings reopen exactly",!reopened.writeProtected&&ControllerBindings.Equal(reopened.profile.settings,profile.settings));
            Verify(check,"Custom HID save retains notes, consent, checkpoint and scientific readings",JsonUtility.ToJson(reopened.profile)==JsonUtility.ToJson(profile));
            var primary=File.ReadAllBytes(Path.Combine(folder,"profile.json"));profile.settings.controllerBack=profile.settings.controllerSelect;Verify(check,"Conflicting save fails without replacing a valid primary",!store.Save(profile,out _)&&primary.SequenceEqual(File.ReadAllBytes(Path.Combine(folder,"profile.json"))));
            profile.settings=defaults;string payload=JsonUtility.ToJson(profile);foreach(string key in new[]{"controllerSelect","controllerGrip","controllerMenu","controllerRecenter","controllerBack"})payload=Regex.Replace(payload,"\""+key+"\"\\s*:\\s*\\d+,?","").Replace(",}","}");
            string legacyFolder=Path.Combine(folder,"legacy");Write(legacyFolder,payload);var legacy=new ProfileStore(legacyFolder).Load();Verify(check,"Profiles missing all binding fields receive original defaults",!legacy.writeProtected&&!legacy.recovered&&ControllerBindings.Equal(legacy.profile.settings,defaults));
            Verify(check,"Binding migration retains the complete legacy profile",JsonUtility.ToJson(legacy.profile)==JsonUtility.ToJson(profile));
            var invalidProfile=JsonUtility.FromJson<LocalProfile>(JsonUtility.ToJson(profile));invalidProfile.settings.controllerMenu=4;string invalidFolder=Path.Combine(folder,"invalid");Write(invalidFolder,JsonUtility.ToJson(invalidProfile));Verify(check,"Explicit invalid profile mapping is protected rather than defaulted",new ProfileStore(invalidFolder).Load().writeProtected);
            Verify(check,"Binding checks leave the caller's profile unchanged",JsonUtility.ToJson(original)==originalJson);
        }
        finally
        {
            SaveReceipt("controller-bindings-persistence.txt");string target=Path.GetFullPath(folder),parent=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(Directory.Exists(target)&&target.StartsWith(parent,StringComparison.OrdinalIgnoreCase))Directory.Delete(target,true);
        }
    }
    static void State(Gamepad pad,GamepadState state){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();}
    public static void Run(Action<string,bool> check)
    {
        receipt.Clear();var owner=new GameObject("Temporary HID binding checks");Gamepad pad=null,hid=null;Mouse mouse=null;const string layout="VlabBindingHidChecks";
        try
        {
            var camera=owner.AddComponent<Camera>();pad=InputSystem.AddDevice<Gamepad>();var provider=new GamepadControllerProvider(pad);double time=1;
            Func<ActionFrame> poll=()=>provider.Poll(camera,time+=.02,true,false);
            State(pad,new GamepadState());poll();State(pad,new GamepadState().WithButton(GamepadButton.South));var frame=poll();Verify(check,"Unchanged default A still selects and holds",frame.pressed&&frame.held&&!frame.menu&&!frame.back);
            frame=poll();Verify(check,"Default held select does not repeat",!frame.pressed&&frame.held);
            provider.ConfigureBindings(Custom());frame=poll();Verify(check,"Applying a remap releases an existing hold without a fresh action",frame.released&&!frame.held&&!frame.pressed&&!frame.menu&&!frame.back&&!frame.recenter);
            State(pad,new GamepadState().WithButton(GamepadButton.South).WithButton(GamepadButton.West));frame=poll();Verify(check,"Held old and newly mapped buttons remain suppressed until all release",!frame.pressed&&!frame.held&&!frame.menu&&!frame.back);
            State(pad,new GamepadState());poll();State(pad,new GamepadState().WithButton(GamepadButton.South));frame=poll();Verify(check,"Old select button is inactive after remapping",!frame.pressed&&!frame.held);
            State(pad,new GamepadState{rightTrigger=1});frame=poll();Verify(check,"Old trigger select is inactive after remapping",!frame.pressed&&!frame.held);
            State(pad,new GamepadState());poll();State(pad,new GamepadState().WithButton(GamepadButton.West));frame=poll();Verify(check,"Mapped West/X produces select edge and hold only",frame.pressed&&frame.held&&!frame.menu&&!frame.back&&!frame.grabPressed);
            frame=poll();Verify(check,"Mapped select remains held without repeated edge",frame.held&&!frame.pressed);State(pad,new GamepadState());frame=poll();Verify(check,"Mapped select releases cleanly",frame.released&&!frame.held);
            State(pad,new GamepadState{leftTrigger=.49f});frame=poll();Verify(check,"Mapped analogue trigger waits for the existing half-press threshold",!frame.grabPressed&&!frame.held);
            State(pad,new GamepadState{leftTrigger=.6f});frame=poll();Verify(check,"Mapped left trigger becomes binary grip hold",frame.grabPressed&&frame.held&&!frame.pressed);frame=poll();Verify(check,"Grip hold has one edge",frame.held&&!frame.grabPressed);State(pad,new GamepadState());frame=poll();Verify(check,"Grip release ends the held action",frame.released&&!frame.held);
            State(pad,new GamepadState().WithButton(GamepadButton.North));frame=poll();Verify(check,"Mapped North/Y opens Menu without selecting",frame.menu&&!frame.pressed&&!frame.back);frame=poll();Verify(check,"Mapped Menu does not repeat while held",!frame.menu);State(pad,new GamepadState());poll();
            State(pad,new GamepadState().WithButton(GamepadButton.LeftStick));frame=poll();Verify(check,"Mapped stick press recenters without selection",frame.recenter&&!frame.pressed&&!frame.menu);frame=poll();Verify(check,"Mapped Recenter is an edge",!frame.recenter);State(pad,new GamepadState());poll();
            State(pad,new GamepadState().WithButton(GamepadButton.East));frame=poll();Verify(check,"B produces Back independently of select and menu",frame.back&&!frame.pressed&&!frame.menu);frame=poll();Verify(check,"Back does not repeat while held",!frame.back);State(pad,new GamepadState());poll();
            State(pad,new GamepadState().WithButton(GamepadButton.West));poll();InputSystem.RemoveDevice(pad);pad=null;frame=poll();Verify(check,"Remapped controller disconnect releases and reports loss without actions",frame.lost&&frame.released&&!frame.held&&!frame.pressed&&!frame.menu&&!frame.back&&!frame.recenter);frame=poll();Verify(check,"Disconnect is emitted only once",!frame.lost&&!frame.released);

            InputSystem.RegisterLayout<Gamepad>(layout,matches:new InputDeviceMatcher().WithInterface("HID").WithProduct("VLAB binding checks"));hid=(Gamepad)InputSystem.AddDevice(new InputDeviceDescription{interfaceName="HID",product="VLAB binding checks"});
            mouse=InputSystem.AddDevice<Mouse>();InputSystem.QueueStateEvent(mouse,new MouseState());State(hid,new GamepadState());var input=owner.AddComponent<VlabInput>();input.Adopt(camera);input.ConfigureControllerBindings(Custom());input.Read();
            State(hid,new GamepadState().WithButton(GamepadButton.West));frame=input.Read();Verify(check,"Fresh mapped X activity discovers actual HID backend and uses configured select",input.UsingGamepad&&frame.pressed&&frame.held);
            var changed=Custom();changed.controllerSelect=7;changed.controllerMenu=2;input.ConfigureControllerBindings(changed);frame=input.Read();Verify(check,"Live facade remapping releases held equipment without phantom navigation",frame.released&&!frame.held&&!frame.pressed&&!frame.back&&!frame.menu);
            State(hid,new GamepadState());input.Read();State(hid,new GamepadState().WithButton(GamepadButton.North));frame=input.Read();Verify(check,"Live facade adopts the new mapping after neutral state",frame.pressed&&frame.held);
            InputSystem.RemoveDevice(hid);hid=null;frame=input.Read();Verify(check,"Facade disconnect preserves safe release with configurable bindings",frame.lost&&frame.released&&!frame.held&&!frame.pressed&&!frame.back&&!frame.menu);
        }
        finally{if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(hid!=null&&hid.added)InputSystem.RemoveDevice(hid);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);InputSystem.RemoveLayout(layout);UnityEngine.Object.DestroyImmediate(owner);SaveReceipt("controller-bindings-native.txt");}
    }
}
#endif
