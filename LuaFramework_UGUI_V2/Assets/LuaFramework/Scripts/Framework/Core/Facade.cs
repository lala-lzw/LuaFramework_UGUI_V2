/* 
    LuaFramework Code By Jarjin lee
*/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件命令
/// </summary>
public class ControllerCommand : ICommand {
    public virtual void Execute(IMessage message) {
    }
}

public class Facade {
    protected IController m_controller;
    //游戏中对应的物体
    static GameObject m_GameManager;
    //上面的物体上面挂载的各种manager脚本
    static Dictionary<string, object> m_Managers = new Dictionary<string, object>();
    //找到目标物体，然后进行脚本挂载
    GameObject AppGameManager {
        get {
            if (m_GameManager == null) {
                m_GameManager = GameObject.Find("GameManager");
            }
            return m_GameManager;
        }
    }
    //初始化Controller命令
    protected Facade() {
        InitFramework();
    }
    //具体初始化，controller（单例）
    protected virtual void InitFramework() {
        
        if (m_controller != null) return;
        m_controller = Controller.Instance;
    }
    //添加command
    public virtual void RegisterCommand(string commandName, Type commandType) {
        m_controller.RegisterCommand(commandName, commandType);
    }
    //去除command
    public virtual void RemoveCommand(string commandName) {
        m_controller.RemoveCommand(commandName);
    }
    //判断是否有command
    public virtual bool HasCommand(string commandName) {
        return m_controller.HasCommand(commandName);
    }
    //批量注册command
    public void RegisterMultiCommand(Type commandType, params string[] commandNames) {
        int count = commandNames.Length;
        for (int i = 0; i < count; i++) {
            RegisterCommand(commandNames[i], commandType);
        }
    }
    //批量去除
    public void RemoveMultiCommand(params string[] commandName) {
        int count = commandName.Length;
        for (int i = 0; i < count; i++) {
            RemoveCommand(commandName[i]);
        }
    }
    //send其实就是执行脚本喔。
    public void SendMessageCommand(string message, object body = null) {
        //进入Controller
        m_controller.ExecuteCommand(new Message(message, body));
    }

    /// <summary>
    /// 添加管理器
    /// </summary>
    public void AddManager(string typeName, object obj) {
        if (!m_Managers.ContainsKey(typeName)) {
            m_Managers.Add(typeName, obj);
        }
    }

    /// <summary>
    /// 添加Unity对象，在响应的obj上面添加
    /// </summary>
    public T AddManager<T>(string typeName) where T : Component {
        object result = null;
        m_Managers.TryGetValue(typeName, out result);
        if (result != null) {
            return (T)result;
        }
        Component c = AppGameManager.AddComponent<T>();
        m_Managers.Add(typeName, c);
        return default(T);
    }

    /// <summary>
    /// 获取系统管理器
    /// </summary>
    public T GetManager<T>(string typeName) where T : class {
        if (!m_Managers.ContainsKey(typeName)) {
            return default(T);
        }
        object manager = null;
        m_Managers.TryGetValue(typeName, out manager);
        return (T)manager;
    }

    /// <summary>
    /// 删除管理器
    /// </summary>
    public void RemoveManager(string typeName) {
        if (!m_Managers.ContainsKey(typeName)) {
            return;
        }
        object manager = null;
        m_Managers.TryGetValue(typeName, out manager);
        Type type = manager.GetType();
        if (type.IsSubclassOf(typeof(MonoBehaviour))) {
            GameObject.Destroy((Component)manager);
        }
        m_Managers.Remove(typeName);
    }
}
