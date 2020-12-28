
using LuaInterface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIEventEx 
{
    //添加button点击监听
    public static void AddButtonClick(GameObject go,LuaFunction luaFunction)
    {
        //传参错误
        if(go == null || luaFunction == null)
        {
            Debug.Log("go or LuaFunction is null");
            return;
        }
        Button btn = go.GetComponent<Button>();
        if(btn == null)
        {
            return;
        }
        btn.onClick.AddListener(
            delegate ()
            {
                luaFunction.Call(go);
            }
        );
    }
    /// <summary>
    /// 给toggler
    /// </summary>
    public static void AddToggle(GameObject go,LuaFunction luaFunction,LuaTable luaTable)
    {
        if(go == null || luaFunction == null)
        {
            Debug.Log(" go or luafunc is null");
            return;
        }
        Toggle toggle = go.GetComponent<Toggle>();
        if(toggle == null)
        {
            Debug.Log("toggle is null,please addComponent");
            return;
        }
        go.GetComponent<Toggle>().onValueChanged.AddListener(
            delegate(bool select){
                luaFunction.Call(luaTable, select);
            });
    }
    
    public static void AddToggle(GameObject go, LuaFunction luaFunction)
    {
        if (go == null || luaFunction == null)
        {
            Debug.Log(" go or luafunc is null");
            return;
        }
        Toggle toggle = go.GetComponent<Toggle>();
        if (toggle == null)
        {
            Debug.Log("toggle is null,please addComponent");
            return;
        }
        go.GetComponent<Toggle>().onValueChanged.AddListener(
            delegate (bool select) {
                luaFunction.Call(go,select);
            });
    }
    

    public static void AddInputFieldEndEditHandler(GameObject go,LuaFunction luaFunction)
    {
        if (go == null || luaFunction == null)
        {
            Debug.Log(" go or luafunc is null");
            return;
        }
        InputField inputField  = go.GetComponent<InputField>();
        if (inputField == null)
        {
            Debug.Log("inputfield is null,please addComponent");
            return;
        }
        go.GetComponent<InputField>().onEndEdit.AddListener(
            delegate (string text) {
                luaFunction.Call(text);
            });
    }
    /// <summary>
    ///    添加对光标按下抬起事件的支持
    ///go 目标对象
    /// </summary>
    /// <param name="go"></param>
    /// <param name="luaFunction1">按下触发</param>
    /// <param name="luaFunction2">抬起触发</param>
    public static void AddPointerDownUpSupport(GameObject go,LuaFunction luaFunction1,LuaFunction luaFunction2)
    {
        if(go == null)
        {
            return;
        }
        EventsSupport eventsSupport = go.AddComponent<EventsSupport>();
        eventsSupport.InitDownUpHandler(
            (PointerEventData pointerEventData) => {
            if (luaFunction1 != null)
            {
                luaFunction1.Call(go, pointerEventData);
            }
        },(PointerEventData pointerEventData) => { 
            if(luaFunction2 != null)
            {
                luaFunction2.Call(go, pointerEventData);
            }

        });
    }
    /// <summary>
    /// 添加slider监听
    /// </summary>
    /// <param name="go"></param>
    /// <param name="luaFunction"></param>
    public static void AddSliderOnChangeEvent(GameObject go,LuaFunction luaFunction)
    {
        if (go == null || luaFunction == null)
        {
            Debug.Log(" go or luafunc is null");
            return;
        }
        Slider slider = go.GetComponent<Slider>();
        if(slider == null)
        {
            Debug.Log("slider is null ,please add this Compoment");
                return;
        }
        go.GetComponent<Slider>().onValueChanged.AddListener(
            delegate (float val)
            {
                luaFunction.Call(val);
            }
        );
    }
    /// <summary>
    /// 清楚绑定
    /// </summary>
    /// <param name="go"></param>
    public static void ClearButtonClick(GameObject go)
    {
        if(go == null)
        {
            return;
        }
        Button btn = go.GetComponent<Button>();
        if(btn == null)
        {
            return;
        }
        btn.onClick.RemoveAllListeners();
    }
}
