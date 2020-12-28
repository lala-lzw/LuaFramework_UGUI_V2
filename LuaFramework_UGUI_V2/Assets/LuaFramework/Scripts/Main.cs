using UnityEngine;
using System.Collections;
using System;

namespace LuaFramework {

    /// <summary>
    /// </summary>
    public class Main : MonoBehaviour {

        public void Start() {
            //并创建一个appfacade。单例，调用startUp函数
            //进入appfacade
            AppFacade.Instance.StartUp();   //启动游戏

        }
    }
}