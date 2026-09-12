using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VLAB.MainMenu
{
    public sealed class VLabGearActivity : VLabActivity
    {
        private static readonly Color Orange = new Color(1f, .48f, .14f);
        private static readonly Color Cyan = new Color(.1f, .78f, .92f);
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly Transform[] gears = new Transform[2];
        private readonly Vector3[] trays = { new Vector3(-.72f, .27f, .16f), new Vector3(.65f, .43f, .16f) };
        private readonly Vector3[] shafts = { new Vector3(-.27f, .52f, -.13f), new Vector3(.27f, .52f, -.13f) };
        private TMP_Text reading;
        private int selected;
        private float driverAngle;
        private bool built;
        public GearTrainModel Model { get; } = new GearTrainModel();
        public bool Running { get; private set; }
        public override string Title => "Bộ truyền bánh răng";
        public override string Objective => "Lắp bộ truyền giảm tốc: trục B quay bằng một nửa tốc độ trục A.";
        public override string Theory => "Hai bánh răng ngoài ăn khớp quay ngược chiều. nB / nA = −ZA / ZB.\n12 răng dẫn 24 răng → 60 vòng/phút thành 30 vòng/phút.\nMô hình lý tưởng cùng môđun, không trượt và không xét ma sát; biên dạng răng được giản lược.";

        public override void Build()
        {
            if (built) return; built = true;
            Part("GearBackplate", PrimitiveType.Cube, new Vector3(0, .48f, .3f), new Vector3(2.05f, .91f, .06f), new Color(.075f, .11f, .16f));
            for (var i = 0; i < 2; i++)
            {
                var shaft = i;
                var socket = Part("GearSocket_" + i, PrimitiveType.Cylinder, shafts[i] + new Vector3(0, 0, .055f), new Vector3(.27f, .035f, .27f), i == 0 ? Orange : Cyan, () => PlaceSelected(shaft));
                socket.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Label("ShaftLabel_" + i, i == 0 ? "A · DẪN" : "B · BỊ DẪN", new Vector3(shafts[i].x, .96f, -.15f), .052f);
                var teeth = i == 0 ? 12 : 24;
                var part = Part("Gear_" + teeth, PrimitiveType.Cube, trays[i], Vector3.one, i == 0 ? Orange : Cyan, () => ActivateGear(teeth));
                var mesh = CreateGearMesh(teeth); meshes.Add(mesh); part.GetComponent<MeshFilter>().sharedMesh = mesh;
                part.GetComponent<BoxCollider>().size = new Vector3(teeth * .03f + .03f, teeth * .03f + .03f, .075f);
                gears[i] = part.transform;
                var marker = Part("GearMark_" + teeth, PrimitiveType.Cube, Vector3.zero, new Vector3(.045f, .045f, .013f), Color.white);
                marker.transform.SetParent(part.transform, false); marker.transform.localPosition = new Vector3(teeth * .011f, 0, -.05f);
                // The marker belongs to its gear and must not intercept the pointer.
                marker.GetComponent<Collider>().enabled = false;
            }
            var handle = Part("GearDriveHandle", PrimitiveType.Cylinder, new Vector3(0, .085f, -.3f), new Vector3(.25f, .06f, .25f), Orange, Run);
            handle.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Label("GearDriveLabel", "CHẠY / DỪNG", new Vector3(0, .085f, -.39f), .045f);
            reading = Label("GearReading", "", new Vector3(0, .79f, -.25f), .045f);
            ResetActivity();
        }

        public void SelectGear(int teeth)
        {
            if (teeth != 12 && teeth != 24) return;
            selected = teeth; Running = false;
            Instruction = $"Đã chọn bánh {teeth} răng. Chọn ổ trục A hoặc B; có thể chọn cả bánh đã lắp để di chuyển.";
            // Bring selected parts forward for an obvious selection cue without changing pitch radius.
            RefreshGeometry(); NotifyChanged();
        }

        private void ActivateGear(int teeth)
        {
            var shaft = Model.DriverTeeth == teeth ? 0 : Model.OutputTeeth == teeth ? 1 : -1;
            if (selected != 0 && selected != teeth && shaft >= 0) PlaceSelected(shaft);
            else SelectGear(teeth);
        }

        public void PlaceSelected(int shaft)
        {
            if (selected == 0) { Instruction = "Chọn bánh 12 hoặc 24 răng trước, rồi chọn ổ trục."; NotifyChanged(); return; }
            if (!Model.Place(selected, shaft)) return;
            selected = 0; Running = false; Completed = false; Result = ""; driverAngle = 0;
            Instruction = "Chọn bánh còn lại rồi chọn ổ trục còn trống. Nhấn núm CHẠY để kiểm tra bộ truyền.";
            RefreshGeometry(); NotifyChanged();
        }

        public void Run()
        {
            var state = Model.Check();
            if (state == GearAssemblyState.Incomplete)
            {
                Running = false; Completed = false; Result = "Chưa đủ hai bánh răng. Lắp một bánh vào mỗi ổ A và B.";
            }
            else
            {
                Running = !Running;
                Completed = state == GearAssemblyState.Reduction;
                Result = $"ZA = {Model.DriverTeeth}; ZB = {Model.OutputTeeth}. nB/nA = {Model.SpeedRatio:0.0}.\nA: 60 vòng/phút → B: {60 * Mathf.Abs(Model.SpeedRatio):0} vòng/phút, ngược chiều.\n" +
                    (Completed ? "Đúng mục tiêu: giảm tốc 2 lần. Trong mô hình lý tưởng, mômen xoắn đầu ra tăng 2 lần." : "Đây là bộ tăng tốc. Đổi vị trí hai bánh để B quay chậm bằng một nửa A.");
            }
            Instruction = Running ? "Quan sát dấu trắng trên hai bánh. Chọn bánh và ổ trục để đổi cấu hình; kết quả sẽ được kiểm tra lại." : "Lắp bánh 12 răng vào A và bánh 24 răng vào B, rồi nhấn CHẠY.";
            RefreshGeometry(); NotifyChanged();
        }

        public override void ResetActivity()
        {
            Model.Reset(); selected = 0; driverAngle = 0; Running = false; Completed = false; Result = "";
            Instruction = "Chọn bánh 12 răng màu cam, rồi chọn ổ trục A. Sau đó đặt bánh 24 răng màu xanh vào B.";
            RefreshGeometry(); NotifyChanged();
        }

        private void Update()
        {
            if (!Running) return;
            driverAngle = (driverAngle + 360f * Time.deltaTime) % 720f;
            ApplyRotation();
        }

        private void RefreshGeometry()
        {
            for (var i = 0; i < gears.Length; i++)
            {
                if (gears[i] == null) continue;
                var teeth = i == 0 ? 12 : 24;
                var position = Model.DriverTeeth == teeth ? shafts[0] : Model.OutputTeeth == teeth ? shafts[1] : trays[i];
                if (selected == teeth) position.z -= .08f;
                gears[i].localPosition = position;
            }
            ApplyRotation();
            if (reading != null) reading.text = selected != 0 ? $"ĐANG CHỌN: {selected} RĂNG" : Running ? $"60 → {Mathf.Abs(Model.SpeedRatio) * 60:0} vòng/phút" : "12 RĂNG  /  24 RĂNG";
        }

        private void ApplyRotation()
        {
            for (var i = 0; i < gears.Length; i++)
            {
                if (gears[i] == null) continue;
                var teeth = i == 0 ? 12 : 24;
                var angle = Model.DriverTeeth == teeth ? driverAngle : Model.OutputTeeth == teeth ? driverAngle * Model.SpeedRatio + 180f / teeth : 0;
                gears[i].localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private static Mesh CreateGearMesh(int teeth)
        {
            // Original low-poly spur silhouette: four profile corners per tooth, extruded along Z.
            var count = teeth * 4; var vertices = new Vector3[count * 2 + 2]; var triangles = new int[count * 12];
            var pitch = teeth * .015f;
            for (var i = 0; i < count; i++)
            {
                var angle = (i - 1.5f) * Mathf.PI * 2 / count;
                var radius = pitch + (i % 4 == 1 || i % 4 == 2 ? .012f : -.014f);
                vertices[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -.04f);
                vertices[i + count] = new Vector3(vertices[i].x, vertices[i].y, .04f);
            }
            vertices[count * 2] = new Vector3(0, 0, -.04f); vertices[count * 2 + 1] = new Vector3(0, 0, .04f);
            for (var i = 0; i < count; i++)
            {
                var next = (i + 1) % count; var t = i * 12;
                triangles[t] = count * 2; triangles[t + 1] = next; triangles[t + 2] = i;
                triangles[t + 3] = count * 2 + 1; triangles[t + 4] = i + count; triangles[t + 5] = next + count;
                triangles[t + 6] = i; triangles[t + 7] = next; triangles[t + 8] = next + count;
                triangles[t + 9] = i; triangles[t + 10] = next + count; triangles[t + 11] = i + count;
            }
            var mesh = new Mesh { name = "VLAB original " + teeth + " tooth gear", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        protected override void OnDestroy()
        {
            foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
            base.OnDestroy();
        }
    }
}
