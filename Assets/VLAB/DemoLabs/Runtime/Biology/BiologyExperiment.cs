using UnityEngine;

namespace VLAB.DemoLabs
{
    public sealed class BiologyExperiment : VLabExperimentController
    {
        public VLabSnapZone PreparationZone, WaterZone, SampleZone, CoverZone, StageZone;
        public VLabGrabInteractable Slide;
        public VLabRotaryControl Nosepiece, CoarseKnob, FineKnob, LightKnob;
        public VLabInteractable ClipLeft, ClipRight, Eyepiece;
        public Transform LeftClipPart, RightClipPart, StageMovingPart;
        public GameObject WaterDrop, SampleMark, CoverMark;
        public MicroscopeView View;
        public MicroscopeModel Model { get; private set; } = new MicroscopeModel();
        private bool leftClosed, rightClosed;
        private Quaternion leftRotation, rightRotation;
        private Vector3 stagePosition;
        private Vector3 stageTravel, leftClipAxis, rightClipAxis;
        private int previousProgress;
        protected override VLabInteractable GuidanceTarget
        {
            get
            {
                var held = Driver.Held;
                if (held != null)
                {
                    if (held.Kind == "slide") return Model.Prepared ? StageZone : PreparationZone;
                    if (held.Kind == "liquid") return WaterZone;
                    if (held.Kind == "sample") return SampleZone;
                    if (held.Kind == "cover") return CoverZone;
                }
                if (!Model.SlidePrepared || (Model.Prepared && !Model.Mounted)) return Slide;
                var needed = !Model.Water ? "liquid" : !Model.Sample ? "sample" : !Model.Coverslip ? "cover" : "";
                if (needed != "") foreach (var item in Driver.Items) if (item.Kind == needed) return item;
                if (!leftClosed) return ClipLeft;
                if (!rightClosed) return ClipRight;
                return Model.Objective == 0 ? Nosepiece : Eyepiece;
            }
        }
        protected override string Objective => "Quan sát tế bào biểu bì hành\ndưới kính hiển vi";
        protected override string Concept => "Chuẩn bị tiêu bản → cố định lam → lấy nét → nhận biết cấu trúc.\n\nĐặt lam lên đệm, nhỏ nước, thêm biểu bì hành và đặt lamen. Bắt đầu với vật kính 10×, dùng ốc sơ cấp rồi vi cấp. Ở 40×, chỉ chỉnh nhẹ bằng ốc vi cấp.\n\nNhân được tăng tương phản để dễ học; màu sắc là minh họa. Mỗi tế bào có thành tế bào, tế bào chất và không bào lớn.";
        protected override string AdaptiveHint => !Model.SlidePrepared ? "Cầm lam kính trên khay rồi đặt vào vùng CHUẨN BỊ."
            : !Model.Water ? "Cầm ống nhỏ giọt và chọn vùng NHỎ NƯỚC trên lam."
            : !Model.Sample ? "Đặt mẫu biểu bì hành vào vùng MẪU trên lam đã có nước."
            : !Model.Coverslip ? "Đặt lamen lên vùng LAMEN để phủ mẫu."
            : !Model.Mounted ? "Cầm lam đã chuẩn bị, đặt vào vùng BÀN KÍNH bên trái kính hiển vi."
            : !Model.Clips ? "Đóng cả hai kẹp giữ lam bằng cách chọn từng kẹp."
            : !Model.Observed10 ? "Chọn mâm vật kính để về 10×. Xoay sơ cấp đến gần rõ, rồi vi cấp. Có thể thao tác trong thị kính."
            : !Model.Observed40 ? "Đổi sang 40×; xoay ốc vi cấp nhẹ theo chiều tăng để lấy nét lại."
            : "Chọn nhân tím đậm, thành tế bào, rồi vùng tế bào chất nhuộm màu sát thành.";
        protected override void Start()
        {
            leftRotation = LeftClipPart.localRotation; rightRotation = RightClipPart.localRotation;
            stagePosition = StageMovingPart.localPosition;
            stageTravel = StageMovingPart.parent.InverseTransformVector(Vector3.up * .016f);
            leftClipAxis = LeftClipPart.InverseTransformDirection(Vector3.up);
            rightClipAxis = RightClipPart.InverseTransformDirection(Vector3.up);
            PreparationZone.Validate = _ => Model.Mounted ? "Hãy mở hai kẹp và lấy lam khỏi bàn kính trước." : "";
            PreparationZone.Placed += _ => { Model.SlidePrepared = true; Refresh(); };
            PreparationZone.Removed += _ => Refresh();
            WaterZone.Validate = _ => PreparationZone.Occupant != Slide ? "Đặt lam lên đệm chuẩn bị trước khi nhỏ nước." : "";
            WaterZone.Placed += _ => { Model.Water = true; WaterDrop.SetActive(true); Refresh(); };
            SampleZone.Validate = _ => !Model.Water || PreparationZone.Occupant != Slide ? "Hãy đặt lam và nhỏ nước trước khi thêm biểu bì hành." : "";
            SampleZone.Placed += item => { Model.Sample = true; SampleMark.SetActive(true); item.gameObject.SetActive(false); Refresh(); };
            CoverZone.Validate = _ => !Model.Sample || PreparationZone.Occupant != Slide ? "Thêm mẫu biểu bì hành lên lam trước khi phủ lamen." : "";
            CoverZone.Placed += item => { Model.Coverslip = true; CoverMark.SetActive(true); item.gameObject.SetActive(false); Refresh(); };
            StageZone.Validate = _ => !Model.Prepared ? "Tiêu bản chưa hoàn chỉnh: cần nước, biểu bì hành và lamen." : "";
            StageZone.Placed += _ => { Model.Mounted = true; Refresh(); };
            StageZone.Removed += _ => { Model.Mounted = false; leftClosed = rightClosed = false; ApplyClips(); View.Exit(); Refresh(); };
            ClipLeft.Activated += () => ToggleClip(true); ClipRight.Activated += () => ToggleClip(false);
            Eyepiece.Activated += EnterView;
            Nosepiece.ValueChanged += SetObjective;
            CoarseKnob.CanAdjust = () =>
            {
                if (Model.Objective != 40) return true;
                Mistake("Ở 40×, hãy dùng ốc vi cấp để chỉnh nhẹ và tránh đưa vật kính quá gần lam."); return false;
            };
            CoarseKnob.ValueChanged += value =>
            {
                if (Resetting) return;
                Model.Coarse = value; Model.CoarseUsed = true;
                StageMovingPart.localPosition = stagePosition + stageTravel * value;
                MarkActivity(); Refresh();
            };
            FineKnob.ValueChanged += value =>
            {
                if (Resetting) return;
                Model.Fine = value;
                if (Model.Objective == 10) Model.FineUsed10 = true;
                if (Model.Objective == 40) Model.FineUsed40 = true;
                MarkActivity(); Refresh();
            };
            LightKnob.ValueChanged += value => { if (!Resetting) { Model.Light = value; MarkActivity(); Refresh(); } };
            base.Start();
        }
        protected override void Update()
        {
            base.Update();
            var progress = (Model.Observed10 ? 1 : 0) + (Model.Observed40 ? 2 : 0);
            if (previousProgress != progress) { previousProgress = progress; Refresh(); }
        }
        private void ToggleClip(bool left)
        {
            if (!Started) return;
            if (!Model.Mounted) { Mistake("Hãy đặt tiêu bản hoàn chỉnh lên bàn kính trước khi đóng kẹp."); return; }
            if (left) leftClosed = !leftClosed; else rightClosed = !rightClosed;
            ApplyClips(); Refresh();
        }
        private void ApplyClips()
        {
            Model.Clips = leftClosed && rightClosed;
            LeftClipPart.localRotation = leftRotation * Quaternion.AngleAxis(leftClosed ? 0 : -65, leftClipAxis);
            RightClipPart.localRotation = rightRotation * Quaternion.AngleAxis(rightClosed ? 0 : 65, rightClipAxis);
        }
        private void SetObjective(float value)
        {
            if (Resetting) return;
            var requested = value < .5f ? 10 : 40;
            if (requested == 40 && !Model.Observed10)
            {
                Mistake("Hãy quan sát ảnh rõ nét ở vật kính 10× trước khi chuyển sang 40×.");
                if (Nosepiece.Value != 0) Nosepiece.SetValue(0);
                return;
            }
            Model.Objective = requested;
            if (requested == 40) Model.FineUsed40 = false;
            MarkActivity(); Refresh();
        }
        public void EnterView()
        {
            if (!Started) return;
            if (!Model.Mounted || !Model.Clips) { Mistake("Đặt tiêu bản và đóng cả hai kẹp giữ lam trước khi nhìn qua thị kính."); return; }
            if (Model.Objective == 0) { Mistake("Xoay mâm vật kính để chọn 10× trước."); return; }
            View.Enter(); MarkActivity();
        }
        public override void ExitInspection() { if (View != null) View.Exit(); }
        public override void RotateInspection(float amount, bool coarse)
        {
            if (coarse && Model.Objective == 10) CoarseKnob.Rotate(amount);
            else FineKnob.Rotate(amount);
        }
        public bool IdentifyAt(Vector2 uv)
        {
            if (!View.Inspecting || !Model.Observed40) { Mistake("Hãy quan sát và lấy nét ở 40× trước khi nhận biết cấu trúc."); return false; }
            if (!Model.Sharp) { Mistake("Ảnh đang mờ. Chỉnh ốc vi cấp để thấy rõ cấu trúc."); return false; }
            var selected = OnionSpecimen.Classify(uv);
            if (!Model.Identify(selected))
            {
                Mistake(selected == CellStructure.Vacuole ? "Vùng nhạt ở giữa là không bào lớn. Tế bào chất tạo lớp mỏng ở phía ngoài." : "Đây chưa phải cấu trúc được yêu cầu. Hãy quan sát hình dạng và vị trí."); return false;
            }
            View.ShowIdentification(Model.Identified - 1, uv);
            Hud.Feedback(Model.Identified == 1 ? "Đúng: nhân điều khiển nhiều hoạt động của tế bào và chứa vật chất di truyền."
                : Model.Identified == 2 ? "Đúng: thành tế bào giúp bảo vệ và giữ hình dạng."
                : "Đúng: tế bào chất là môi trường diễn ra nhiều hoạt động của tế bào. Chọn Kiểm tra để xem kết quả.");
            Refresh(); return true;
        }
        public override void Check()
        {
            if (!Started) return;
            if (Model.Identified < 3) { Mistake(AdaptiveHint); return; }
            View.Exit();
            Finish("Bạn đã nhận biết tế bào biểu bì hành", "Đã chuẩn bị tiêu bản, cố định lam và lấy nét ở 10× / 40×.\n\nThành tế bào: đường bao giữ hình dạng.\nNhân: chứa vật chất di truyền.\nTế bào chất: lớp mỏng bao quanh không bào lớn.\n\nVật kính 10× × thị kính 10× = 100×.\nVật kính 40× × thị kính 10× = 400×, trường nhìn nhỏ hơn.\n\nGhi nhớ: Kính hiển vi giúp quan sát những cấu trúc mắt thường khó phân biệt.");
        }
        protected override void ResetModel()
        {
            Model = new MicroscopeModel(); leftClosed = rightClosed = false; ApplyClips();
            previousProgress = 0; View.ResetAnnotations();
            StageMovingPart.localPosition = stagePosition;
            WaterDrop.SetActive(false); SampleMark.SetActive(false); CoverMark.SetActive(false);
            foreach (var item in Driver.Items) item.gameObject.SetActive(true);
            Nosepiece.SetValue(1); CoarseKnob.SetValue(0); FineKnob.SetValue(.5f); LightKnob.SetValue(.7f);
        }
        protected override void Refresh()
        {
            var onPreparationPad = PreparationZone.Occupant == Slide;
            WaterZone.gameObject.SetActive(onPreparationPad); SampleZone.gameObject.SetActive(onPreparationPad); CoverZone.gameObject.SetActive(onPreparationPad);
            WaterZone.ShowGuide(!Model.Water); SampleZone.ShowGuide(Model.Water && !Model.Sample); CoverZone.ShowGuide(Model.Sample && !Model.Coverslip);
            if (!Model.Prepared) Hud.Step(1, 8, "CHUẨN BỊ TIÊU BẢN", AdaptiveHint);
            else if (!Model.Mounted) Hud.Step(2, 8, "ĐẶT LAM LÊN BÀN KÍNH", "Cầm lam đã chuẩn bị và gắn vào bàn kính.");
            else if (!Model.Clips) Hud.Step(3, 8, "CỐ ĐỊNH TIÊU BẢN", "Đóng cả hai kẹp giữ lam.");
            else if (!Model.Observed10) Hud.Step(4, 8, "LẤY NÉT Ở 10×", "Chọn 10×; xoay ốc sơ cấp, rồi vi cấp. Chọn thị kính để quan sát.");
            else if (!Model.Observed40) Hud.Step(5, 8, "CHUYỂN SANG 40×", "Xoay mâm vật kính và chỉnh nhẹ ốc vi cấp để lấy nét lại.");
            else if (Model.Identified < 3) Hud.Step(6, 8, "NHẬN BIẾT CẤU TRÚC", "Chọn trực tiếp nhân → thành tế bào → vùng tế bào chất trong ảnh.");
            else Hud.Step(7, 8, "KẾT QUẢ QUAN SÁT", "Chọn Kiểm tra để xem giải thích và hoàn thành.");
            if (Completed) Hud.Step(8, 8, "HOÀN THÀNH", "Kính hiển vi mở ra thế giới cấu trúc vi mô.");
        }
    }
}
