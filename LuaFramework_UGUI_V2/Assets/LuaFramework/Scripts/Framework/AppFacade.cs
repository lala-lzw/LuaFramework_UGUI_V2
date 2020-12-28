using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class AppFacade : Facade
{
    private static AppFacade _instance;
    //父亲哪里调用了initFramework
    public AppFacade() : base()
    {
    }
    //单例
    public static AppFacade Instance
    {
        get{
            if (_instance == null) {
                _instance = new AppFacade();
            }
            return _instance;
        }
    }
    //初始化controller以及在controller里面注册StartUpCommand
    override protected void InitFramework()
    {
        base.InitFramework();
        RegisterCommand(NotiConst.START_UP, typeof(StartUpCommand));
    }

    /// <summary>
    /// 启动框架
    /// </summary>
    public void StartUp() {
        //去facade查看命令的作用
        //    public const string START_UP = "StartUp";    
        SendMessageCommand(NotiConst.START_UP);
        RemoveMultiCommand(NotiConst.START_UP);
    }
}

