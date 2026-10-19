using System;
using UnityEngine;

public sealed class VlabCableReview : MonoBehaviour
{
    [SerializeField] private VlabCable cable;
    [SerializeField] private VlabElectricalTerminal terminalA,terminalB;
    [SerializeField] private Material red,black;
    private string note="Hai ổ cắm là đồ gá kiểm tra.";
    private bool darkCable;
    public VlabElectricalTerminal A=>terminalA;
    public VlabElectricalTerminal B=>terminalB;
    private void Start()
    {
        for(int end=0;end<2;end++)if(cable.Terminal(end)==null)
        {
            var plug=cable.Plug(end);var terminal=end==0?terminalA:terminalB;
            cable.MoveFreeEnd(end,plug.position+terminal.Axis*.020f,plug.rotation);
        }
    }
    public void SetSpacing(float distance)
    {
        if(distance!=.1f&&distance!=.2f&&distance!=.3f)throw new ArgumentOutOfRangeException(nameof(distance));
        for(int end=0;end<2;end++)
        {
            var terminal=end==0?terminalA:terminalB;var position=terminal.transform.position;position.x=(end==0?-1:1)*distance/2;terminal.transform.position=position;
            if(cable.Terminal(end)==null)
            {
                var plug=cable.Plug(end);var target=plug.position;target.x=position.x;
                if(!cable.MoveFreeEnd(end,target,plug.rotation))throw new InvalidOperationException("Review route rejected.");
            }
        }
        cable.SyncConnections();
    }
    private void Toggle(int end)
    {
        if(cable.Terminal(end)!=null){cable.Disconnect(end);note="Đã tháo "+(end==0?"A":"B")+". Chạm dây không tự nối điện.";}
        else note=cable.Connect(end,end==0?terminalA:terminalB)?"Đã gắn đầu "+(end==0?"A":"B")+".":"Không gắn được đầu nối.";
    }
    private void OnGUI()
    {
        GUI.Box(new Rect(12,12,345,292),"");
        GUI.Label(new Rect(24,22,320,24),"DÂY ĐIỆN · Đầu cắm Ø4 mm");
        GUI.Label(new Rect(24,50,320,24),"A ↔ B: "+(VlabCable.AreConnected(terminalA,terminalB,new[]{cable})?"LIÊN THÔNG":"HỞ"));
        GUI.Label(new Rect(24,76,320,24),$"Đường dây {cable.RouteLength*1000:F0} mm · {(cable.RouteValid?"Hợp lệ":"Không hợp lệ")}");
        if(GUI.Button(new Rect(24,112,150,30),"Gắn / Tháo A"))Toggle(0);
        if(GUI.Button(new Rect(184,112,150,30),"Gắn / Tháo B"))Toggle(1);
        if(GUI.Button(new Rect(24,150,96,30),"100 mm"))SetSpacing(.1f);
        if(GUI.Button(new Rect(130,150,96,30),"200 mm"))SetSpacing(.2f);
        if(GUI.Button(new Rect(236,150,98,30),"300 mm"))SetSpacing(.3f);
        if(GUI.Button(new Rect(24,188,310,30),"Vỏ dây đỏ / đen"))
        {
            darkCable=!darkCable;
            foreach(var renderer in cable.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)if(materials[i]==red||materials[i]==black)materials[i]=darkCable?black:red;
                renderer.sharedMaterials=materials;
            }
        }
        GUI.Label(new Rect(24,226,310,36),note);
        GUI.Label(new Rect(24,266,310,24),"Chưa nối thiết bị và bộ giải dòng/áp.");
    }
}
