using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VLAB.DemoLabs
{
    public sealed class EngineeringExperiment : VLabExperimentController
    {
        public VLabSnapZone ResistorZone, LedZone;
        public VLabWire[] Wires;
        public VLabInteractable PowerButton, ValidateButton, ResultButton;
        public Renderer LedLens;
        public Light LedLight;
        public TMP_Text SupplyDisplay, ReadingDisplay;
        public bool Powered { get; private set; }
        public bool Validated { get; private set; }
        public CircuitResult Result { get; private set; }
        private readonly List<CircuitLink> links = new List<CircuitLink>(3);
        private MaterialPropertyBlock ledBlock;
        protected override VLabInteractable GuidanceTarget
        {
            get
            {
                if (Driver.Held != null)
                {
                    if (Driver.Held.Kind == "resistor") return ResistorZone;
                    if (Driver.Held.Kind == "led") return LedZone;
                    for (var i = 0; i < Wires.Length; i++)
                    {
                        if (Driver.Held != Wires[i].EndA && Driver.Held != Wires[i].EndB) continue;
                        var first = i == 0 ? Terminal.Positive : i == 1 ? Terminal.ResistorB : Terminal.Cathode;
                        var second = i == 0 ? Terminal.ResistorA : i == 1 ? Terminal.Anode : Terminal.Ground;
                        foreach (var zone in Driver.Zones)
                        {
                            var socket = zone.GetComponent<VLabConnectionSocket>();
                            if (socket != null && zone.Occupant == null && (socket.Terminal == first || socket.Terminal == second)) return zone;
                        }
                    }
                }
                if (ResistorZone.Occupant == null)
                    foreach (var item in Driver.Items) if (item.Kind == "resistor" && item.Value == 220) return item;
                if (LedZone.Occupant == null)
                    foreach (var item in Driver.Items) if (item.Kind == "led") return item;
                foreach (var wire in Wires)
                { if (wire.EndA.Zone == null) return wire.EndA; if (wire.EndB.Zone == null) return wire.EndB; }
                return !Validated ? ValidateButton : !Powered ? PowerButton : ResultButton;
            }
        }
        protected override string Objective => "Thiết kế và lắp ráp\nmạch LED an toàn";
        protected override string Concept => "Nguồn 5 V · LED 2 V · Dòng mong muốn 15 mA\n\nU = I × R\nR ≈ (5 − 2) / 0,015 = 200 Ω\nChọn giá trị thực tế gần nhất: 220 Ω.\n\nCầm linh kiện và đặt vào breadboard. Nối từng đầu dây vào cọc có nhãn; kiểm tra trước khi bật nguồn.\n\nĐây là mô phỏng mạch điện áp thấp, dùng mô hình LED xấp xỉ.";
        protected override string AdaptiveHint => ResistorZone.Occupant == null ? "Chọn điện trở 220 Ω ở khay, rồi chọn vùng ĐIỆN TRỞ trên breadboard."
            : LedZone.Occupant == null ? "Cầm LED và đặt vào vùng LED. Cuộn chuột khi cầm để đảo cực."
            : "Nối ba dây: nguồn + → R.A; R.B → LED A; LED K → nguồn −. Mỗi đầu dây phải gắn vào một cọc.";
        protected override void Start()
        {
            ledBlock = new MaterialPropertyBlock();
            ResistorZone.Placed += _ => CircuitChanged(); ResistorZone.Removed += _ => CircuitChanged();
            LedZone.Placed += _ => CircuitChanged(); LedZone.Removed += _ => CircuitChanged();
            foreach (var item in Driver.Items) item.Changed += CircuitChanged;
            PowerButton.Activated += TogglePower; ValidateButton.Activated += Check; ResultButton.Activated += ObserveResult;
            base.Start();
        }
        public CircuitResult Evaluate()
        {
            links.Clear(); foreach (var wire in Wires) if (wire.TryGetLink(out var link)) links.Add(link);
            return CircuitModel.Evaluate(ResistorZone.Occupant != null ? ResistorZone.Occupant.Value : 0,
                LedZone.Occupant != null, LedZone.Occupant != null && LedZone.Occupant.Flipped, links);
        }
        private void CircuitChanged()
        {
            if (Resetting) return;
            var wasPowered = Powered; Powered = false; Validated = false; SetLed(0);
            if (wasPowered) Hud.Feedback("Nguồn đã tắt vì mạch vừa thay đổi. Hãy kiểm tra lại trước khi cấp điện.", true);
            SupplyDisplay.text = "5.00 V\nNGUỒN TẮT"; ReadingDisplay.text = "CHƯA KIỂM TRA";
            Refresh();
        }
        public override void Check()
        {
            if (!Started) return;
            Result = Evaluate(); Validated = Result.CanPower;
            if (!Powered) ReadingDisplay.text = Validated ? "MẠCH HỢP LỆ\nSẴN SÀNG CẤP NGUỒN" : "CẦN SỬA MẠCH\nNGUỒN TẮT";
            if (!Validated) { Powered = false; SetLed(0); Mistake(Explain(Result.State)); }
            else Hud.Feedback("Mạch nối kín và đúng cực. " + (Result.State == CircuitState.Overcurrent ? "100 Ω tạo dòng quá lớn; hãy đổi sang 220 Ω. Có thể quan sát cảnh báo trong mô phỏng." : "Bạn có thể bật nguồn để quan sát."), Result.State == CircuitState.Overcurrent);
            Refresh();
        }
        public void TogglePower()
        {
            if (!Started) return;
            if (Powered) { Powered = false; SetLed(0); SupplyDisplay.text = "5.00 V\nNGUỒN TẮT"; ReadingDisplay.text = "0.0 mA\nNGUỒN TẮT"; Refresh(); return; }
            Result = Evaluate();
            if (!Validated || !Result.CanPower) { Mistake("Nguồn được giữ ở trạng thái tắt. Hãy chọn Kiểm tra mạch trước. " + (Result.CanPower ? "" : Explain(Result.State))); return; }
            Powered = true;
            SetLed(Result.State == CircuitState.Safe ? 1 : Result.State == CircuitState.Dim ? .16f : 2);
            SupplyDisplay.text = $"5.00 V\n{Result.Current * 1000:0.0} mA";
            ReadingDisplay.text = $"{ResistorZone.Occupant.Value} Ω  /  {Result.Current * 1000:0.0} mA\n" + (Result.State == CircuitState.Safe ? "DÒNG PHÙ HỢP" : Result.State == CircuitState.Dim ? "LED SÁNG YẾU" : "QUÁ DÒNG");
            Hud.Feedback(Explain(Result.State), Result.State == CircuitState.Overcurrent);
            Refresh();
        }
        public void ObserveResult()
        {
            if (!Powered || Result.State != CircuitState.Safe) { Mistake("Hãy tạo mạch an toàn với điện trở 220 Ω, kiểm tra và bật nguồn trước khi hoàn thành."); return; }
            Finish("Mạch LED hoạt động an toàn", $"Điện trở đã chọn: {ResistorZone.Occupant.Value} Ω\nI ≈ (5 − 2) / 220 = 13,6 mA\n\n100 Ω → 30 mA: dòng quá lớn.\n220 Ω → 13,6 mA: phù hợp với mục tiêu.\n1 kΩ → 3 mA: LED sáng yếu.\n\nGhi nhớ: Điện trở hạn dòng giúp LED hoạt động trong vùng an toàn. Điện áp thuận thực tế thay đổi theo loại LED và dòng điện.");
        }
        private void SetLed(float brightness)
        {
            // End the hover override before replacing the physical light state.
            LedLens.GetComponentInParent<VLabInteractable>()?.SetFocus(false);
            if (ledBlock == null) ledBlock = new MaterialPropertyBlock();
            var color = Color.Lerp(new Color(.045f, .14f, .07f), new Color(.72f, 1f, .78f), Mathf.Clamp01(brightness / 2));
            ledBlock.SetColor("_BaseColor", color); ledBlock.SetColor("_Color", color);
            ledBlock.SetColor("_EmissionColor", new Color(.12f, 1f, .2f) * brightness * .35f);
            LedLens.SetPropertyBlock(ledBlock); LedLight.intensity = brightness * .7f;
        }
        protected override void ResetModel()
        {
            Powered = false; Validated = false; Result = default; SetLed(0);
            SupplyDisplay.text = "5.00 V\nNGUỒN TẮT"; ReadingDisplay.text = "CHƯA KIỂM TRA";
        }
        protected override void Refresh()
        {
            if (ResistorZone.Occupant == null) Hud.Step(1, 6, "CHỌN ĐIỆN TRỞ", "Chọn 100 Ω, 220 Ω hoặc 1 kΩ rồi đặt lên breadboard.");
            else if (LedZone.Occupant == null) Hud.Step(2, 6, "LẮP LED", "Chọn LED và đặt vào vị trí. A: anode (+); K: cathode (−).");
            else if (!Validated) Hud.Step(3, 6, "NỐI & KIỂM TRA MẠCH", "Gắn 6 đầu dây: + → R.A · R.B → A · K → −. Chọn Kiểm tra mạch.");
            else if (!Powered) Hud.Step(4, 6, "BẬT NGUỒN", "Nhấn nút nguồn trên bộ nguồn 5 V.");
            else Hud.Step(5, 6, "QUAN SÁT & GIẢI THÍCH", "So sánh độ sáng và dòng điện. Chọn Kết quả trên bàn khi mạch an toàn.");
            if (Completed) Hud.Step(6, 6, "HOÀN THÀNH", "Điện trở hạn dòng giúp bảo vệ LED.");
        }
        private static string Explain(CircuitState state)
        {
            switch (state)
            {
                case CircuitState.Safe: return "Điện trở 220 Ω giới hạn dòng điện ở mức phù hợp với LED.";
                case CircuitState.Overcurrent: return "Dòng điện qua LED quá lớn (30 mA). Điện trở 100 Ω chưa hạn dòng đủ tốt. Tắt nguồn và thay điện trở.";
                case CircuitState.Dim: return "Dòng điện nhỏ (3 mA) nên LED sáng yếu. Hãy thử điện trở 220 Ω.";
                case CircuitState.Reversed: return "LED đang mắc ngược cực. Hãy kiểm tra anode và cathode; lấy LED ra và cuộn chuột để đảo cực.";
                case CircuitState.Incomplete: return "Đường nối mạch chưa hoàn chỉnh. Hãy lắp đủ linh kiện và gắn cả hai đầu của ba dây.";
                default: return "Mạch nối sai hoặc có đường nối tắt. Hãy kiểm tra đường từ cực dương qua điện trở, LED và về cực âm. Nguồn vẫn tắt.";
            }
        }
    }
}
