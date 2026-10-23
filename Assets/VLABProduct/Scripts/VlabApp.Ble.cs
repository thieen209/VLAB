using UnityEngine;

namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        void ShowBleController(int start=0,bool services=false)
        {
            var ble=Input.Ble;if(!ble)return;
            var box=Modal(L("Kết nối tay cầm BLE","BLE controller connection"),1100,840);
            string state=ble.Fresh?L("Đang nhận thao tác","Receiving input"):L("Chưa có thao tác hợp lệ","No valid input yet");
            var statusLabel=ui.Text(box,state,40,108,1020,84,24);StartCoroutine(BleStatus(statusLabel,false));
            ui.Text(box,ble.Available?L("Quét, chọn tay cầm, rồi chọn kênh dữ liệu. Giao thức phải khớp firmware.","Scan, select the controller, then its data channel. The protocol must match the firmware."):L("Quét Bluetooth hoạt động trên Android. Cấu hình và kiểm thử giả lập tách riêng.","Bluetooth scanning requires Android. Configuration and simulation are separate."),40,204,1020,72,24,VlabUi.Secondary);
            Button(box,L("Quét 10 giây","Scan for 10 seconds"),40,290,320,64,()=>{ble.Scan();ShowBleController();}).interactable=ble.Available;
            Button(box,L("Cập nhật","Refresh"),390,290,320,64,()=>ShowBleController(start,services));
            Button(box,L("Ngắt kết nối","Disconnect"),740,290,320,64,()=>{ble.Stop();ShowBleController();});
            int count=services?ble.Services.Length:ble.Devices.Count;
            start=Mathf.Clamp(start,0,Mathf.Max(0,((count-1)/3)*3));
            for(int i=start;i<Mathf.Min(count,start+3);i++)
            {
                int row=i-start;
                if(services)
                {
                    var channel=ble.Services[i];
                    var button=Button(box,channel.characteristic,40,370+row*76,1020,64,()=>
                    {
                        var profiles=JsonUtility.FromJson<BleProfileCollection>(JsonUtility.ToJson(ble.Profiles));
                        profiles.Selected.service=channel.service;profiles.Selected.characteristic=channel.characteristic;
                        if(!ble.Apply(profiles,out _))Toast(L("Không lưu được cấu hình.","Configuration could not be saved."));
                        else if(profiles.Selected.address!="")ble.Connect(profiles.Selected.address,false);
                        ShowBleController();
                    });button.interactable=(channel.properties&48)!=0;
                }
                else
                {
                    var device=ble.Devices[i];Button(box,BleControllerRuntime.Plain(device.name)+" · "+device.rssi+" dBm",40,370+row*76,1020,64,()=>{ble.Connect(device.address);ShowBleController(0,true);});
                }
            }
            Button(box,L("Trước","Previous"),40,610,240,64,()=>ShowBleController(start-3,services)).interactable=start>0;
            Button(box,L("Sau","Next"),300,610,240,64,()=>ShowBleController(start+3,services)).interactable=start+3<count;
            Button(box,services?L("Thiết bị","Devices"):L("Kênh dữ liệu","Data channels"),560,610,470,64,()=>ShowBleController(0,!services));
            ui.Text(box,ble.Profiles.Selected.Configured?L("Cấu hình gói đã lưu; cần dữ liệu thực để xác minh.","Packet configuration saved; live data is required to verify it."):L("Chưa cấu hình gói firmware: có thể xem thiết bị/kênh, chưa điều khiển được.","Firmware packet format is not configured: devices/channels can be inspected, input is unavailable."),40,690,1020,60,22,VlabUi.Secondary);
            Button(box,L("Cấu hình / hiệu chỉnh","Profiles / calibration"),40,760,470,60,ShowBleProfiles);
            Button(box,L("Quay lại","Back"),560,760,470,60,()=>{ble.StopScan();CloseModal();ShowSettings();});
        }
        void ShowBleProfiles()
        {
            var ble=Input.Ble;var box=Modal(L("Cấu hình tay cầm","Controller profiles"),1100,840);
            var selected=ble.Profiles.Selected;
            ui.Text(box,BleControllerRuntime.Plain(selected.name)+" · "+(ble.Profiles.active+1)+" / "+ble.Profiles.profiles.Count,40,108,1020,60,28);
            ui.Text(box,L("Cấu hình gói dành cho firmware. Chỉ nhập JSON do người phát triển tay cầm cung cấp; không tự đoán UUID hoặc trục hướng.","Packet profiles are firmware-specific. Import JSON supplied by the controller developer; do not guess UUIDs or orientation axes."),40,180,1020,90,24,VlabUi.Secondary);
            Button(box,L("Đổi hồ sơ","Switch profile"),40,282,470,64,()=>ChangeBleProfile(p=>p.active=(p.active+1)%p.profiles.Count));
            Button(box,L("Nhân bản hồ sơ","Duplicate profile"),560,282,470,64,()=>ChangeBleProfile(p=>{if(p.profiles.Count>=8)return;p.profiles.Add(p.Selected.Copy());p.active=p.profiles.Count-1;})).interactable=ble.Profiles.profiles.Count<8;
            Button(box,L("Sao chép JSON","Copy JSON"),40,360,470,64,()=>{GUIUtility.systemCopyBuffer=JsonUtility.ToJson(selected,true);Toast(L("Đã sao chép cấu hình.","Profile copied."));});
            Button(box,L("Nhập JSON đã sao chép","Import copied JSON"),560,360,470,64,()=>
            {
                try
                {
                    string json=GUIUtility.systemCopyBuffer;
                    if(string.IsNullOrWhiteSpace(json)||json.Length>16384)throw new System.FormatException();
                    var imported=JsonUtility.FromJson<BleControllerProfile>(json);if(imported==null)throw new System.FormatException();imported.Validate();
                    Confirm(L("Thay cấu hình tay cầm đang chọn? Kết nối hiện tại sẽ ngắt.","Replace the selected controller profile? The current connection will stop."),()=>ChangeBleProfile(p=>p.profiles[p.active]=imported));
                }
                catch(System.Exception e)when(e is System.ArgumentException||e is System.FormatException||e is System.IO.InvalidDataException)
                {Toast(L("JSON không hợp lệ. Hồ sơ cũ vẫn được giữ.","Invalid JSON. Existing profiles are preserved."));}
            });
            Button(box,L("Hiệu chỉnh cần điều khiển","Calibrate joystick"),40,438,470,64,ShowBleCalibration);
            Button(box,L("Bluetooth hệ thống / HID","System Bluetooth / HID"),560,438,470,64,()=>{if(!Input.OpenControllerPairing())Toast(L("Mở Bluetooth trong cài đặt Android.","Open Bluetooth in Android Settings."));}).interactable=Input.CanPairController;
            Button(box,L("Vùng chết −","Deadzone −"),40,516,470,64,()=>ChangeBleProfile(p=>p.Selected.deadzone=Mathf.Max(.05f,p.Selected.deadzone-.05f)));
            Button(box,L("Vùng chết +","Deadzone +"),560,516,470,64,()=>ChangeBleProfile(p=>p.Selected.deadzone=Mathf.Min(.5f,p.Selected.deadzone+.05f)));
            ui.Text(box,L("Vùng chết: ","Deadzone: ")+selected.deadzone.ToString("P0")+"\n"+L("Thay cấu hình sẽ ngắt kết nối; kết nối lại để sử dụng.","Profile changes disconnect the controller; reconnect to use them."),40,594,1020,76,24,VlabUi.Secondary);
            Button(box,L("Đặt lại hồ sơ BLE","Reset BLE profiles"),40,684,470,64,()=>Confirm(L("Lưu bản cũ để khôi phục và đặt lại riêng hồ sơ BLE? Sổ tay không thay đổi.","Archive existing BLE profiles and reset them? Your notebook is preserved."),()=>{if(!ble.ResetProfiles(out _))Toast(L("Chưa đặt lại được.","Reset failed."));ShowBleProfiles();}));
            Button(box,L("Gán chức năng nút","Button mapping"),560,684,470,64,ShowControllerBindings);
            Button(box,L("Quay lại kết nối","Back to connection"),40,760,1020,60,()=>ShowBleController());
        }
        void ChangeBleProfile(System.Action<BleProfileCollection> change)
        {
            var profiles=JsonUtility.FromJson<BleProfileCollection>(JsonUtility.ToJson(Input.Ble.Profiles));change(profiles);
            if(!Input.Ble.Apply(profiles,out _))Toast(L("Không lưu được cấu hình; dữ liệu cũ được giữ.","Could not save the configuration; existing data is preserved."));
            ShowBleProfiles();
        }
        void ShowBleCalibration()
        {
            var ble=Input.Ble;var box=Modal(L("Hiệu chỉnh joystick","Joystick calibration"),1100,760);
            ui.Text(box,L("1. Thả cần về giữa, rồi chọn Đo tâm.\n2. Chọn Đo biên, xoay cần hết vòng ít nhất 2 giây.\n3. Chọn Lưu biên. Sau mỗi lần lưu, kết nối lại tay cầm.","1. Release the stick, then choose Measure centre.\n2. Choose Measure range and move around the full rim for at least 2 seconds.\n3. Save range. Reconnect after each save."),40,110,1020,160,26);
            var statusLabel=ui.Text(box,"",40,290,1020,64,24);StartCoroutine(BleStatus(statusLabel,true));
            Button(box,L("Đo tâm","Measure centre"),40,376,470,64,()=>{ble.BeginCalibration(false);ShowBleCalibration();});
            Button(box,L("Đo biên","Measure range"),560,376,470,64,()=>{ble.BeginCalibration(true);ShowBleCalibration();});
            Button(box,L("Lưu biên","Save range"),40,464,470,64,()=>{ble.FinishCalibration();ShowBleCalibration();});
            Button(box,L("Cập nhật trạng thái","Refresh status"),560,464,470,64,ShowBleCalibration);
            Button(box,L("Hủy hiệu chỉnh","Cancel calibration"),40,552,1020,64,()=>{ble.CancelCalibration();ShowBleProfiles();});
            Button(box,L("Quay lại kết nối","Back to connection"),40,660,1020,64,()=>{ble.CancelCalibration();ShowBleController();});
        }
        System.Collections.IEnumerator BleStatus(TMPro.TextMeshProUGUI label,bool calibration)
        {
            var ble=Input.Ble;
            while(label&&label.gameObject.activeInHierarchy)
            {
                if(calibration)label.text=L("Mẫu nhận: ","Samples: ")+ble.CalibrationSamples+" · "+BleStatusLabel(ble.Calibration);
                else label.text=BleStatusLabel(ble.State)+" · "+(ble.Fresh?L("Có dữ liệu hợp lệ","Valid input"):L("Chưa có dữ liệu hợp lệ","No valid input"))+"\n"+L("Gói nhận / từ chối: ","Accepted / rejected: ")+ble.Provider.Accepted+" / "+ble.Provider.Rejected;
                yield return new WaitForSecondsRealtime(.2f);
            }
        }
        string BleStatusLabel(string value)
        {
            switch(value)
            {
                case "Idle":return L("Sẵn sàng","Idle");case "Scanning":return L("Đang quét","Scanning");
                case "Connecting":return L("Đang kết nối","Connecting");case "Discovering":return L("Đang tìm kênh dữ liệu","Discovering channels");
                case "Inspecting":return L("Chọn kênh dữ liệu","Select a data channel");case "Subscribing":return L("Đang đăng ký nhận dữ liệu","Subscribing");
                case "Ready":return L("Đã kết nối","Connected");case "Stopped":return L("Đã ngắt","Disconnected");
                case "Error":return L("Lỗi kết nối hoặc quyền Bluetooth","Connection or Bluetooth permission error");
                case "Reconnecting":return L("Đang kết nối lại","Reconnecting");
                case "centre":return L("Giữ cần ở giữa","Keep the stick centred");case "range":return L("Xoay hết vòng biên","Move around the full rim");
                case "saved":return L("Đã lưu — kết nối lại tay cầm","Saved — reconnect the controller");
                case "noSamples":case "disconnected":return L("Cần kết nối có dữ liệu joystick","Connect a controller with joystick data");
                case "moreSamples":return L("Tiếp tục đo thêm","Keep measuring");case "keepStill":return L("Cần bị di chuyển; đo tâm lại","Stick moved; measure centre again");
                case "fullRange":return L("Chưa đủ biên; xoay hết hành trình","Range incomplete; move to the full limits");
                case "invalidRange":return L("Chưa lưu được; kiểm tra biên đo","Not saved; check measured range");
                default:return string.IsNullOrEmpty(value)?L("Chưa đo","Not measuring"):BleControllerRuntime.Plain(value);
            }
        }
    }
}
