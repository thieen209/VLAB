using TMPro;
using UnityEngine;

namespace VLAB.MainMenu
{
    /// <summary>Finite-dose precipitation and negative-control lesson; OpenStax Chemistry 2e, 4.2.</summary>
    public sealed class VLabQualitativeActivity : VLabActivity
    {
        private readonly int[] reagentVolumes = { 6, 6, 6 };
        private readonly int[] sampleVolumes = { 4, 4 };
        private readonly GameObject[] liquids = new GameObject[5];
        private readonly TMP_Text[] volumeLabels = new TMP_Text[5];
        private readonly Vector3[] positions = new Vector3[5];
        private readonly GameObject[] selectionRings = new GameObject[3];
        private readonly GameObject[] precipitate = new GameObject[12];
        private GameObject transferDrop;
        private Vector3 transferStart;
        private Vector3 transferEnd;
        private float transferProgress = 1;
        private TMP_Text observation;
        private bool built;
        private static readonly string[] Reagents = { "AgNO₃", "NaNO₃", "H₂O" };
        private const int Dose = 2;
        private const int Capacity = 10;

        public override string Title => "Nhận biết kết tủa";
        public override string Objective => "Chọn thuốc thử tạo kết tủa với NaCl, rồi so sánh cùng thuốc thử trên mẫu đối chứng NaNO₃.";
        public override string Theory => "Trong các mẫu đã biết của bài này, Ag⁺ + Cl⁻ → AgCl↓ trắng. Na⁺ và NO₃⁻ là ion không tham gia phản ứng. Đối chứng NaNO₃ giúp phân biệt tạo kết tủa với chỉ pha trộn dung dịch. Đây là mô hình định tính, không mô phỏng cân bằng nồng độ.";
        public int SelectedReagent { get; private set; } = -1;
        public bool HasPrecipitate { get; private set; }
        public bool NegativeControlObserved { get; private set; }
        public int ReagentVolume(int index) => index>=0 && index<3 ? reagentVolumes[index] : 0;
        public int SampleVolume(int index) => index>=0 && index<2 ? sampleVolumes[index] : 0;

        public override void Build()
        {
            if(built) return;
            built=true;
            Part("ReagentRack",PrimitiveType.Cube,new Vector3(-.53f,.025f,.02f),new Vector3(.82f,.04f,.68f),new Color(.08f,.16f,.22f));
            for(int i=0;i<3;i++)
            {
                int index=i;
                positions[i]=new Vector3(-.78f+i*.26f,.02f,.2f);
                CreateVessel(i,Reagents[i],()=>SelectReagent(index));
                selectionRings[i]=Part("ReagentSelected_"+i,PrimitiveType.Cylinder,positions[i]+new Vector3(0,.01f,0),new Vector3(.22f,.014f,.22f),new Color(.35f,.88f,.73f));
                selectionRings[i].GetComponent<Collider>().enabled=false;
            }
            for(int i=0;i<2;i++)
            {
                int index=i;
                positions[i+3]=new Vector3(.25f+i*.51f,.02f,0);
                CreateVessel(i+3,i==0?"Mẫu NaCl":"Đối chứng NaNO₃",()=>TransferTo(index));
            }
            for(int i=0;i<precipitate.Length;i++)
            {
                float x=(i%4-1.5f)*.035f;
                float y=.055f+(i/4)*.023f;
                precipitate[i]=Part("AgClParticle_"+i,PrimitiveType.Sphere,positions[3]+new Vector3(x,y,-.086f),Vector3.one*(.024f+(i%3)*.006f),Color.white);
                precipitate[i].GetComponent<Collider>().enabled=false;
            }
            transferDrop=Part("ControlledTransferDrop",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.035f,new Color(.77f,.92f,.96f));
            transferDrop.GetComponent<Collider>().enabled=false;
            observation=Label("PrecipitateObservation","",new Vector3(.51f,.55f,.03f),.047f);
            Label("ReagentHeading","THUỐC THỬ",new Vector3(-.53f,.55f,.19f),.05f);
            Label("VolumeLegend","Mỗi lần chọn: 2 đơn vị mô phỏng",new Vector3(0,.07f,-.31f),.04f);
            ResetActivity();
        }

        private void CreateVessel(int index,string title,System.Action action)
        {
            Vector3 position=positions[index];
            Color glass=new Color(.35f,.53f,.58f);
            var bottom=Part("Vessel_"+index,PrimitiveType.Cylinder,position+Vector3.up*.022f,new Vector3(.2f,.017f,.2f),glass);
            // Configure the generous hit volume before XRI caches this vessel's colliders.
            foreach(var collider in bottom.GetComponents<Collider>()) collider.enabled=false;
            var target=bottom.AddComponent<BoxCollider>();
            target.center=new Vector3(0,4.5f,0); target.size=new Vector3(1.3f,9,1.3f);
            MakeInteractive(bottom,action);
            for(int j=0;j<4;j++)
            {
                float angle=(45+j*90)*Mathf.Deg2Rad;
                var rod=Part("VesselFrame_"+index+"_"+j,PrimitiveType.Cylinder,position+new Vector3(Mathf.Sin(angle)*.094f,.17f,Mathf.Cos(angle)*.094f),new Vector3(.009f,.15f,.009f),glass);
                rod.GetComponent<Collider>().enabled=false;
            }
            var rim=Part("VesselRim_"+index,PrimitiveType.Cylinder,position+Vector3.up*.32f,new Vector3(.21f,.006f,.21f),glass);
            // A thin top disc is an intentionally simplified rim in this schematic apparatus.
            rim.GetComponent<Collider>().enabled=false;
            liquids[index]=Part("Liquid_"+index,PrimitiveType.Cylinder,position+Vector3.up*.1f,new Vector3(.175f,.07f,.175f),new Color(.66f,.78f,.81f));
            liquids[index].GetComponent<Collider>().enabled=false;
            Label("VesselLabel_"+index,title,position+new Vector3(0,.4f,-.035f),index<3?.043f:.046f);
            volumeLabels[index]=Label("VesselVolume_"+index,"",position+new Vector3(0,.055f,-.13f),.035f);
        }

