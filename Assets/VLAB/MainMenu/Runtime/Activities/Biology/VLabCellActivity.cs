using TMPro;
using UnityEngine;

namespace VLAB.MainMenu
{
    /// <summary>
    /// Original, deliberately simplified cutaway of a photosynthetic plant cell.
    /// The open front and exaggerated organelles make every structure ray-selectable.
    /// </summary>
    public sealed class VLabCellActivity : VLabActivity
    {
        public enum Structure
        {
            CellWall, Membrane, Cytoplasm, Vacuole, Chloroplast, Nucleus, Mitochondrion
        }

        private static readonly string[] StructureNames =
        {
            "Thành tế bào", "Màng sinh chất", "Tế bào chất", "Không bào trung tâm",
            "Lục lạp", "Nhân", "Ti thể"
        };

        private static readonly string[] Functions =
        {
            "Thành tế bào giàu cellulose, giúp bảo vệ và duy trì hình dạng tế bào.",
            "Màng sinh chất kiểm soát có chọn lọc sự trao đổi chất giữa tế bào và môi trường.",
            "Tế bào chất chứa các bào quan; nhiều phản ứng chuyển hóa diễn ra trong bào tương.",
            "Không bào trung tâm chứa dịch tế bào; nước trong không bào góp phần duy trì sức trương.",
            "Lục lạp là nơi quang hợp, sử dụng năng lượng ánh sáng để tổng hợp chất hữu cơ.",
            "Nhân chứa phần lớn vật chất di truyền và tham gia điều khiển hoạt động của tế bào.",
            "Ti thể tham gia hô hấp tế bào, tạo phần lớn ATP trong điều kiện hiếu khí."
        };

        private static readonly string[] Questions =
        {
            "Chọn lớp cứng ở ngoài cùng giúp duy trì hình dạng tế bào.",
            "Chọn lớp mỏng ngay phía trong thành, kiểm soát trao đổi chất.",
            "Chọn vùng nền chứa các bào quan, nơi có nhiều hoạt động chuyển hóa.",
            "Chọn khoang lớn chứa dịch tế bào và góp phần duy trì sức trương.",
            "Chọn bào quan sử dụng năng lượng ánh sáng để quang hợp.",
            "Chọn cấu trúc chứa phần lớn vật chất di truyền.",
            "Chọn bào quan tham gia hô hấp tế bào và tạo ATP."
        };

        private Transform model;
        private TMP_Text selectedLabel;
        private bool built;

        public int IdentifiedCount { get; private set; }
        public float InspectionAngle { get; private set; }
        public override string Title => "Khám phá tế bào thực vật 3D";
        public override string Objective => "Nhận biết 7 cấu trúc của tế bào lá và liên hệ hình dạng với chức năng.";
        public override string Theory =>
            "Đây là sơ đồ cắt mở, giản lược của một tế bào lá có quang hợp. Màu và kích thước được quy ước để dễ học, không theo tỉ lệ thật. " +
            "Nhiều cấu trúc nhỏ được lược bỏ. Các lớp bao ngoài đã được mở để quan sát bên trong.\n\n" +
            "Xanh đậm: thành tế bào. Vàng: màng sinh chất. Nền xanh nhạt: tế bào chất. " +
            "Xanh lam lớn: không bào. Xanh lá nhỏ: lục lạp. Tím: nhân. Cam: ti thể.\n\n" +
            "Không phải mọi tế bào thực vật đều có lục lạp; ví dụ biểu bì củ hành trong bài kính hiển vi thường không có lục lạp. " +
            "Tế bào thực vật có cả lục lạp và ti thể. Hãy chọn trực tiếp cấu trúc theo câu hỏi; dùng hai nút xoay để đổi góc nhìn.";

