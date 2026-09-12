using TMPro;
using UnityEngine;

namespace VLAB.MainMenu
{
    /// <summary>Water ball-and-stick lesson. Geometry reference: OpenStax Chemistry 2e, 7.6.</summary>
    public sealed class VLabMoleculeActivity : VLabActivity
    {
        private readonly int[] occupants = { -1, -1, -1 };
        private readonly GameObject[] atoms = new GameObject[4];
        private readonly GameObject[] sockets = new GameObject[3];
        private readonly GameObject[] bonds = new GameObject[2];
        private readonly TMP_Text[] atomLabels = new TMP_Text[4];
        private readonly Vector3[] homes = new Vector3[4];
        private TMP_Text angleLabel;
        private bool built;
        private bool bent = true;
        private static readonly string[] Symbols = { "H", "H", "O", "C" };
        private static readonly Color[] Colors = { Color.white, Color.white, new Color(.95f,.24f,.3f), new Color(.24f,.29f,.34f) };
        private static readonly Vector3 Center = new Vector3(.24f,.62f,.07f);

        public override string Title => "Lắp phân tử nước";
        public override string Objective => "Lắp 2 nguyên tử H và 1 nguyên tử O, chọn hình học đúng rồi kiểm tra cấu trúc H₂O.";
        public override string Theory => "O liên kết với hai H và có hai cặp electron không liên kết. Phân tử nước gấp khúc, góc H–O–H xấp xỉ 104,5°. Màu và kích thước quả cầu là quy ước mô hình.";
        public int SelectedAtom { get; private set; } = -1;
        public int PlacedCount { get { int count=0; foreach(var atom in occupants) if(atom>=0) count++; return count; } }
        public float BondAngle => Vector3.Angle(SocketPosition(1)-Center, SocketPosition(2)-Center);

        public override void Build()
        {
            if(built) return;
            built=true;
            Part("AtomTray",PrimitiveType.Cube,new Vector3(-.8f,.065f,0),new Vector3(.5f,.05f,.34f),new Color(.08f,.16f,.22f));
            Part("AtomTrayUpperShelf",PrimitiveType.Cube,new Vector3(-.8f,.36f,.04f),new Vector3(.5f,.04f,.30f),new Color(.08f,.16f,.22f));
            for(int i=0;i<4;i++)
            {
                int index=i;
                homes[i]=new Vector3(-.93f+(i%2)*.26f,.16f+(i/2)*.31f,0);
                atoms[i]=Part("Atom_"+i+"_"+Symbols[i],PrimitiveType.Sphere,homes[i],Vector3.one*(i==2?.16f:.13f),Colors[i],()=>SelectAtom(index));
                atoms[i].GetComponent<SphereCollider>().radius=.7f;
                atomLabels[i]=Label("AtomLabel_"+i,Symbols[i],homes[i]+new Vector3(0,.12f,-.015f),.065f);
            }
            for(int i=0;i<3;i++)
            {
                int index=i;
                sockets[i]=Part("WaterSocket_"+i,PrimitiveType.Sphere,SocketPosition(i),Vector3.one*.22f,new Color(.12f,.34f,.4f),()=>SelectSocket(index));
            }
            for(int i=0;i<2;i++)
            {
                bonds[i]=Part("WaterBond_"+i,PrimitiveType.Cylinder,Center,Vector3.one,new Color(.58f,.75f,.8f));
                bonds[i].GetComponent<Collider>().enabled=false;
            }
            Part("WaterBentGeometry",PrimitiveType.Cube,new Vector3(-.23f,.09f,-.29f),new Vector3(.33f,.12f,.16f),new Color(.12f,.32f,.36f),()=>SetBentGeometry(true));
            Part("WaterLinearGeometry",PrimitiveType.Cube,new Vector3(.17f,.09f,-.29f),new Vector3(.33f,.12f,.16f),new Color(.12f,.32f,.36f),()=>SetBentGeometry(false));
            Part("WaterValidate",PrimitiveType.Cube,new Vector3(.67f,.09f,-.29f),new Vector3(.48f,.12f,.16f),new Color(.16f,.49f,.39f),()=>ValidateStructure());
            Label("BentLabel","Gấp khúc",new Vector3(-.23f,.2f,-.31f),.045f);
            Label("LinearLabel","Thẳng",new Vector3(.17f,.2f,-.31f),.045f);
            Label("ValidateLabel","Kiểm tra",new Vector3(.67f,.2f,-.31f),.045f);
            angleLabel=Label("WaterAngle","",new Vector3(.25f,.87f,.05f),.06f);
            ResetActivity();
        }

        private Vector3 SocketPosition(int index)
        {
            if(index==0) return Center;
            float half=(bent?104.5f:180f)*.5f*Mathf.Deg2Rad;
            return Center+new Vector3((index==1?-1:1)*Mathf.Sin(half)*.38f,-Mathf.Cos(half)*.38f,0);
        }

