using TMPro;
using UnityEngine;

namespace VLAB.MainMenu
{
    public sealed class VLabLeverActivity : VLabActivity
    {
        private enum SelectedPart { Effort, Load, Fulcrum }
        private static readonly Color Orange = new Color(1f, .48f, .14f);
        private static readonly Color Cyan = new Color(.1f, .78f, .92f);
        private Transform beamAssembly, beam, load, effort, fulcrum;
        private TMP_Text reading;
        private SelectedPart selected;
        private bool built;
        public LeverBalanceModel Model { get; } = new LeverBalanceModel();
        public override string Title => "Đòn bẩy và cân bằng mômen";
        public override string Objective => "Dùng lực 5 N cân bằng tải 10 N bằng cách chọn đúng cánh tay đòn.";
        public override string Theory => "Cân bằng khi F tải × d tải = F tác dụng × d tác dụng.\nMỗi vạch tương ứng 0,1 m. Tải 10 N ở 0,2 m cần lực 5 N ở 0,4 m.\nHai lực đều hướng xuống ở hai phía điểm tựa. Bỏ qua khối lượng thanh và ma sát; độ nghiêng chỉ minh họa chiều mất cân bằng.";

        public override void Build()
        {
            if (built) return; built = true;
            Part("LeverBase", PrimitiveType.Cube, new Vector3(0, .045f, .04f), new Vector3(1.94f, .09f, .57f), new Color(.075f, .11f, .16f));
            beamAssembly = new GameObject("LeverBeamAssembly").transform; beamAssembly.SetParent(transform, false);
            beam = Part("LeverBeam", PrimitiveType.Cube, Vector3.zero, new Vector3(1.86f, .065f, .16f), new Color(.56f, .63f, .68f)).transform;
            beam.SetParent(beamAssembly, false);
            fulcrum = Part("LeverFulcrum", PrimitiveType.Cylinder, new Vector3(0, .24f, 0), new Vector3(.16f, .21f, .23f), new Color(.55f, .6f, .7f), () => Select(SelectedPart.Fulcrum)).transform;
            load = Part("LeverLoad10N", PrimitiveType.Cube, Vector3.zero, new Vector3(.19f, .23f, .19f), Orange, () => Select(SelectedPart.Load)).transform;
            load.SetParent(beamAssembly, false);
            effort = Part("LeverEffort5N", PrimitiveType.Cube, Vector3.zero, new Vector3(.15f, .16f, .17f), Cyan, () => Select(SelectedPart.Effort)).transform;
            effort.SetParent(beamAssembly, false);
            var loadLabel = Label("LoadLabel", "10 N", Vector3.zero, .05f); loadLabel.transform.SetParent(load, false);
            loadLabel.transform.localScale = new Vector3(1 / .19f, 1 / .23f, 1 / .19f); loadLabel.transform.localPosition = new Vector3(0, 0, -.55f);
            var effortLabel = Label("EffortLabel", "5 N", Vector3.zero, .05f); effortLabel.transform.SetParent(effort, false);
            effortLabel.transform.localScale = new Vector3(1 / .15f, 1 / .16f, 1 / .17f); effortLabel.transform.localPosition = new Vector3(0, 0, -.55f);
            for (var position = -4; position <= 4; position++)
            {
                var division = position;
                var mark = Part("LeverDivision_" + position, PrimitiveType.Cube, new Vector3(position * .2f, .025f, -.12f), new Vector3(.115f, .11f, .095f), position == 0 ? Color.white : new Color(.25f, .37f, .46f), () => PlaceOnDivision(division));
                mark.transform.SetParent(beamAssembly, false);
                var text = Label("LeverDivisionLabel_" + position, position.ToString(), new Vector3(position * .2f, -.035f, -.18f), .043f);
                text.transform.SetParent(beamAssembly, false);
            }
            Part("LeverCheckBalance", PrimitiveType.Cube, new Vector3(0, .095f, -.27f), new Vector3(.43f, .11f, .11f), Cyan, CheckBalance);
            Label("LeverCheckLabel", "KIỂM TRA", new Vector3(0, .095f, -.335f), .045f);
            Label("LeverScaleLabel", "1 vạch = 0,1 m  ·  chọn vật → chọn vạch", new Vector3(0, .81f, -.16f), .05f);
            reading = Label("LeverTorqueReading", "", new Vector3(0, .96f, -.15f), .055f);
            ResetActivity();
        }

