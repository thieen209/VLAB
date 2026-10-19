using UnityEngine;

public class LabFixture : MonoBehaviour
{
    public enum Action { Hinge, Drawer, Tap, Drain, Shade, Lamp }
    public Action action;
    public string label;
    public Transform movingPart;
    public LabSink sink; public LabShade shade; public Light lamp;
    public Vector3 openOffset, openAngles;
    public bool isOpen;
    public LabFixture[] requiredOpen = new LabFixture[0], closeBeforeClosing = new LabFixture[0];
    Vector3 closedPosition;
    Quaternion closedRotation;
    bool initialized;
    void Awake(){Initialize();}
    public void Initialize(){if(initialized||!movingPart)return;closedPosition=movingPart.localPosition;closedRotation=movingPart.localRotation;initialized=true;}
    public bool Settled(bool open){Initialize();return isOpen==open&&(!movingPart||(Vector3.Distance(movingPart.localPosition,closedPosition+(open?openOffset:Vector3.zero))<.001f&&Quaternion.Angle(movingPart.localRotation,closedRotation*Quaternion.Euler(open?openAngles:Vector3.zero))<.1f));}
    string BlockReason { get { if(!isOpen&&requiredOpen!=null)foreach(var f in requiredOpen)if(f&&!f.Settled(true))return "Open cabinet doors fully first";if(isOpen&&closeBeforeClosing!=null)foreach(var f in closeBeforeClosing)if(f&&!f.Settled(false))return "Close inner drawers first";return ""; } }
    public string Hint => BlockReason.Length>0?BlockReason:label;
    public void Activate(){Initialize();if(BlockReason.Length>0)return;if(action==Action.Tap)sink.ToggleTap();else if(action==Action.Drain)sink.ToggleDrain();else if(action==Action.Shade)shade.Toggle();else if(action==Action.Lamp)lamp.enabled=!lamp.enabled;else isOpen=!isOpen;}
    public void Animate(float seconds)
    {
        Initialize();if(!movingPart)return;
        movingPart.localPosition=Vector3.MoveTowards(movingPart.localPosition,closedPosition+(isOpen?openOffset:Vector3.zero),seconds*.45f);
        movingPart.localRotation=Quaternion.RotateTowards(movingPart.localRotation,closedRotation*Quaternion.Euler(isOpen?openAngles:Vector3.zero),seconds*110);
    }
    void Update(){Animate(Time.deltaTime);}
}


