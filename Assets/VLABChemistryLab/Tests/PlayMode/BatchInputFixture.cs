using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VLAB.ChemistryLab.Tests.PlayMode
{
    // Batch Unity has no focused Game view or guaranteed physical keyboard.
    // Route synthetic input to the player for these tests, then restore editor preferences.
    [SetUpFixture]
    public sealed class BatchInputFixture
    {
        private InputSettings.EditorInputBehaviorInPlayMode editorBehavior;
        private InputSettings.BackgroundBehavior background;
        private Keyboard keyboard;
        private Mouse mouse;
        [OneTimeSetUp] public void Setup()
        {
            editorBehavior=InputSystem.settings.editorInputBehaviorInPlayMode;
            background=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            if(Keyboard.current==null) keyboard=InputSystem.AddDevice<Keyboard>();
            if(Mouse.current==null) mouse=InputSystem.AddDevice<Mouse>();
        }
        [OneTimeTearDown] public void Restore()
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(mouse!=null)InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode=editorBehavior;
            InputSystem.settings.backgroundBehavior=background;
        }
    }
}