        public override void Build()
        {
            if (built) return;
            built = true;
            model = new GameObject("CellModel").transform;
            model.SetParent(transform, false);
            model.localPosition = new Vector3(0, .52f, .015f);

            var wall = new Color(.12f, .38f, .26f);
            var membrane = new Color(.95f, .72f, .25f);
            var cytoplasm = new Color(.46f, .7f, .57f);

            // A cutaway perimeter and rear cytoplasm leave the organelles unobstructed.
            CellPart("CellWall_Left", PrimitiveType.Cube, new Vector3(-.62f, 0, 0), new Vector3(.08f, .66f, .2f), wall, Structure.CellWall);
            CellPart("CellWall_Right", PrimitiveType.Cube, new Vector3(.62f, 0, 0), new Vector3(.08f, .66f, .2f), wall, Structure.CellWall);
            CellPart("CellWall_Top", PrimitiveType.Cube, new Vector3(0, .29f, 0), new Vector3(1.2f, .08f, .2f), wall, Structure.CellWall);
            CellPart("CellWall_Bottom", PrimitiveType.Cube, new Vector3(0, -.29f, 0), new Vector3(1.2f, .08f, .2f), wall, Structure.CellWall);
            CellPart("Membrane_Left", PrimitiveType.Cube, new Vector3(-.568f, 0, -.03f), new Vector3(.025f, .5f, .12f), membrane, Structure.Membrane);
            CellPart("Membrane_Right", PrimitiveType.Cube, new Vector3(.568f, 0, -.03f), new Vector3(.025f, .5f, .12f), membrane, Structure.Membrane);
            CellPart("Membrane_Top", PrimitiveType.Cube, new Vector3(0, .237f, -.03f), new Vector3(1.15f, .025f, .12f), membrane, Structure.Membrane);
            CellPart("Membrane_Bottom", PrimitiveType.Cube, new Vector3(0, -.237f, -.03f), new Vector3(1.15f, .025f, .12f), membrane, Structure.Membrane);
            CellPart("Cytoplasm", PrimitiveType.Cube, new Vector3(0, 0, .09f), new Vector3(1.12f, .45f, .04f), cytoplasm, Structure.Cytoplasm);
            CellPart("Vacuole", PrimitiveType.Sphere, new Vector3(.09f, 0, -.02f), new Vector3(.67f, .34f, .17f), new Color(.24f, .63f, .85f), Structure.Vacuole);
            var nucleus = CellPart("Nucleus", PrimitiveType.Sphere, new Vector3(-.37f, -.015f, -.07f), new Vector3(.24f, .23f, .18f), new Color(.6f, .39f, .79f), Structure.Nucleus);
            Detail("Nucleolus", PrimitiveType.Sphere, new Vector3(-.39f, -.005f, -.158f), new Vector3(.065f, .06f, .025f), new Color(.33f, .2f, .51f), nucleus.transform);

            var chloroplastPositions = new[]
            {
                new Vector3(-.37f, .176f, -.08f), new Vector3(.38f, .18f, -.08f),
                new Vector3(.42f, -.178f, -.08f)
            };
            for (var i = 0; i < chloroplastPositions.Length; i++)
            {
                var position = chloroplastPositions[i];
                var chloroplast = CellPart("Chloroplast_" + i, PrimitiveType.Sphere, position, new Vector3(.205f, .086f, .09f), new Color(.27f, .63f, .22f), Structure.Chloroplast);
                // Stylized internal stacks, decorative only, share the selectable parent.
                for (var stack = -1; stack <= 1; stack++)
                    Detail("ThylakoidStack_" + stack, PrimitiveType.Cube, position + new Vector3(stack * .044f, 0, -.043f), new Vector3(.014f, .043f, .01f), new Color(.13f, .38f, .15f), chloroplast.transform);
            }

            var mitochondrion = CellPart("Mitochondrion", PrimitiveType.Sphere, new Vector3(-.32f, -.174f, -.09f), new Vector3(.24f, .085f, .1f), new Color(.93f, .46f, .23f), Structure.Mitochondrion);
            for (var fold = -2; fold <= 2; fold++)
                Detail("Crista_" + fold, PrimitiveType.Cube, new Vector3(-.32f + fold * .031f, -.174f, -.139f), new Vector3(.01f, .041f, .008f), new Color(.63f, .26f, .13f), mitochondrion.transform);

            selectedLabel = Label("CellSelectionLabel", "", new Vector3(0, .935f, -.12f), .052f);
            selectedLabel.rectTransform.sizeDelta = new Vector2(1.65f, .15f);
            Part("CellRotateLeft", PrimitiveType.Cube, new Vector3(-.4f, .09f, -.3f), new Vector3(.35f, .12f, .065f), new Color(.14f, .28f, .33f), () => RotateInspection(-15));
            Part("CellRotateRight", PrimitiveType.Cube, new Vector3(.4f, .09f, -.3f), new Vector3(.35f, .12f, .065f), new Color(.14f, .28f, .33f), () => RotateInspection(15));
            var left = Label("CellRotateLeftLabel", "XOAY TRÁI", new Vector3(-.4f, .09f, -.337f), .045f);
            var right = Label("CellRotateRightLabel", "XOAY PHẢI", new Vector3(.4f, .09f, -.337f), .045f);
            left.rectTransform.sizeDelta = right.rectTransform.sizeDelta = new Vector2(.34f, .1f);
            ResetActivity();
        }

