#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Layouts;
using Vlab.Product;

// Uses actual Input System device/control state. Requires Play mode; no paired
// controller, phone, Android Bluetooth stack or custom BLE protocol is certified.
public static class GamepadIntegrationChecks
{
    static readonly List<string> checks=new List<string>();
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks.Add("PASS "+name);}
    static void State(Gamepad pad,GamepadState state){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();}
    [UnityEditor.MenuItem("VLAB/Checks/HID gamepad input")]
    public static void Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Run gamepad integration checks in Play mode.");
        checks.Clear();var go=new GameObject("VLAB gamepad input checks");Gamepad pad=null,hid=null;Mouse mouse=null;Touchscreen touch=null;bool passed=false;
        try
        {
            var camera=go.AddComponent<Camera>();camera.transform.rotation=Quaternion.identity;
            pad=InputSystem.AddDevice<Gamepad>();var provider=new GamepadControllerProvider(pad);
            Check(!GamepadControllerProvider.IsEligible(pad),"unlabelled virtual gamepad cannot steal production pointer");
            State(pad,new GamepadState());var frame=provider.Poll(camera,1);Check(!frame.held&&!frame.pressed,"neutral provider has no select");
            State(pad,new GamepadState().WithButton(GamepadButton.South));frame=provider.Poll(camera,1.02);Check(frame.pressed&&frame.held&&!frame.released,"A creates one select edge and hold");
            frame=provider.Poll(camera,1.04);Check(!frame.pressed&&frame.held,"held A does not repeat select edges");
            State(pad,new GamepadState());frame=provider.Poll(camera,1.06);Check(frame.released&&!frame.held,"A release ends hold");
            State(pad,new GamepadState{rightTrigger=1});frame=provider.Poll(camera,1.08);Check(frame.pressed&&frame.held,"right trigger selects");
            State(pad,new GamepadState());provider.Poll(camera,1.10);
            State(pad,new GamepadState().WithButton(GamepadButton.LeftShoulder));frame=provider.Poll(camera,1.12);Check(frame.grabPressed&&frame.held,"shoulder creates grip edge and hold");
            State(pad,new GamepadState());frame=provider.Poll(camera,1.14);Check(frame.released,"shoulder release ends grip");
            State(pad,new GamepadState().WithButton(GamepadButton.Start).WithButton(GamepadButton.Select));frame=provider.Poll(camera,1.16);Check(frame.menu&&frame.recenter,"Start pauses and Select recenters");
            frame=provider.Poll(camera,1.18);Check(!frame.menu&&!frame.recenter,"menu and recenter are edges");
            State(pad,new GamepadState().WithButton(GamepadButton.DpadUp));frame=provider.Poll(camera,1.20);Check(frame.adjustment==1,"dpad produces positive parameter adjustment");
            frame=provider.Poll(camera,1.22);Check(frame.adjustment==0,"dpad repeat is rate limited");
            State(pad,new GamepadState{leftStick=new Vector2(.8f,0)});var before=provider.CursorNormalized;provider.Poll(camera,1.24,false,false);Check(provider.CursorNormalized.x>before.x,"left stick moves ordinary cursor");
            State(pad,new GamepadState{rightStick=new Vector2(.8f,0)});var rotation=camera.transform.rotation;provider.Poll(camera,1.26,true,true);Check(Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"VR gaze ignores right-stick camera rotation");
            provider.Poll(camera,1.28,false,true);Check(Quaternion.Angle(rotation,camera.transform.rotation)>.01f,"ordinary right stick can look");
            State(pad,new GamepadState().WithButton(GamepadButton.South));provider.Poll(camera,1.30);InputSystem.RemoveDevice(pad);frame=provider.Poll(camera,1.32);Check(frame.lost&&frame.released&&!frame.held&&!frame.pressed,"disconnect emits loss and release without select");
            frame=provider.Poll(camera,1.34);Check(!frame.lost&&!frame.released,"disconnect loss and release occur once");pad=null;

            // Facade selection uses an explicitly described HID backend. The
            // custom matcher exists only in this isolated test and is removed.
            InputSystem.RegisterLayout<Gamepad>("VlabHidCheck",matches:new InputDeviceMatcher().WithInterface("HID").WithProduct("VLAB gamepad checks"));
            hid=(Gamepad)InputSystem.AddDevice(new InputDeviceDescription{interfaceName="HID",product="VLAB gamepad checks"});
            mouse=InputSystem.AddDevice<Mouse>();InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(100,100)});State(hid,new GamepadState());
            var input=go.AddComponent<VlabInput>();input.Adopt(camera);input.Read();Check(!input.UsingGamepad,"idle HID connection does not choose controller");
            State(hid,new GamepadState().WithButton(GamepadButton.South));frame=input.Read();Check(input.UsingGamepad&&frame.pressed&&input.ControllerPointerActive,"real HID activity selects controller facade");
            InputSystem.RemoveDevice(hid);hid=null;frame=input.Read();Check(frame.lost&&frame.released&&!frame.held&&!input.UsingGamepad,"facade disconnect safely returns to PC pointer");
            hid=(Gamepad)InputSystem.AddDevice(new InputDeviceDescription{interfaceName="HID",product="VLAB gamepad checks"});State(hid,new GamepadState());input.Read();State(hid,new GamepadState().WithButton(GamepadButton.South));input.Read();Check(input.UsingGamepad,"fresh controller action can choose HID again");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(120,100),delta=new Vector2(20,0)});InputSystem.Update();input.Read();Check(!input.UsingGamepad&&!input.ControllerPointerActive,"mouse activity restores ordinary mouse provider");
            touch=InputSystem.AddDevice<Touchscreen>();input.Gameplay=true;camera.transform.rotation=Quaternion.identity;
            Vector2 origin=new Vector2(Screen.width*.45f,Screen.height*.45f),moved=origin+new Vector2(80,25);
            Action<UnityEngine.InputSystem.TouchPhase,Vector2> send=(phase,position)=>{InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=phase,position=position});InputSystem.Update();};
            input.BlocksTouchLook=ray=>false;send(UnityEngine.InputSystem.TouchPhase.Began,origin);frame=input.Read();
            Check(!frame.pressed&&!frame.held,"empty-space phone look does not select or grab");rotation=camera.transform.rotation;
            send(UnityEngine.InputSystem.TouchPhase.Moved,origin+new Vector2(3,2));input.Read();Check(Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"small touch jitter does not turn the phone camera");
            send(UnityEngine.InputSystem.TouchPhase.Moved,moved);frame=input.Read();Check(Quaternion.Angle(rotation,camera.transform.rotation)>.01f,"phone empty-space swipe rotates flat camera");
            var lastRay=camera.ScreenPointToRay(moved);send(UnityEngine.InputSystem.TouchPhase.Ended,Vector2.zero);frame=input.Read();
            Check(!frame.pressed&&!frame.held,"ending a look gesture never selects equipment");
            input.BlocksTouchLook=ray=>true;send(UnityEngine.InputSystem.TouchPhase.Began,origin);input.Read();rotation=camera.transform.rotation;
            send(UnityEngine.InputSystem.TouchPhase.Moved,moved);input.Read();Check(Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"UI/equipment-blocked swipe does not rotate phone view");
            send(UnityEngine.InputSystem.TouchPhase.Canceled,Vector2.zero);frame=input.Read();Check(frame.released&&!frame.held,"cancelled touch releases a carried object");
            input.BlocksTouchLook=ray=>false;input.Gameplay=false;send(UnityEngine.InputSystem.TouchPhase.Began,origin);input.Read();rotation=camera.transform.rotation;
            send(UnityEngine.InputSystem.TouchPhase.Moved,moved);input.Read();Check(Quaternion.Angle(rotation,camera.transform.rotation)<.001f,"phone menus never rotate the lab camera");
            send(UnityEngine.InputSystem.TouchPhase.Ended,moved);input.Read();
            passed=true;
        }
        catch(Exception ex){checks.Add("FAIL "+ex);throw;}
        finally
        {
            if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);if(pad!=null&&pad.added)InputSystem.RemoveDevice(pad);if(hid!=null&&hid.added)InputSystem.RemoveDevice(hid);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveLayout("VlabHidCheck");UnityEngine.Object.DestroyImmediate(go);Directory.CreateDirectory("Evidence/Input");File.WriteAllLines("Evidence/Input/gamepad-integration.txt",checks);Debug.Log("HID input checks "+(passed?"PASS":"FAIL"));
        }
    }
}
#endif
