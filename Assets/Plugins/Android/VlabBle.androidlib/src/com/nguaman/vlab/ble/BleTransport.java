package com.nguaman.vlab.ble;

import android.Manifest;
import android.bluetooth.*;
import android.bluetooth.le.*;
import android.content.Context;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.os.SystemClock;
import android.util.Base64;
import org.json.JSONArray;
import org.json.JSONObject;
import java.util.ArrayDeque;
import java.util.UUID;

/** Foreground read/notify-only GATT client. Unity polls bounded events on its own thread. */
public final class BleTransport {
    private static final UUID CCCD=UUID.fromString("00002902-0000-1000-8000-00805f9b34fb");
    private final Context context;
    private final Handler main=new Handler(Looper.getMainLooper());
    private final BluetoothAdapter adapter;
    private final ArrayDeque<Event> events=new ArrayDeque<>();
    private BluetoothGatt gatt;
    private BluetoothLeScanner scanner;
    private BluetoothGattCharacteristic characteristic;
    private String serviceUuid="",characteristicUuid="",address="";
    private boolean wanted,reconnect,scanning;
    private int retries,scanEpoch;
    private volatile int generation;
    private volatile String state="Idle";
    private volatile boolean disposed;
    private static final class Event {
        final JSONObject value;final long at;
        Event(JSONObject v,long time){value=v;at=time;}
    }
    public BleTransport(Context value) {
        context=value.getApplicationContext();BluetoothManager manager=(BluetoothManager)context.getSystemService(Context.BLUETOOTH_SERVICE);
        adapter=manager==null?null:manager.getAdapter();
    }
    private JSONObject event(String type) {
        JSONObject value=new JSONObject();try{value.put("type",type).put("generation",generation);}catch(Exception ignored){}return value;
    }
    private void emit(JSONObject value){emit(value,SystemClock.elapsedRealtimeNanos());}
    private void emit(JSONObject value,long at){synchronized(events){while(events.size()>=128)events.removeFirst();events.addLast(new Event(value,at));}}
    private void status(String next,String code) {
        state=next;JSONObject value=event("state");try{value.put("state",next).put("code",code);}catch(Exception ignored){}emit(value);
    }
    public String poll() {
        synchronized(events){if(events.isEmpty())return "";Event item=events.removeFirst();try{item.value.put("queuedMs",(SystemClock.elapsedRealtimeNanos()-item.at)/1000000.0);}catch(Exception ignored){}return item.value.toString();}
    }
    public String state(){if(!permissions(false)&&!state.equals("Idle")&&!state.equals("Stopped")){if(!state.equals("Error"))main.post(()->failure("permission",false));return "Error";}return state;}
    public int generation(){return generation;}
    private boolean permissions(boolean scan) {
        if(Build.VERSION.SDK_INT>=31)return context.checkSelfPermission(Manifest.permission.BLUETOOTH_CONNECT)==PackageManager.PERMISSION_GRANTED&&(!scan||context.checkSelfPermission(Manifest.permission.BLUETOOTH_SCAN)==PackageManager.PERMISSION_GRANTED);
        return !scan||context.checkSelfPermission(Manifest.permission.ACCESS_FINE_LOCATION)==PackageManager.PERMISSION_GRANTED;
    }
    private boolean available(boolean scan) {
        if(disposed)return false;
        if(adapter==null||!context.getPackageManager().hasSystemFeature(PackageManager.FEATURE_BLUETOOTH_LE)){status("Error","unsupported");return false;}
        if(!permissions(scan)){status("Error","permission");return false;}
        try{if(!adapter.isEnabled()){status("Error","bluetoothOff");return false;}}catch(SecurityException e){status("Error","permission");return false;}return true;
    }
    private final ScanCallback scanCallback=new ScanCallback() {
        @Override public void onScanResult(int callbackType,ScanResult result) {
            main.post(()->{
                if(!scanning||disposed||!permissions(true))return;
                try{BluetoothDevice device=result.getDevice();String name=result.getScanRecord()==null?device.getName():result.getScanRecord().getDeviceName();
                    JSONObject value=event("device");value.put("address",device.getAddress()).put("name",name==null?"":name).put("rssi",result.getRssi());emit(value);
                }catch(Exception e){status("Error","scanResult");}
            });
        }
        @Override public void onScanFailed(int code){main.post(()->{stopScanNow();status("Error","scan:"+code);});}
    };
    public void scan(){main.post(()->{
        if(!available(true))return;if(gatt!=null){status(state,"stopBeforeScan");return;}stopScanNow();
        try{scanner=adapter.getBluetoothLeScanner();if(scanner==null){status("Error","bluetoothOff");return;}scanning=true;int epoch=++scanEpoch;status("Scanning","");scanner.startScan(scanCallback);
            main.postDelayed(()->{if(epoch==scanEpoch&&scanning){stopScanNow();status(gatt==null?"Idle":state,"scanFinished");}},10000);
        }catch(SecurityException e){stopScanNow();status("Error","permission");}catch(Exception e){stopScanNow();status("Error","scanStart");}
    });}
    private void stopScanNow(){scanning=false;scanEpoch++;if(scanner!=null){try{scanner.stopScan(scanCallback);}catch(Exception ignored){}scanner=null;}}
    public void stopScan(){main.post(()->{stopScanNow();status(gatt==null?"Idle":state,"");});}
    public void connect(String deviceAddress,String service,String notification,boolean allowReconnect) {
        main.post(()->{
            if(!available(false))return;
            try{if(!BluetoothAdapter.checkBluetoothAddress(deviceAddress))throw new IllegalArgumentException();if(!service.isEmpty())UUID.fromString(service);if(!notification.isEmpty())UUID.fromString(notification);}catch(Exception e){status("Error","configuration");return;}
            stopScanNow();wanted=false;closeGatt();address=deviceAddress;serviceUuid=service;characteristicUuid=notification;reconnect=allowReconnect;retries=0;wanted=true;connectNow();
        });
    }
    private void connectNow() {
        if(!wanted||!available(false))return;generation++;synchronized(events){events.clear();}status("Connecting","");
        try{gatt=adapter.getRemoteDevice(address).connectGatt(context,false,callback,BluetoothDevice.TRANSPORT_LE);if(gatt==null){failure("connect",true);return;}final BluetoothGatt expected=gatt;main.postDelayed(()->{if(gatt==expected&&!state.equals("Ready")&&!state.equals("Inspecting"))failure("timeout",true);},12000);}
        catch(SecurityException e){failure("permission",false);}catch(Exception e){failure("connect",true);}
    }
    private void closeGatt() {
        BluetoothGatt old=gatt;gatt=null;characteristic=null;generation++;synchronized(events){events.clear();}
        if(old!=null){try{old.disconnect();}catch(Exception ignored){}try{old.close();}catch(Exception ignored){}}
    }
    private void failure(String code,boolean retry) {
        closeGatt();
        if(wanted&&reconnect&&retry&&retries<3&&!disposed){int attempt=++retries;int epoch=generation;status("Retrying",code+":"+attempt);main.postDelayed(()->{if(wanted&&generation==epoch&&state.equals("Retrying"))connectNow();},1000L<<(attempt-1));}
        else{wanted=false;status("Error",code);}
    }
    private void discovered(BluetoothGatt source,int result) {
        if(source!=gatt||disposed)return;
        if(result!=BluetoothGatt.GATT_SUCCESS){failure("discovery:"+result,true);return;}
        try{
            JSONArray list=new JSONArray();int count=0;
            for(BluetoothGattService service:source.getServices())for(BluetoothGattCharacteristic item:service.getCharacteristics()){
                if(count++>=64)break;JSONObject row=new JSONObject();row.put("service",service.getUuid().toString()).put("characteristic",item.getUuid().toString()).put("properties",item.getProperties());list.put(row);
            }
            JSONObject value=event("services");value.put("services",list);emit(value);
            if(serviceUuid.isEmpty()||characteristicUuid.isEmpty()){status("Inspecting","configureUuid");return;}
            BluetoothGattService service=source.getService(UUID.fromString(serviceUuid));characteristic=service==null?null:service.getCharacteristic(UUID.fromString(characteristicUuid));
            if(characteristic==null){status("Inspecting","characteristicMissing");return;}
            int properties=characteristic.getProperties();boolean notify=(properties&BluetoothGattCharacteristic.PROPERTY_NOTIFY)!=0,indicate=(properties&BluetoothGattCharacteristic.PROPERTY_INDICATE)!=0;
            if(!notify&&!indicate){status("Inspecting","notNotifiable");return;}
            BluetoothGattDescriptor descriptor=characteristic.getDescriptor(CCCD);
            if(descriptor==null||!source.setCharacteristicNotification(characteristic,true)){status("Inspecting","notificationFailed");return;}
            byte[] bytes=notify?BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE:BluetoothGattDescriptor.ENABLE_INDICATION_VALUE;boolean started;
            if(Build.VERSION.SDK_INT>=33)started=source.writeDescriptor(descriptor,bytes)==BluetoothStatusCodes.SUCCESS;
            else{descriptor.setValue(bytes);started=source.writeDescriptor(descriptor);}
            if(started)status("Subscribing","");else failure("descriptorStart",true);
        }catch(SecurityException e){failure("permission",false);}catch(Exception e){failure("discovery",false);}
    }
    private void notification(BluetoothGatt source,BluetoothGattCharacteristic item,byte[] data,long receivedAt) {
        if(disposed||source!=gatt||!state.equals("Ready")||characteristic==null||!item.getUuid().equals(characteristic.getUuid())||!item.getService().getUuid().equals(characteristic.getService().getUuid())||data==null||data.length>512)return;
        try{JSONObject value=event("packet");value.put("data",Base64.encodeToString(data,Base64.NO_WRAP));emit(value,receivedAt);}catch(Exception ignored){}
    }
    private final BluetoothGattCallback callback=new BluetoothGattCallback() {
        @Override public void onConnectionStateChange(BluetoothGatt source,int status,int next){main.post(()->{
            if(source!=gatt||disposed)return;
            if(status!=BluetoothGatt.GATT_SUCCESS||next==BluetoothProfile.STATE_DISCONNECTED){failure("gatt:"+status,true);return;}
            if(next==BluetoothProfile.STATE_CONNECTED){try{status("Discovering","");if(!source.discoverServices())failure("discoveryStart",true);}catch(SecurityException e){failure("permission",false);}}
        });}
        @Override public void onServicesDiscovered(BluetoothGatt source,int status){main.post(()->discovered(source,status));}
        @Override public void onDescriptorWrite(BluetoothGatt source,BluetoothGattDescriptor descriptor,int result){main.post(()->{
            if(source!=gatt||disposed||characteristic==null||!descriptor.getUuid().equals(CCCD)||!descriptor.getCharacteristic().getUuid().equals(characteristic.getUuid()))return;
            if(result==BluetoothGatt.GATT_SUCCESS)status("Ready","");else failure("descriptor:"+result,result!=5&&result!=15);
        });}
        @Override public void onCharacteristicChanged(BluetoothGatt source,BluetoothGattCharacteristic item,byte[] data){long at=SystemClock.elapsedRealtimeNanos();byte[] copy=data==null?null:data.clone();main.post(()->notification(source,item,copy,at));}
        @Override public void onCharacteristicChanged(BluetoothGatt source,BluetoothGattCharacteristic item){if(Build.VERSION.SDK_INT<33){long at=SystemClock.elapsedRealtimeNanos();byte[] value=item.getValue();byte[] copy=value==null?null:value.clone();main.post(()->notification(source,item,copy,at));}}
    };
    public void stop(){main.post(()->{wanted=false;stopScanNow();closeGatt();status("Stopped","");});}
    public void dispose(){disposed=true;main.post(()->{wanted=false;main.removeCallbacksAndMessages(null);stopScanNow();closeGatt();state="Stopped";});}
}
