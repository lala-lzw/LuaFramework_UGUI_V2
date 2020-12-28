/*
Copyright (c) 2015-2017 topameng(topameng@qq.com)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using UnityEngine;
using LuaInterface;

public class LuaLooper : MonoBehaviour 
{    
    public LuaBeatEvent UpdateEvent
    {
        get;
        private set;
    }

    public LuaBeatEvent LateUpdateEvent
    {
        get;
        private set;
    }

    public LuaBeatEvent FixedUpdateEvent
    {
        get;
        private set;
    }

    public LuaState luaState = null;
    //初始化三个update
    void Start() 
    {
        try
        {
            UpdateEvent = GetEvent("UpdateBeat");
            LateUpdateEvent = GetEvent("LateUpdateBeat");
            FixedUpdateEvent = GetEvent("FixedUpdateBeat");
        }
        catch (Exception e)
        {
            Destroy(this);
            throw e;
        }        
	}
    /// <summary>
    /// 访问状态机里面响应类（table）得到对应函数委托事件
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    LuaBeatEvent GetEvent(string name)
    {
        LuaTable table = luaState.GetTable(name);

        if (table == null)
        {
            throw new LuaException(string.Format("Lua table {0} not exists", name));
        }

        LuaBeatEvent e = new LuaBeatEvent(table);
        table.Dispose();
        table = null;
        return e;
    }
    /// <summary>
    /// 抛出异常
    /// </summary>
    void ThrowException()
    {
        string error = luaState.LuaToString(-1);
        luaState.LuaPop(2);                
        throw new LuaException(error, LuaException.GetLastError());
    }

    void Update()
    {
#if UNITY_EDITOR
        if (luaState == null)
        {
            //Debug.Log("luastate is null");
            return;
        }
#endif
        if (luaState.LuaUpdate(Time.deltaTime, Time.unscaledDeltaTime) != 0)
        {
            Debug.Log("Update error");
            ThrowException();
        }
        //Debug.Log("Update zhixing");
        luaState.LuaPop(1);
        luaState.Collect();
#if UNITY_EDITOR
        luaState.CheckTop();
#endif
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
        if (luaState == null)
       {
            //Debug.Log("latestate null");
            return;
        }
#endif
        if (luaState.LuaLateUpdate() != 0)
        {
            Debug.Log("lateUpdate error");
            ThrowException();
        }

        //Debug.Log("lateupdate zhixing");
        luaState.LuaPop(1);
    }

    void FixedUpdate()
    {
#if UNITY_EDITOR
        if (luaState == null)
        {
            //Debug.Log("fixedupdate state null");
            return;
        }
#endif
        if (luaState.LuaFixedUpdate(Time.fixedDeltaTime) != 0)
        {
            Debug.Log("fixedUpdate error");
            ThrowException();
        }
        //Debug.Log("fixedUpdate zhixing");
        luaState.LuaPop(1);
    }

    public void Destroy()
    {
        if (luaState != null)
        {
            if (UpdateEvent != null)
            {
                UpdateEvent.Dispose();
                UpdateEvent = null;
            }

            if (LateUpdateEvent != null)
            {
                LateUpdateEvent.Dispose();
                LateUpdateEvent = null;
            }

            if (FixedUpdateEvent != null)
            {
                FixedUpdateEvent.Dispose();
                FixedUpdateEvent = null;
            }

            luaState = null;
        }
    }

    void OnDestroy()
    {
        if (luaState != null)
        {
            Destroy();
        }
    }
}