        public void SelectAtom(int index)
        {
            if(index<0 || index>=atoms.Length) return;
            for(int i=0;i<occupants.Length;i++)
                if(occupants[i]==index) { SelectSocket(i); return; }
            SelectedAtom=SelectedAtom==index?-1:index;
            Result="";
            Instruction=SelectedAtom<0?"Chọn nguyên tử trong khay, rồi chọn vị trí lắp.":"Đã chọn "+Symbols[index]+". Chọn vị trí trống trên mô hình để lắp.";
            Refresh(); NotifyChanged();
        }

        public bool SelectSocket(int index)
        {
            if(index<0 || index>=occupants.Length) return false;
            if(occupants[index]>=0)
            {
                occupants[index]=-1; SelectedAtom=-1; Completed=false;
                Instruction="Đã tháo nguyên tử về khay. Chọn nguyên tử để lắp lại.";
                Result=""; Refresh(); NotifyChanged(); return true;
            }
            if(SelectedAtom<0) return Reject("Chọn một nguyên tử trong khay trước khi chọn vị trí lắp.");
            string expected=index==0?"O":"H";
            if(Symbols[SelectedAtom]!=expected) return Reject("Vị trí này cần "+expected+". H₂O có O ở giữa và hai H ở hai đầu; C không thuộc phân tử nước.");
            occupants[index]=SelectedAtom; SelectedAtom=-1; Completed=false; Result="";
            Instruction=PlacedCount==3?"Đã lắp đủ H₂O. Chọn hình học rồi bấm Kiểm tra.":"Tiếp tục chọn nguyên tử rồi chọn vị trí trống. Chọn nguyên tử đã lắp để tháo.";
            Refresh(); NotifyChanged(); return true;
        }

        public void SetBentGeometry(bool useBent)
        {
            bent=useBent; Completed=false; Result="";
            Instruction="Quan sát góc H–O–H, sau đó chọn Kiểm tra để giải thích cấu trúc.";
            Refresh(); NotifyChanged();
        }

        public bool ValidateStructure()
        {
            if(PlacedCount!=3) return Reject("Chưa đủ cấu trúc: cần 1 O ở giữa và 2 H ở hai đầu.");
            if(Symbols[occupants[0]]!="O" || Symbols[occupants[1]]!="H" || Symbols[occupants[2]]!="H") return Reject("Kiểm tra lại nguyên tố và vị trí liên kết O–H.");
            if(Mathf.Abs(BondAngle-104.5f)>.5f) return Reject("Đủ nguyên tử nhưng hình học chưa đúng: H₂O gấp khúc khoảng 104,5°, không thẳng 180°. Hai cặp electron không liên kết trên O làm giảm góc liên kết.");
            Completed=true;
            Result="Chính xác: H₂O có hai liên kết O–H, hình gấp khúc với góc 104,5°. Các lưỡng cực liên kết không triệt tiêu nên phân tử nước phân cực.";
            Instruction="Chọn nguyên tử đã lắp để tháo, hoặc Đặt lại để thực hành từ đầu.";
            NotifyChanged(); return true;
        }

        private bool Reject(string message) { Completed=false; Result=message; NotifyChanged(); return false; }
        public override void ResetActivity()
        {
            for(int i=0;i<occupants.Length;i++) occupants[i]=-1;
            SelectedAtom=-1; bent=true; Completed=false; Result="";
            Instruction="Chọn một nguyên tử trong khay, rồi chọn vị trí trống trên mô hình. O ở giữa; H ở hai đầu.";
            Refresh(); NotifyChanged();
        }

        private void Refresh()
        {
            if(!built) return;
            for(int i=0;i<atoms.Length;i++)
            {
                Vector3 position=homes[i];
                for(int j=0;j<occupants.Length;j++) if(occupants[j]==i) position=SocketPosition(j);
                atoms[i].transform.localPosition=position+(SelectedAtom==i?Vector3.up*.1f:Vector3.zero);
                atoms[i].transform.localScale=Vector3.one*(i==2?.16f:.13f)*(SelectedAtom==i?1.2f:1);
                atomLabels[i].transform.localPosition=atoms[i].transform.localPosition+new Vector3(0,.12f,-.025f);
            }
            for(int i=0;i<sockets.Length;i++) { sockets[i].transform.localPosition=SocketPosition(i); sockets[i].SetActive(occupants[i]<0); }
            for(int i=0;i<bonds.Length;i++)
            {
                Vector3 end=SocketPosition(i+1),delta=end-Center;
                bonds[i].transform.localPosition=(Center+end)*.5f;
                bonds[i].transform.localRotation=Quaternion.FromToRotation(Vector3.up,delta);
                bonds[i].transform.localScale=new Vector3(.035f,delta.magnitude*.5f,.035f);
                bonds[i].SetActive(occupants[0]>=0 && occupants[i+1]>=0);
            }
            angleLabel.text="H–O–H   "+(bent?"104,5°":"180°");
        }
    }
}