        private GameObject CellPart(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Color color, Structure structure)
        {
            var part = Part(name, shape, position, scale, color, () => SelectStructure(structure));
            part.transform.SetParent(model, false);
            return part;
        }

        private void Detail(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Color color, Transform owner)
        {
            var detail = Part(name, shape, position, scale, color);
            detail.GetComponent<Collider>().enabled = false;
            detail.transform.SetParent(model, false);
            // Preserve model-relative position/scale, while letting the ray find the parent.
            detail.transform.SetParent(owner, true);
        }

        public bool SelectStructure(Structure structure)
        {
            var index = (int)structure;
            if (!built || !isActiveAndEnabled || Time.timeScale <= 0 || index < 0 || index >= StructureNames.Length) return false;
            if (selectedLabel != null) selectedLabel.text = StructureNames[index].ToUpperInvariant();
            if (Completed)
            {
                Result = "Đã hoàn thành 7/7. " + Functions[index];
                NotifyChanged();
                return false;
            }
            var accepted = index == IdentifiedCount;
            if (accepted)
            {
                IdentifiedCount++;
                Completed = IdentifiedCount == StructureNames.Length;
                Result = Completed
                    ? "Hoàn thành 7/7 cấu trúc. " + Functions[index] + " Tế bào lá vừa quang hợp vừa hô hấp."
                    : "Đúng — " + Functions[index];
            }
            else
            {
                Result = "Chưa đúng với câu hỏi hiện tại. Bạn chọn " + StructureNames[index].ToLowerInvariant() + ". " + Functions[index];
            }
            UpdateInstruction();
            NotifyChanged();
            return accepted;
        }

        public void RotateInspection(float degrees)
        {
            if (!built || !isActiveAndEnabled || Time.timeScale <= 0 || float.IsNaN(degrees) || float.IsInfinity(degrees)) return;
            // Keep the open front facing the learner and within the workstation bounds.
            InspectionAngle = Mathf.Clamp(InspectionAngle + degrees, -30, 30);
            model.localRotation = Quaternion.Euler(0, InspectionAngle, 0);
            NotifyChanged();
        }

        public override void ResetActivity()
        {
            IdentifiedCount = 0;
            Completed = false;
            InspectionAngle = 0;
            Result = string.Empty;
            if (model != null) model.localRotation = Quaternion.identity;
            if (selectedLabel != null) selectedLabel.text = "TẾ BÀO LÁ • SƠ ĐỒ CẮT MỞ";
            UpdateInstruction();
            NotifyChanged();
        }

        private void UpdateInstruction()
        {
            Instruction = Completed
                ? "Đã nhận biết đủ 7 cấu trúc. Tiếp tục chọn để ôn chức năng, hoặc chọn Làm lại."
                : "Câu " + (IdentifiedCount + 1) + "/7 · " + Questions[IdentifiedCount];
        }
    }
}
