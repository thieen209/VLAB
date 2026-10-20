#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Test-only Editor Game View sizing. Player Screen.SetResolution cannot size an Editor Game View.
public static class SecondTestGameView
{
    const string Prefix="VLAB.SecondTest.GameView.";
    const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static Type Type(string name)=>typeof(Editor).Assembly.GetType("UnityEditor."+name,true);
    static object Sizes()
    {
        var type=Type("GameViewSizes");
        return type.BaseType.GetProperty("instance",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.FlattenHierarchy).GetValue(null);
    }
    static object Group(object sizes,string groupName)
    {
        var groupType=Type("GameViewSizeGroupType");
        return sizes.GetType().GetMethod("GetGroup",Members).Invoke(sizes,new[]{Enum.Parse(groupType,groupName)});
    }
    static object Invoke(object value,string method,params object[] args)=>value.GetType().GetMethod(method,Members).Invoke(value,args);
    static EditorWindow View()=>EditorWindow.GetWindow(Type("GameView"));
    static PropertyInfo Selected(EditorWindow view)=>view.GetType().GetProperty("selectedSizeIndex",Members);
    public static bool Configure(out string diagnostic)
    {
        diagnostic="";
        try
        {
            if(SessionState.GetBool(Prefix+"configured",false))Restore();
            var sizes=Sizes();var groupProperty=sizes.GetType().GetProperty("currentGroupType",Members);
            string groupName=groupProperty.GetValue(sizes).ToString();var group=Group(sizes,groupName);var view=View();var selected=Selected(view);
            int previous=(int)selected.GetValue(view);int count=(int)Invoke(group,"GetTotalCount");
            string label="VLAB QA 1600x900 "+Guid.NewGuid().ToString("N").Substring(0,8);
            var kind=Enum.Parse(Type("GameViewSizeType"),"FixedResolution");
            var size=Activator.CreateInstance(Type("GameViewSize"),Members,null,new object[]{kind,1600,900,label},null);
            Invoke(group,"AddCustomSize",size);
            SessionState.SetInt(Prefix+"previous",previous);SessionState.SetInt(Prefix+"created",count);
            SessionState.SetString(Prefix+"group",groupName);SessionState.SetBool(Prefix+"configured",true);
            selected.SetValue(view,count);view.Repaint();EditorApplication.QueuePlayerLoopUpdate();
            diagnostic="Selected fixed Game View 1600x900 in "+groupName+"; previous index="+previous+", QA index="+count;
            SessionState.SetString(Prefix+"diagnostic",diagnostic);return true;
        }
        catch(Exception ex)
        {
            diagnostic="Game View fixed-size configuration failed: "+ex;SessionState.SetString(Prefix+"diagnostic",diagnostic);Restore();return false;
        }
    }
    public static string Diagnostic=>SessionState.GetString(Prefix+"diagnostic","");
    public static void Restore()
    {
        if(!SessionState.GetBool(Prefix+"configured",false))return;
        try
        {
            var view=View();Selected(view).SetValue(view,SessionState.GetInt(Prefix+"previous",0));view.Repaint();
            var group=Group(Sizes(),SessionState.GetString(Prefix+"group","Standalone"));
            int custom=SessionState.GetInt(Prefix+"created",-1)-(int)Invoke(group,"GetBuiltinCount");
            if(custom>=0)Invoke(group,"RemoveCustomSize",custom);
        }
        catch(Exception ex){Debug.LogWarning("VLAB QA could not restore previous Game View: "+ex.Message);}
        finally{SessionState.SetBool(Prefix+"configured",false);}
    }
}
#endif
