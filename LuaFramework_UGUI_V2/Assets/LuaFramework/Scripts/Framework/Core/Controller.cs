/* 
 LuaFramework Code By Jarjin lee
*/

using System;
using System.Collections.Generic;
using UnityEngine;

public class Controller : IController {  
    //
    //controller里面定义两个字典，一个装命令，一个装视图
    protected IDictionary<string, Type> m_commandMap;
    protected IDictionary<IView, List<string>> m_viewCmdMap;
    //controller引用
    protected static volatile IController m_instance;
    //
    protected readonly object m_syncRoot = new object();
    protected static readonly object m_staticSyncRoot = new object();

    protected Controller() {
        InitializeController();
    }

    static Controller() {
    }
    //没有创建，有则返回
    public static IController Instance {
        get {
            if (m_instance == null) {
                lock (m_staticSyncRoot) {
                    if (m_instance == null) m_instance = new Controller();
                }
            }
            return m_instance;
        }
    }
    //初始化主要就是给两个字典分配内存
    protected virtual void InitializeController() {
        m_commandMap = new Dictionary<string, Type>();
        m_viewCmdMap = new Dictionary<IView, List<string>>();
    }
    /// <summary>
    /// 执行command，
    /// 如果已知字典里面已经添加了他，就执行喔，（）
    /// 如果没有就在view里面操作，暂时不明意义
    /// 
    /// </summary>
    /// <param name="note"></param>
    public virtual void ExecuteCommand(IMessage note) {
        
        //message 有三个变量name(str)，type(str)，body(obj)
        Type commandType = null;
        List<IView> views = null;
        lock (m_syncRoot) {
            Debug.Log("执行命令的类型"+note.Type);
            //主要任务在命令里面添加他（startUp）
            if (m_commandMap.ContainsKey(note.Name)) {
                //Debug.Log("1ccccccccccc" + note.Name);

                commandType = m_commandMap[note.Name];
            } else {
                views = new List<IView>();
                foreach (var de in m_viewCmdMap) {
                    if (de.Value.Contains(note.Name)) {
                        //Debug.Log("ccccccccccc  VIEWS aDD" + de.Key);

                        views.Add(de.Key);
                    }
                }
            }
        }
        if (commandType != null) {  //Controller
            object commandInstance = Activator.CreateInstance(commandType);
            if (commandInstance is ICommand) {
                Debug.Log("如果command继承了ICommand就执行他的execute方法:"+commandInstance.ToString());
                
                ((ICommand)commandInstance).Execute(note);
            }
        }
        if (views != null && views.Count > 0) {
            //Debug.Log("view不为空 " + views.Count);
            for (int i = 0; i < views.Count; i++) {
                views[i].OnMessage(note);
            }
            views = null;
        }
    }
    //往字典command里面添加元素StratUP
    public virtual void RegisterCommand(string commandName, Type commandType) {
        lock (m_syncRoot) {
            Debug.Log("正在添加command " + commandName);
            m_commandMap[commandName] = commandType;
        }
    }
    //添加view元素,如果已经有了元素了就添加该元素去对应存的建，没有就添加主键
    public virtual void RegisterViewCommand(IView view, string[] commandNames) {
        lock (m_syncRoot) {
            if (m_viewCmdMap.ContainsKey(view)) {
                List<string> list = null;
                if (m_viewCmdMap.TryGetValue(view, out list)) {
                    for (int i = 0; i < commandNames.Length; i++) {
                        if (list.Contains(commandNames[i])) continue;
                        list.Add(commandNames[i]);
                    }
                }
            } else {
                m_viewCmdMap.Add(view, new List<string>(commandNames));
            }
        }
    }
    //判断是否已经有了该元素
    public virtual bool HasCommand(string commandName) {
        lock (m_syncRoot) {
            return m_commandMap.ContainsKey(commandName);
        }
    }
    //去除.有就去除，没有则不管
    public virtual void RemoveCommand(string commandName) {
        lock (m_syncRoot) {
            if (m_commandMap.ContainsKey(commandName)) {
                m_commandMap.Remove(commandName);
            }
        }
    }
    //这里的去除只关注value，对应key是不进行为空时删除的，就行创建的时候也是如此

    public virtual void RemoveViewCommand(IView view, string[] commandNames) {
        lock (m_syncRoot) {
            if (m_viewCmdMap.ContainsKey(view)) {
                List<string> list = null;
                if (m_viewCmdMap.TryGetValue(view, out list)) {
                    for (int i = 0; i < commandNames.Length; i++) {
                        if (!list.Contains(commandNames[i])) continue;
                        list.Remove(commandNames[i]);
                    }
                }
            }
        }
    }
}