        private void Select(SelectedPart part)
        {
            selected = part;
            Instruction = part == SelectedPart.Effort ? "Đã chọn quả cân 5 N màu xanh. Chọn một vạch bên phải điểm tựa để đặt lực."
                : part == SelectedPart.Load ? "Đã chọn tải 10 N màu cam. Chọn một vạch bên trái điểm tựa để di chuyển tải."
                : "Đã chọn điểm tựa. Chọn vạch −1, 0 hoặc 1 trên thanh; tải và lực phải ở hai phía.";
            NotifyChanged();
        }

        public void PlaceOnDivision(int division)
        {
            if (selected == SelectedPart.Effort) MoveEffort(division);
            else if (selected == SelectedPart.Load) MoveLoad(division);
            else MoveFulcrum(division);
        }
        public void MoveEffort(int division) => Change(Model.LoadPosition, Model.PivotPosition, division);
        public void MoveLoad(int division) => Change(division, Model.PivotPosition, Model.EffortPosition);
        public void MoveFulcrum(int division) => Change(Model.LoadPosition, division, Model.EffortPosition);

        private void Change(int loadPosition, int pivotPosition, int effortPosition)
        {
            if (!Model.SetPositions(loadPosition, pivotPosition, effortPosition))
            {
                Instruction = "Vị trí không phù hợp: tải ở bên trái, lực ở bên phải điểm tựa; điểm tựa chỉ đặt tại −1, 0 hoặc 1.";
                NotifyChanged(); return;
            }
            Completed = false; Result = "";
            Instruction = "Quan sát độ nghiêng và so sánh hai mômen. Khi thanh cân bằng, nhấn KIỂM TRA.";
            RefreshGeometry(); NotifyChanged();
        }

        public void CheckBalance()
        {
            Completed = Model.Balanced;
            Result = $"M tải = 10 × {(Model.PivotPosition - Model.LoadPosition) * .1f:0.0} = {Model.LoadTorque:0.0} N·m.\nM lực = 5 × {(Model.EffortPosition - Model.PivotPosition) * .1f:0.0} = {Model.EffortTorque:0.0} N·m.\n" +
                (Completed ? "Cân bằng! Lực nhỏ bằng một nửa tải nên cần cánh tay đòn dài gấp đôi." : Model.NetTorque > 0 ? "Phía tải 10 N hạ xuống. Tăng khoảng cách lực 5 N tới điểm tựa hoặc đưa tải gần điểm tựa." : "Phía lực 5 N hạ xuống. Giảm cánh tay đòn của lực hoặc đưa tải xa điểm tựa.");
            Instruction = Completed ? "Đã đạt mục tiêu. Thử di chuyển điểm tựa và cân bằng lại để kiểm chứng quy tắc mômen." : "Chọn quả cân hoặc điểm tựa, rồi chọn vạch mới trên thanh và kiểm tra lại.";
            RefreshGeometry(); NotifyChanged();
        }

        public override void ResetActivity()
        {
            Model.Reset(); selected = SelectedPart.Effort; Completed = false; Result = "";
            Instruction = "Chọn quả cân 5 N màu xanh rồi chọn vạch +4. Tải 10 N ở −2 và điểm tựa ở 0. Nhấn KIỂM TRA để giải thích.";
            RefreshGeometry(); NotifyChanged();
        }

        private void RefreshGeometry()
        {
            if (beamAssembly == null) return;
            beamAssembly.localPosition = new Vector3(Model.PivotPosition * .2f, .48f, 0);
            beamAssembly.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(Model.NetTorque * 9, -12, 12));
            beam.localPosition = new Vector3(-Model.PivotPosition * .2f, 0, 0);
            load.localPosition = new Vector3((Model.LoadPosition - Model.PivotPosition) * .2f, .15f, 0);
            effort.localPosition = new Vector3((Model.EffortPosition - Model.PivotPosition) * .2f, .115f, 0);
            fulcrum.localPosition = new Vector3(Model.PivotPosition * .2f, .24f, 0);
            // Division marks stay fixed along the beam while the pivot moves underneath it.
            for (var i = 0; i < beamAssembly.childCount; i++)
            {
                var child = beamAssembly.GetChild(i);
                if (!child.name.StartsWith("LeverDivision")) continue;
                var separator = child.name.LastIndexOf('_');
                if (int.TryParse(child.name.Substring(separator + 1), out var division))
                {
                    var position = child.localPosition; position.x = (division - Model.PivotPosition) * .2f; child.localPosition = position;
                }
            }
            if (reading != null) reading.text = $"TẢI: {Model.LoadTorque:0.0} N·m   |   LỰC: {Model.EffortTorque:0.0} N·m";
        }
    }
}