        public void SelectReagent(int index)
        {
            if(index<0 || index>=reagentVolumes.Length) return;
            SelectedReagent=SelectedReagent==index?-1:index;
            Result="";
            Instruction=SelectedReagent<0?"Chọn thuốc thử, sau đó chọn cốc mẫu để chuyển một lượng cố định.":"Đã chọn "+Reagents[index]+". Chọn mẫu NaCl hoặc mẫu đối chứng NaNO₃ để chuyển 2 đơn vị.";
            Refresh(); NotifyChanged();
        }

        public bool TransferTo(int sample)
        {
            if(sample<0 || sample>=sampleVolumes.Length) return false;
            if(SelectedReagent<0) return Reject("Chưa chọn thuốc thử. Chọn một lọ ở giá bên trái trước.");
            int reagent=SelectedReagent;
            if(reagentVolumes[reagent]<Dose) return Reject("Lọ đã hết lượng mô phỏng. Chọn Đặt lại để phục hồi mẫu và thuốc thử.");
            if(sampleVolumes[sample]+Dose>Capacity) return Reject("Cốc đã đạt giới hạn 10 đơn vị. Không chuyển thêm; chọn Đặt lại để làm lại với mẫu mới.");
            reagentVolumes[reagent]-=Dose;
            sampleVolumes[sample]+=Dose;
            SelectedReagent=-1;
            if(reagent==0 && sample==0)
            {
                HasPrecipitate=true;
                Result="Mẫu NaCl xuất hiện chất rắn trắng AgCl: Ag⁺ + Cl⁻ → AgCl↓. Đây là kết tủa, không phải bọt khí.";
            }
            else if(reagent==0)
            {
                NegativeControlObserved=true;
                Result="Đối chứng NaNO₃ vẫn không có kết tủa: Ag⁺ và NO₃⁻ cùng tồn tại trong dung dịch. Mẫu này không có Cl⁻ để tạo AgCl.";
            }
            else
            {
                Result=HasPrecipitate && sample==0
                    ? "Kết tủa AgCl từ lần trước vẫn còn. Lần thêm này không tạo phản ứng kết tủa mới."
                    : "Không xuất hiện kết tủa. "+Reagents[reagent]+" không cung cấp Ag⁺; hãy chọn thuốc thử có ion bạc để kiểm tra hai mẫu.";
            }
            Completed=HasPrecipitate && NegativeControlObserved;
            Instruction=Completed?"Đã so sánh đủ hai mẫu: NaCl tạo AgCl trắng, đối chứng NaNO₃ không tạo kết tủa. Đặt lại để thử lại.":HasPrecipitate?"Tiếp tục: chọn cùng thuốc thử AgNO₃ cho mẫu đối chứng NaNO₃.":"Chọn thuốc thử có ion Ag⁺, rồi chọn mẫu NaCl. So sánh với mẫu đối chứng.";
            if(built)
            {
                transferStart=positions[reagent]+Vector3.up*.37f;
                transferEnd=positions[sample+3]+Vector3.up*.21f;
                transferProgress=0;
                transferDrop.transform.localPosition=transferStart;
                transferDrop.SetActive(true);
            }
            Refresh(); NotifyChanged(); return true;
        }

        private bool Reject(string message) { Result=message; NotifyChanged(); return false; }
        public override void ResetActivity()
        {
            for(int i=0;i<3;i++) reagentVolumes[i]=6;
            for(int i=0;i<2;i++) sampleVolumes[i]=4;
            SelectedReagent=-1; HasPrecipitate=false; NegativeControlObserved=false; Completed=false;
            transferProgress=1; Result="";
            Instruction="Chọn thuốc thử ở giá bên trái, rồi chọn cốc mẫu. Quan sát NaCl và mẫu đối chứng NaNO₃ với cùng thuốc thử.";
            if(transferDrop!=null) transferDrop.SetActive(false);
            Refresh(); NotifyChanged();
        }

        private void Refresh()
        {
            if(!built) return;
            for(int i=0;i<liquids.Length;i++)
            {
                int amount=i<3?reagentVolumes[i]:sampleVolumes[i-3];
                float height=amount*.024f;
                liquids[i].transform.localScale=new Vector3(.175f,height*.5f,.175f);
                liquids[i].transform.localPosition=positions[i]+Vector3.up*(.042f+height*.5f);
                liquids[i].SetActive(amount>0);
                volumeLabels[i].text=amount+" / "+(i<3?"6":"10");
            }
            for(int i=0;i<3;i++) selectionRings[i].SetActive(SelectedReagent==i);
            foreach(var particle in precipitate) particle.SetActive(HasPrecipitate);
            observation.text=HasPrecipitate?"AgCl ↓ trắng"+(NegativeControlObserved?"    |    Đối chứng: trong":""):NegativeControlObserved?"Đối chứng: không kết tủa":"So sánh hai mẫu";
        }

        private void Update()
        {
            if(!built || transferProgress>=1) return;
            transferProgress=Mathf.Min(1,transferProgress+Time.deltaTime*1.5f);
            transferDrop.transform.localPosition=Vector3.Lerp(transferStart,transferEnd,transferProgress)+Vector3.up*(Mathf.Sin(transferProgress*Mathf.PI)*.18f);
            if(transferProgress>=1) transferDrop.SetActive(false);
        }
    }
}
