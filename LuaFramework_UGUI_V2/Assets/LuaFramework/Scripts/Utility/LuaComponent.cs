using LuaInterface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LuaComponent : MonoBehaviour
{
    public LuaTable table;

    public static LuaTable Add(GameObject go,LuaTable tableClass)
    {
        LuaFunction fun = tableClass.GetLuaFunction("New");
        if(fun == null)
        {
            return null;
        }
        object rets = fun.Invoke<LuaTable, object>(tableClass);
        if(rets == null)
        {
            return null;
        }
        LuaComponent cmp = go.AddComponent<LuaComponent>();
        cmp.table = (LuaTable)rets;
        cmp.CallAwake();
        return cmp.table;
    }
    //添加
    public static LuaTable Add(GameObject go, LuaTable tableClass,LuaTable table)
    {
        LuaFunction fun = tableClass.GetLuaFunction("New");
        if (fun == null)
        {
            return null;
        }
        object rets = fun.Invoke<LuaTable, object>(tableClass);
        if (rets == null)
        {
            return null;
        }
        LuaComponent cmp = go.AddComponent<LuaComponent>();
        cmp.table = (LuaTable)rets;
        cmp.CallAwake(table);
        return cmp.table;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="go"></param>
    /// <param name="tableClass"></param>
    /// <param name="isJustAllowOneComponent">true则只添加一次组件</param>
    /// <returns></returns>
    public static LuaTable Add(GameObject go, LuaTable tableClass,bool isJustAllowOneComponent)
    {
        LuaComponent luaComponent = go.GetComponent<LuaComponent>();
        if (luaComponent != null && isJustAllowOneComponent)
            return null;
        LuaFunction fun = tableClass.GetLuaFunction("New");
        if (fun == null)
        {
            return null;
        }
        object rets = fun.Invoke<LuaTable, object>(tableClass);
        if (rets == null)
        {
            return null;
        }
        LuaComponent cmp = go.AddComponent<LuaComponent>();
        cmp.table = (LuaTable)rets;
        cmp.CallAwake();
        return cmp.table;
    }
    public static LuaTable Get(GameObject go,LuaTable table)
    {
        LuaComponent cmp = go.GetComponent<LuaComponent>();
        string mat1 = table.ToString();
        string mat2 = cmp.table.GetMetaTable().ToString();
        if(mat1 == mat2)
        {
            return cmp.table;
        }

        return null;
    }
    void CallAwake()
    {
        LuaFunction fun = table.GetLuaFunction("Awake");
        if(fun!= null)
        {
            fun.Call(table, gameObject);
        }
    }
    void CallAwake(LuaTable table)
    {
        LuaFunction fun = table.GetLuaFunction("Awake");
        if (fun != null)
        {
            fun.Call(table, gameObject,table);
        }
    }
    private void OnEnable()
    {
        if(table == null)
        {
            return;
        }
        LuaFunction fun = table.GetLuaFunction("OnEnable");
        if(fun!=null)
        {
            fun.Call(table, gameObject);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        LuaFunction fun = table.GetLuaFunction("Start");

        if(fun != null)
        {
            fun.Call(table, gameObject);
        }
    }
    private void FixedUpdate()
    {
        LuaFunction fun = table.GetLuaFunction("FixedUpdate");
        if(fun!=null)
        {
            fun.Call(table, gameObject);
        }
    }
    private void LateUpdate()
    {
        LuaFunction fun = table.GetLuaFunction("LateUpdate");
        if(fun!=null)
        {
            fun.Call(table, gameObject);
        }
    }
    // Update is called once per frame
    void Update()
    {
        LuaFunction fun = table.GetLuaFunction("Update");
        if (fun != null)
        {
            fun.Call(table, gameObject);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        
    }
    private void OnDisable()
    {
        if(table!=null)
        {
            LuaFunction fun = table.GetLuaFunction("OnDisable");
            if(fun != null)
            {
                fun.Call(table, gameObject);
            }
        }
        
    }
    private void OnDestroy()
    {
        if (table != null)
        {
            LuaFunction fun = table.GetLuaFunction("OnDisable");
            if (fun != null)
            {
                fun.Call(table, gameObject);
            }
        }
    }
}
