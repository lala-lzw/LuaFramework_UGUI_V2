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
//打开开关没有写入导出列表的纯虚类自动跳过
//#define JUMP_NODEFINED_ABSTRACT         

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using System.Diagnostics;
using LuaInterface;

using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;
using Debugger = LuaInterface.Debugger;
using System.Threading;
using LuaFramework;

[InitializeOnLoad]
public static class ToLuaMenu
{
    //不需要导出或者无法导出的类型
    public static List<Type> dropType = new List<Type>
    {
        typeof(ValueType),                                  //不需要
#if UNITY_4_6 || UNITY_4_7
        typeof(Motion),                                     //很多平台只是空类
#endif

#if UNITY_5_3_OR_NEWER
        typeof(UnityEngine.CustomYieldInstruction),
#endif
        typeof(UnityEngine.YieldInstruction),               //无需导出的类      
        typeof(UnityEngine.WaitForEndOfFrame),              //内部支持
        typeof(UnityEngine.WaitForFixedUpdate),
        typeof(UnityEngine.WaitForSeconds),        
        typeof(UnityEngine.Mathf),                          //lua层支持                
        typeof(Plane),                                      
        typeof(LayerMask),                                  
        typeof(Vector3),
        typeof(Vector4),
        typeof(Vector2),
        typeof(Quaternion),
        typeof(Ray),
        typeof(Bounds),
        typeof(Color),                                    
        typeof(Touch),
        typeof(RaycastHit),                                 
        typeof(TouchPhase),     
        //typeof(LuaInterface.LuaOutMetatable),               //手写支持
        typeof(LuaInterface.NullObject),             
        typeof(System.Array),                        
        typeof(System.Reflection.MemberInfo),    
        typeof(System.Reflection.BindingFlags),
        typeof(LuaClient),
        typeof(LuaInterface.LuaFunction),
        typeof(LuaInterface.LuaTable),
        typeof(LuaInterface.LuaThread),
        typeof(LuaInterface.LuaByteBuffer),                 //只是类型标识符
        typeof(DelegateFactory),  
        //无需导出，导出类支持lua函数转换为委托。如UIEventListener.OnClick(luafunc)
    };

    //可以导出的内部支持类型
    public static List<Type> baseType = new List<Type>
    {
        typeof(System.Object),
        typeof(System.Delegate),
        typeof(System.String),
        typeof(System.Enum),
        typeof(System.Type),
        typeof(System.Collections.IEnumerator),
        typeof(UnityEngine.Object),
        typeof(LuaInterface.EventObject),
        typeof(LuaInterface.LuaMethod),
        typeof(LuaInterface.LuaProperty),
        typeof(LuaInterface.LuaField),
        typeof(LuaInterface.LuaConstructor),        
    };

    private static bool beAutoGen = false;
    private static bool beCheck = true;        
    static List<BindType> allTypes = new List<BindType>();
    /// <summary>
    /// 弹出的那个啥
    /// </summary>
    static ToLuaMenu()
    {
        string dir = CustomSettings.saveDir;
        string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly);

        if (files.Length < 3 && beCheck)
        {
            if (EditorUtility.DisplayDialog("自动生成", "点击确定自动生成常用类型注册文件， 也可通过菜单逐步完成此功能", "确定", "取消"))
            {
                beAutoGen = true;
                GenLuaDelegates();
                AssetDatabase.Refresh();
                GenerateClassWraps();
                GenLuaBinder();
                beAutoGen = false;                
            }

            beCheck = false;
        }
    }

    static string RemoveNameSpace(string name, string space)
    {
        if (space != null)
        {
            name = name.Remove(0, space.Length + 1);
        }

        return name;
    }

    public class BindType
    {
        public string name;                 //类名称
        public Type type;
        public bool IsStatic;        
        public string wrapName = "";        //产生的wrap文件名字
        public string libName = "";         //注册到lua的名字
        public Type baseType = null;
        public string nameSpace = null;     //注册到lua的table层级

        public List<Type> extendList = new List<Type>();

        public BindType(Type t)
        {
            if (typeof(System.MulticastDelegate).IsAssignableFrom(t))
            {
                throw new NotSupportedException(string.Format("\nDon't export Delegate {0} as a class, register it in customDelegateList", LuaMisc.GetTypeName(t)));
            }            

            //if (IsObsolete(t))
            //{
            //    throw new Exception(string.Format("\n{0} is obsolete, don't export it!", LuaMisc.GetTypeName(t)));
            //}

            type = t;                        
            nameSpace = ToLuaExport.GetNameSpace(t, out libName);
            //Debug.LogError("1"+nameSpace);
            //1LuaInterface
            name = ToLuaExport.CombineTypeStr(nameSpace, libName);
            //Debug.LogError("2"+name);
            //2LuaInterface.LuaInjectionStation
            libName = ToLuaExport.ConvertToLibSign(libName);
            //Debug.LogError("3"+libName);
            //3LuaInjectionStation
            if (name == "object")
            {
                wrapName = "System_Object";
                name = "System.Object";
            }
            else if (name == "string")
            {
                wrapName = "System_String";
                name = "System.String";
            }
            else
            {
                //eg：UnityEngine_EventSystems_UIBehaviourWrap
                wrapName = name.Replace('.', '_');
                wrapName = ToLuaExport.ConvertToLibSign(wrapName);
            }
            //经过这些得到，name是.形式的字符串，而wrapName是把点偷换成了_


            int index = CustomSettings.staticClassTypes.IndexOf(type);
            //index大于等于零表示有这个元素呗，
            //如果是抽象或者密封类
            if (index >= 0 || (type.IsAbstract && type.IsSealed))
            {
                IsStatic = true;                
            }
            //baseType被赋值为一些基类,正如其名子
            //这里他认为抽象类
            baseType = LuaMisc.GetExportBaseType(type);
        }







        public BindType SetBaseType(Type t)
        {
            baseType = t;
            return this;
        }
        //添加一个Type
        public BindType AddExtendType(Type t)
        {
            if (!extendList.Contains(t))
            {
                extendList.Add(t);
            }

            return this;
        }

        public BindType SetWrapName(string str)
        {
            wrapName = str;
            return this;
        }

        public BindType SetLibName(string str)
        {
            libName = str;
            return this;
        }

        public BindType SetNameSpace(string space)
        {
            nameSpace = space;            
            return this;
        }
        //过时的
        public static bool IsObsolete(Type type)
        {
            //从非托管代码访问托管类，不应该从托管代码调用
            //返回该成员的所有属性
            object[] attrs = type.GetCustomAttributes(true);

            for (int j = 0; j < attrs.Length; j++)
            {
                Type t = attrs[j].GetType();

                //过时或者tolua没有的属性
                if (t == typeof(System.ObsoleteAttribute) || t == typeof(NoToLuaAttribute) || t.Name == "MonoNotSupportedAttribute" || t.Name == "MonoTODOAttribute")
                {
                    return true;
                }
            }

            return false;
        }
    }
    /// <summary>
    /// 究极自动系统，没找到（interface，droptype）就创建，然后再找就找到了
    /// 寻找到直接继承object层和valuetype层为止
    /// </summary>
    /// <param name="bt"></param>
    /// <param name="beDropBaseType"></param>
    static void AutoAddBaseType(BindType bt, bool beDropBaseType)
    {
        
        //Debug.LogError(bt.type + "0");
        Type t = bt.baseType;
        //Debug.LogError(t+"1");
        if (t == null)
        {
            return;
        }
        //最终类，这里不用管，目前是个空的表，且他是为了优化ngui优化
        if (CustomSettings.sealedList.Contains(t))
        {
            CustomSettings.sealedList.Remove(t);
            Debugger.LogError("{0} not a sealed class, it is parent of {1}", LuaMisc.GetTypeName(t), bt.name);
        }
        //如果是接口，就
        //这里出现一个人间迷惑行为   t = bt.baseType;
        //                            bt.basetype = t.basetype;
        //你吧值给我，然后又出现我把值给你的操作，别是和无语，
        //但是后来细想。发现，或许存在父子关系，
        //然后出现这种情况和调赴执行子的思路是一样的
         
        if (t.IsInterface)
        {
            //经过实践证明接口的基类是object，也就是他没有父亲
            //（说法有点问题，但是明白就好了）
            Debugger.LogWarning("{0} has a base type {1} is Interface, use SetBaseType to jump it", bt.name, t.FullName);
            bt.baseType = t.BaseType;
        }
        //dropType好像是黑名单，具体我不记得了
        //就是黑名单，且位置就是再脚本内，而不是再cunstomSetting
        else if (dropType.IndexOf(t) >= 0)
        {
            Debugger.LogWarning("{0} has a base type {1} is a drop type", bt.name, t.FullName);
            bt.baseType = t.BaseType;
        }
        else if (!beDropBaseType || baseType.IndexOf(t) < 0)
        {
            //小于零默认表示为木有找到起始就是-1，没有其他的值
            int index = allTypes.FindIndex((iter) => { return iter.type == t; });

            if (index < 0)
            {
#if JUMP_NODEFINED_ABSTRACT
                if (t.IsAbstract && !t.IsSealed)
                {
                    Debugger.LogWarning("not defined bindtype for {0}, it is abstract class, jump it, child class is {1}", LuaMisc.GetTypeName(t), bt.name);
                    bt.baseType = t.BaseType;
                }
                else
                {
                    Debugger.LogWarning("not defined bindtype for {0}, autogen it, child class is {1}", LuaMisc.GetTypeName(t), bt.name);
                    bt = new BindType(t);
                    allTypes.Add(bt);
                }
#else
                Debugger.LogWarning("not defined bindtype for {0}, autogen it, child class is {1}", LuaMisc.GetTypeName(t), bt.name);                        
                bt = new BindType(t);
                allTypes.Add(bt);
#endif
            }
            else
            {
                //Debug.LogError(bt.baseType+"2");
                return;
            }
        }
        else
        {
            //Debug.LogError(bt.baseType+"3");
            return;
        }
        //Debug.LogError(bt.baseType+"2");
        AutoAddBaseType(bt, beDropBaseType);
    }
    /// <summary>
    /// 个人认为功能是移除重复以及添加basetype里面没有的
    /// </summary>
    /// <param name="list"></param>
    /// <param name="beDropBaseType">默认true</param>
    /// <returns></returns>
    static BindType[] GenBindTypes(BindType[] list, bool beDropBaseType = true)
    {
        allTypes = new List<BindType>(list);
        
        for (int i = 0; i < list.Length; i++)
        {
            //循环去重复
            
            for (int j = i + 1; j < list.Length; j++)
            {
                //抛出异常
                if (list[i].type == list[j].type)
                    throw new NotSupportedException("Repeat BindType:" + list[i].type);
            }
            //如果他在黑名单里面，那就不需要管了
            if (dropType.IndexOf(list[i].type) >= 0)
            {
                Debug.LogWarning(list[i].type.FullName + " in dropType table, not need to export");
                allTypes.Remove(list[i]);
                continue;
            }
            //如果在basetype名单里面，就在all里面移除他
            else if (beDropBaseType && baseType.IndexOf(list[i].type) >= 0)
            {
                Debug.LogWarning(list[i].type.FullName + " is Base Type, not need to export");
                allTypes.Remove(list[i]);
                continue;
            }
            else if (list[i].type.IsEnum)
            {
                continue;
            }
            //最后调用添加
            AutoAddBaseType(list[i], beDropBaseType);
        }
        return allTypes.ToArray();
    }
    /// <summary>
    /// 使用debug查看发现他将customsetting里面的list全都注册了
    /// </summary>
    [MenuItem("Lua/Gen Lua Wrap Files", false, 1)]
    public static void GenerateClassWraps()
    {
        if (!beAutoGen && EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }
        //如果file里面没有generate文件夹，我们就创建他
        if (!File.Exists(CustomSettings.saveDir))
        {
            //Debug.Log("mmmmmmmmmmmmmmmmmmm Exit");
            Directory.CreateDirectory(CustomSettings.saveDir);
        }

        allTypes.Clear();
        //这里是你自己写的要注册到lua的类
        BindType[] typeList = CustomSettings.customTypeList;

        BindType[] list = GenBindTypes(typeList);
        //在末尾添加元素
        ToLuaExport.allTypes.AddRange(baseType);
        //将coustomsetting里面的list都添加啊到toluaexport
        for (int i = 0; i < list.Length; i++)
        {

            Debug.Log("mmmmmmmmmmmmmmmmmmm Exit"+list[i].type.ToString());
            ToLuaExport.allTypes.Add(list[i].type);
        }
        //我觉得可以加一层数据结构，将基本信息存起来使用list存储
        for (int i = 0; i < list.Length; i++)
        {
            ToLuaExport.Clear();
            ToLuaExport.className = list[i].name;
            ToLuaExport.type = list[i].type;
            ToLuaExport.isStaticClass = list[i].IsStatic;            
            ToLuaExport.baseType = list[i].baseType;
            ToLuaExport.wrapClassName = list[i].wrapName;
            ToLuaExport.libClassName = list[i].libName;
            ToLuaExport.extendList = list[i].extendList;
            ToLuaExport.Generate(CustomSettings.saveDir);
        }

        Debug.Log("Generate lua binding files over");
        ToLuaExport.allTypes.Clear();
        allTypes.Clear();        
        AssetDatabase.Refresh();
    }
    /// <summary>
    /// 获得用户添加的类里面的委托类型
    /// </summary>
    /// <returns></returns>
    static HashSet<Type> GetCustomTypeDelegates()
    {
        BindType[] list = CustomSettings.customTypeList;
        HashSet<Type> set = new HashSet<Type>();
        BindingFlags binding = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.Instance;

        for (int i = 0; i < list.Length; i++)
        {
            Type type = list[i].type;
            //搜索字段
            FieldInfo[] fields = type.GetFields(BindingFlags.GetField | BindingFlags.SetField | binding);
            if(fields.Length>0)
            Debug.Log("GetCustomTypeDelegates output fields" + fields[0].Name);
            //也是搜索，约束匹配
            PropertyInfo[] props = type.GetProperties(BindingFlags.GetProperty | BindingFlags.SetProperty | binding);
            MethodInfo[] methods = null;
            //获得方法
            if (type.IsInterface)
            {
                methods = type.GetMethods();
            }
            else
            {
                methods = type.GetMethods(BindingFlags.Instance | binding);
            }
            //通过反射实例对应type类型，是委托就添加
            for (int j = 0; j < fields.Length; j++)
            {
                Type t = fields[j].FieldType;

                if (ToLuaExport.IsDelegateType(t))
                {
                    set.Add(t);
                }
            }

            for (int j = 0; j < props.Length; j++)
            {
                Type t = props[j].PropertyType;

                if (ToLuaExport.IsDelegateType(t))
                {
                    set.Add(t);
                }
            }
            //将方法参数类型是委托加入到里面
            for (int j = 0; j < methods.Length; j++)
            {
                MethodInfo m = methods[j];

                if (m.IsGenericMethod)
                {
                    continue;
                }
                //获取参数
                ParameterInfo[] pifs = m.GetParameters();

                for (int k = 0; k < pifs.Length; k++)
                {
                    Type t = pifs[k].ParameterType;
                    if (t.IsByRef) t = t.GetElementType();

                    if (ToLuaExport.IsDelegateType(t))
                    {
                        set.Add(t);
                    }
                }
            }

        }

        return set;
    }
    //方式都是差不多的
    [MenuItem("Lua/Gen Lua Delegates", false, 2)]
    static void GenLuaDelegates()
    {
        if (!beAutoGen && EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }

        ToLuaExport.Clear();
        List<DelegateType> list = new List<DelegateType>();
        //将一个list里面的元素添加到list里面
        //这里添加不一定是list（这里是附加的，暂时不明为什么会有这个）
        list.AddRange(CustomSettings.customDelegateList);
        //获得委托
        HashSet<Type> set = GetCustomTypeDelegates();        
        //在list里面没有找到和set相等的就把他添加到list
        foreach (Type t in set)
        {
            if (null == list.Find((p) => { return p.type == t; }))
            {
                list.Add(new DelegateType(t));
            }
        }
        //生成打包代码
        ToLuaExport.GenDelegates(list.ToArray());
        set.Clear();
        ToLuaExport.Clear();
        AssetDatabase.Refresh();
        Debug.Log("Create lua delegate over");
    }    
    //将命名空间构建成一个结构树
    static ToLuaTree<string> InitTree()
    {                        
        ToLuaTree<string> tree = new ToLuaTree<string>();
        //ToLuaTree<List<int>> asd = new ToLuaTree<List<int>>();
        ToLuaNode<string> root = tree.GetRoot();        
        BindType[] list = GenBindTypes(CustomSettings.customTypeList);
        
        for (int i = 0; i < list.Length; i++)
        {
            string space = list[i].nameSpace;
            AddSpaceNameToTree(tree, root, space);
        }

        DelegateType[] dts = CustomSettings.customDelegateList;
        string str = null;      

        for (int i = 0; i < dts.Length; i++)
        {            
            string space = ToLuaExport.GetNameSpace(dts[i].type, out str);
            AddSpaceNameToTree(tree, root, space);            
        }
        //Debug.Log("treeeeeeeeeeeeeeee  " + tree);
        return tree;
    }
    /// <summary>
    ///   将所有命名空间加进去且父子级也确定好，
    ///   但是都是基础的命名空间eg：UnityEngine
    /// </summary>
    /// <param name="tree"></param>
    /// <param name="parent"></param>
    /// <param name="space">一个完整的命名空间有父也有子（也可能只有一个）</param>

    static void AddSpaceNameToTree(ToLuaTree<string> tree, ToLuaNode<string> parent, string space)
    {
        if (space == null || space == string.Empty)
        {
            return;
        }

        string[] ns = space.Split(new char[] { '.' });

        for (int j = 0; j < ns.Length; j++)
        {
            List<ToLuaNode<string>> nodes = tree.Find((_t) => { return _t == ns[j]; }, j);
            //只有一级命名空间
            if (nodes.Count == 0)
            {
                ToLuaNode<string> node = new ToLuaNode<string>();
                node.value = ns[j];
                
                parent.childs.Add(node);
                Debug.Log(ns[j] + "  and father " + parent.value);
                node.parent = parent;
                node.layer = j;
                parent = node;
            }
            else
            {
                //多级命名空间
                bool flag = false;
                int index = 0;

                for (int i = 0; i < nodes.Count; i++)
                {
                    int count = j;
                    int size = j;
                    ToLuaNode<string> nodecopy = nodes[i];
                
                    while (nodecopy.parent != null)
                    {
                        nodecopy = nodecopy.parent;
                        if (nodecopy.value != null && nodecopy.value == ns[--count])
                        {
                            size--;
                        }
                    }

                    if (size == 0)
                    {
                        index = i;
                        flag = true;
                        break;
                    }
                }
                //吧子级命名空间弄好了，就把最低级目录进行存储
                if (!flag)
                {
                    ToLuaNode<string> nnode = new ToLuaNode<string>();
                    nnode.value = ns[j];
                    nnode.layer = j;
                    nnode.parent = parent;
                    parent.childs.Add(nnode);
                    parent = nnode;
                }
                else
                {
                    parent = nodes[index];
                }
            }
        }
    }
    /// <summary>
    /// 输出命名空间呗
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    static string GetSpaceNameFromTree(ToLuaNode<string> node)
    {
        string name = node.value;

        while (node.parent != null && node.parent.value != null)
        {
            node = node.parent;
            name = node.value + "." + name;
        }
        //Debug.LogError(name);
        return name;
    }
    /// <summary>
    /// 先将所有的<替换成_
    /// asd>asd><<<<<<<<<<<a<sd<111>d>>>>s>a
    /// 变成
    /// asdasd___________a_sd_111dsa
    /// >asd>asd><<<<<<<<<<<a<sd<111>d>>>>s>a
    /// >asd>asd>___________a_sd_111>d>>>>s>a
    /// 因为第一个就是>所以退出了替换循环
    /// 基本看过去就是吧
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    static string RemoveTemplateSign(string str)
    {
        Debug.LogError(str);
        str = str.Replace('<', '_');

        int index = str.IndexOf('>');

        while (index > 0)
        {
            str = str.Remove(index, 1);
            index = str.IndexOf('>');
        }
        Debug.LogError(str);
        return str;
    }
     /// <summary>
     /// 通过这个函数创造了一个LuaBinder脚本，
     /// 其目的就是为了实际创建所有的脚本的wrap版本
     /// </summary>
    [MenuItem("Lua/Gen LuaBinder File", false, 4)]
    static void GenLuaBinder()
    {
        if (!beAutoGen && EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }
        allTypes.Clear();
        //在初始话结构树的时候就会吧alltypes赋值，
        //比如组件类,脚本，Gameobject,Time,Texture等
        ToLuaTree<string> tree = InitTree();
        StringBuilder sb = new StringBuilder();
        List<DelegateType> dtList = new List<DelegateType>();
        List<DelegateType> list = new List<DelegateType>();
        list.AddRange(CustomSettings.customDelegateList);
        HashSet<Type> set = GetCustomTypeDelegates();
        List<BindType> backupList = new List<BindType>();
        backupList.AddRange(allTypes);
        ToLuaNode<string> root = tree.GetRoot();
        string libname = null;

        foreach (Type t in set)
        {
            if (null == list.Find((p) => { return p.type == t; }))
            {
                DelegateType dt = new DelegateType(t);                                
                AddSpaceNameToTree(tree, root, ToLuaExport.GetNameSpace(t, out libname));
                list.Add(dt);
            }
        }

        sb.AppendLineEx("//this source code was auto-generated by tolua#, do not modify it");
        sb.AppendLineEx("using System;");
        sb.AppendLineEx("using UnityEngine;");
        sb.AppendLineEx("using LuaInterface;");
        sb.AppendLineEx();
        sb.AppendLineEx("public static class LuaBinder");
        sb.AppendLineEx("{");
        sb.AppendLineEx("\tpublic static void Bind(LuaState L)");
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\tfloat t = Time.realtimeSinceStartup;");
        sb.AppendLineEx("\t\tL.BeginModule(null);");

        GenRegisterInfo(null, sb, list, dtList);
        //L.regFunction({})
        Action<ToLuaNode<string>> begin = (node) =>
        {
            if (node.value == null)
            {
                return;
            }

            sb.AppendFormat("\t\tL.BeginModule(\"{0}\");\r\n", node.value);
            string space = GetSpaceNameFromTree(node);
            //有不同命名空间的
            GenRegisterInfo(space, sb, list, dtList);
        };

        Action<ToLuaNode<string>> end = (node) =>
        {
            if (node.value != null)
            {
                sb.AppendLineEx("\t\tL.EndModule();");
            }
        };

        tree.DepthFirstTraversal(begin, end, tree.GetRoot());        
        sb.AppendLineEx("\t\tL.EndModule();");
        //l.AddPreLoad({1})
        if (CustomSettings.dynamicList.Count > 0)
        {
            sb.AppendLineEx("\t\tL.BeginPreLoad();");            

            for (int i = 0; i < CustomSettings.dynamicList.Count; i++)
            {
                Type t1 = CustomSettings.dynamicList[i];
                BindType bt = backupList.Find((p) => { return p.type == t1; });
                if (bt != null) sb.AppendFormat("\t\tL.AddPreLoad(\"{0}\", LuaOpen_{1}, typeof({0}));\r\n", bt.name, bt.wrapName);
            }

            sb.AppendLineEx("\t\tL.EndPreLoad();");
        }

        sb.AppendLineEx("\t\tDebugger.Log(\"Register lua type cost time: {0}\", Time.realtimeSinceStartup - t);");
        sb.AppendLineEx("\t}");
        //这里开始添加函数
        for (int i = 0; i < dtList.Count; i++)
        {
            ToLuaExport.GenEventFunction(dtList[i].type, sb);
        }

        if (CustomSettings.dynamicList.Count > 0)
        {
            
            for (int i = 0; i < CustomSettings.dynamicList.Count; i++)
            {
                Type t = CustomSettings.dynamicList[i];
                BindType bt = backupList.Find((p) => { return p.type == t; });
                if (bt != null) GenPreLoadFunction(bt, sb);
            }            
        }

        sb.AppendLineEx("}\r\n");
        allTypes.Clear();
        string file = CustomSettings.saveDir + "LuaBinder.cs";

        using (StreamWriter textWriter = new StreamWriter(file, false, Encoding.UTF8))
        {
            textWriter.Write(sb.ToString());
            textWriter.Flush();
            textWriter.Close();
        }

        AssetDatabase.Refresh();
        //Debug.LogError(sb);
        Debugger.Log("Generate LuaBinder over !");
    }
    /// <summary>
    /// 三联通其二
    /// </summary>
    /// <param name="nameSpace"></param>
    /// <param name="sb"></param>
    /// <param name="delegateList"></param>
    /// <param name="wrappedDelegatesCache"></param>
    static void GenRegisterInfo(string nameSpace, StringBuilder sb, List<DelegateType> delegateList, List<DelegateType> wrappedDelegatesCache)
    {
        for (int i = 0; i < allTypes.Count; i++)
        {
            Type dt = CustomSettings.dynamicList.Find((p) => { return allTypes[i].type == p; });

            if (dt == null && allTypes[i].nameSpace == nameSpace)
            {
                string str = "\t\t" + allTypes[i].wrapName + "Wrap.Register(L);\r\n";
                sb.Append(str);
                allTypes.RemoveAt(i--);
            }
        }

        string funcName = null;

        for (int i = 0; i < delegateList.Count; i++)
        {
            DelegateType dt = delegateList[i];
            Type type = dt.type;
            string typeSpace = ToLuaExport.GetNameSpace(type, out funcName);

            if (typeSpace == nameSpace)
            {
                funcName = ToLuaExport.ConvertToLibSign(funcName);
                string abr = dt.abr;
                abr = abr == null ? funcName : abr;
                sb.AppendFormat("\t\tL.RegFunction(\"{0}\", {1});\r\n", abr, dt.name);
                wrappedDelegatesCache.Add(dt);
            }
        }
    }

    /// <summary>
    /// 三联通其三
    /// </summary>
    /// <param name="bt"></param>
    /// <param name="sb"></param>
    static void GenPreLoadFunction(BindType bt, StringBuilder sb)
    {
        string funcName = "LuaOpen_" + bt.wrapName;

        sb.AppendLineEx("\r\n\t[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}(IntPtr L)\r\n", funcName);
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\ttry");
        sb.AppendLineEx("\t\t{");        
        sb.AppendLineEx("\t\t\tLuaState state = LuaState.Get(L);");
        sb.AppendFormat("\t\t\tstate.BeginPreModule(\"{0}\");\r\n", bt.nameSpace);
        sb.AppendFormat("\t\t\t{0}Wrap.Register(state);\r\n", bt.wrapName);
        sb.AppendFormat("\t\t\tint reference = state.GetMetaReference(typeof({0}));\r\n", bt.name);
        sb.AppendLineEx("\t\t\tstate.EndPreModule(L, reference);");                
        sb.AppendLineEx("\t\t\treturn 1;");
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t\tcatch(Exception e)");
        sb.AppendLineEx("\t\t{");
        sb.AppendLineEx("\t\t\treturn LuaDLL.toluaL_exception(L, e);");
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t}");
    }

    static string GetOS()
    {
        return LuaConst.osDir;
    }
    /// <summary>
    /// 在streamingassets路径创建文件夹以及路径
    /// 目前经过验证并没有实际创建文件夹，问题暂时不清楚
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    static string CreateStreamDir(string dir)
    {
        dir = Application.streamingAssetsPath + "/" + dir;
        //Debug.LogError(dir);
        if (!File.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        return dir;
    }
    /// <summary>
    /// 
    /// 第一次参数表示文件夹名字，第二个则是资源路径
    /// 输出路径发现，不出意外应该是把所有的lua文件编译成的二进制流文件，打包成
    /// Assets/temp/Lua/3rd/pbc\test2.lua.bytes    ->  lua_3rd.unity3d
    /// Assets/temp/Lua/System/Injection\InjectionBridgeInfo.lua.bytes
    /// Assets/temp/Lua/View/Shop\ShopPanel.lua.bytes
    /// 后两个依此类推
    /// </summary>
    /// <param name="subDir"></param>
    /// <param name="sourceDir"></param>
    static void BuildLuaBundle(string subDir, string sourceDir)
    {
        ///cjson            Assets/temp/Lua   
        ///可以配合下面的foreach循环看
        //Debug.LogError(subDir + "            " + sourceDir);
        string[] files = Directory.GetFiles(sourceDir + subDir, "*.bytes");
        foreach(var item in files)
        {
            //Assets/temp/Lua/cjson\util.lua.bytes
            //Debug.LogError(item);
            //Debug.LogError(subDir);
        } 
        
        string bundleName = subDir == null ? "lua.unity3d" : "lua" + subDir.Replace('/', '_') + ".unity3d";
        bundleName = bundleName.ToLower();
        Debug.Log(bundleName);
#if UNITY_4_6 || UNITY_4_7
        List<Object> list = new List<Object>();

        for (int i = 0; i < files.Length; i++)
        {
            Object obj = AssetDatabase.LoadMainAssetAtPath(files[i]);
            list.Add(obj);
        }

        BuildAssetBundleOptions options = BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.DeterministicAssetBundle;

        if (files.Length > 0)
        {
            string output = string.Format("{0}/{1}/" + bundleName, Application.streamingAssetsPath, GetOS());
            File.Delete(output);
            BuildPipeline.BuildAssetBundle(null, list.ToArray(), output, options, EditorUserBuildSettings.activeBuildTarget);            
        }
#else
        //个人认为的实际导入函数
        for (int i = 0; i < files.Length; i++)
        {
            AssetImporter importer = AssetImporter.GetAtPath(files[i]);

            if (importer)
            {
                importer.assetBundleName = bundleName;
                importer.assetBundleVariant = null;
            }
        }
#endif
    }
    //顾名思义清楚所有的lua文件（lua*.unity3d）
    static void ClearAllLuaFiles()
    {
        string osPath = Application.streamingAssetsPath + "/" + GetOS();
        //删除文件，这里应该是删除了索引
        if (Directory.Exists(osPath))
        {
            string[] files = Directory.GetFiles(osPath, "Lua*.unity3d");

            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
        }
        //删除路径
        string path = osPath + "/Lua";
       
        if (Directory.Exists(path))
        {
            Debug.Log(path);
            Directory.Delete(path, true);
        }

        path = Application.streamingAssetsPath + "/Lua";
       
        if (Directory.Exists(path))
        {
            Debug.Log(path);
            Directory.Delete(path, true);
        }

        path = Application.dataPath + "/temp";
        
        if (Directory.Exists(path))
        {
            Debug.Log(path);
            Directory.Delete(path, true);
        }

        path = Application.dataPath + "/Resources/Lua";
        
        if (Directory.Exists(path))
        {
            Debug.Log(path);
            Directory.Delete(path, true);
        }

        path = Application.persistentDataPath + "/" + GetOS() + "/Lua";
        
        if (Directory.Exists(path))
        {
            Debug.Log(path);
            Directory.Delete(path, true);
        }
    }

    [MenuItem("Lua/Gen LuaWrap + Binder", false, 4)]
    static void GenLuaWrapBinder()
    {
        if (EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }

        beAutoGen = true;        
        AssetDatabase.Refresh();
        GenerateClassWraps();
        GenLuaBinder();
        beAutoGen = false;   
    }

    [MenuItem("Lua/Generate All", false, 5)]
    static void GenLuaAll()
    {
        if (EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }

        beAutoGen = true;
        GenLuaDelegates();
        AssetDatabase.Refresh();
        GenerateClassWraps();
        GenLuaBinder();
        beAutoGen = false;
    }
    /// <summary>
    /// 将generate文件夹目录下的文件清除
    /// 
    /// </summary>
    [MenuItem("Lua/Clear wrap files", false, 6)]
    static void ClearLuaWraps()
    {
        string[] files = Directory.GetFiles(CustomSettings.saveDir, "*.cs", SearchOption.TopDirectoryOnly);

        for (int i = 0; i < files.Length; i++)
        {
            File.Delete(files[i]);
        }

        ToLuaExport.Clear();
        List<DelegateType> list = new List<DelegateType>();
        ToLuaExport.GenDelegates(list.ToArray());
        ToLuaExport.Clear();

        StringBuilder sb = new StringBuilder();
        sb.AppendLineEx("using System;");
        sb.AppendLineEx("using LuaInterface;");
        sb.AppendLineEx();
        sb.AppendLineEx("public static class LuaBinder");
        sb.AppendLineEx("{");
        sb.AppendLineEx("\tpublic static void Bind(LuaState L)");
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\tthrow new LuaException(\"Please generate LuaBinder files first!\");");
        sb.AppendLineEx("\t}");
        sb.AppendLineEx("}");

        string file = CustomSettings.saveDir + "LuaBinder.cs";
        //Debug.LogError(file);
        //Debug.LogError(sb);
        using (StreamWriter textWriter = new StreamWriter(file, false, Encoding.UTF8))
        {
            textWriter.Write(sb.ToString());
            textWriter.Flush();
            textWriter.Close();
        }

        AssetDatabase.Refresh();
    }
    //输出files的路径喔
    /// <summary>
    ///  将shourceDir目录 拷贝到destDir目录
    /// </summary>
    /// <param name="sourceDir">资源目录</param>
    /// <param name="destDir">目标目录</param>
    /// <param name="appendext"></param>
    /// <param name="searchPattern"></param>
    /// <param name="option"></param>
    public static void CopyLuaBytesFiles(string sourceDir, string destDir, bool appendext = true, string searchPattern = "*.lua", SearchOption option = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(sourceDir))
        {
            return;
        }
        //eg：
        //E:/beifen2/LuaFramework_UGUI_V2/Assets/LuaFramework/Lua
        //E:/beifen2/LuaFramework_UGUI_V2/Assets/Resources/Lua
        //Debug.LogError(sourceDir);
        //Debug.LogError(destDir);
        string[] files = Directory.GetFiles(sourceDir, searchPattern, option);
        int len = sourceDir.Length;
        //E:/beifen2/LuaFramework_UGUI_V2/Assets/LuaFramework/Lua
        //Debug.LogError(sourceDir);
        if (sourceDir[len - 1] == '/' || sourceDir[len - 1] == '\\')
        {
            --len;
        }
        // E:/beifen2/LuaFramework_UGUI_V2/Assets/LuaFramework/Lua
        // Debug.LogError(sourceDir);
        for (int i = 0; i < files.Length; i++)
        {
            string str = files[i].Remove(0, len);
            string dest = destDir + "/" + str;
            if (appendext) dest += ".bytes";
            string dir = Path.GetDirectoryName(dest);
            Directory.CreateDirectory(dir);
            File.Copy(files[i], dest, true);
            //  E:/beifen2/LuaFramework_UGUI_V2/Assets/Resources/Lua/\eventlib.lua.bytes
            // \eventlib.lua
            //Debug.LogError(dest);
            //Debug.LogError(str);
        }
        
    }

    /// <summary>
    /// 将lua和tolua文件夹copy到Resource文件夹喔
    /// </summary>
    [MenuItem("Lua/Copy Lua  files to Resources", false, 51)]
    public static void CopyLuaFilesToRes()
    {
        ClearAllLuaFiles();
        //Thread.Sleep(5000);
        //Debug.Log("Sleep over");
        string destDir = Application.dataPath + "/Resources" + "/Lua";
        CopyLuaBytesFiles(LuaConst.luaDir, destDir);
        CopyLuaBytesFiles(LuaConst.toluaDir, destDir);
        AssetDatabase.Refresh();
        //Debug.Log("Copy lua files over");
    }

    [MenuItem("Lua/Copy Lua  files to Persistent", false, 52)]
    public static void CopyLuaFilesToPersistent()
    {
        ClearAllLuaFiles();
        string destDir = Application.persistentDataPath + "/" + GetOS() + "/Lua";
        CopyLuaBytesFiles(LuaConst.luaDir, destDir, false);
        CopyLuaBytesFiles(LuaConst.toluaDir, destDir, false);
        //E:/beifen2/LuaFramework_UGUI_V2/Assets/LuaFramework/Lua
        //C:/Users/caohua/AppData/LocalLow/lala/toluaTest/Android/Lua
        //Debug.LogError(LuaConst.luaDir);
        //Debug.LogError(destDir);

        AssetDatabase.Refresh();
        //Debug.Log("Copy lua files over");
    }
    /// <summary>
    /// 输出dir目录下所有的文件夹目录
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="list"></param>
    static void GetAllDirs(string dir, List<string> list)
    {
        string[] dirs = Directory.GetDirectories(dir);
        //foreach(var i in dirs)
        //{
        //    Debug.LogError(i);
        //}
        list.AddRange(dirs);

        for (int i = 0; i < dirs.Length; i++)
        {
            GetAllDirs(dirs[i], list);
        }
    }
    /// <summary>
    /// 将source目录的.lua文件复制到dest目录下
    /// </summary>
    /// <param name="source"></param>
    /// <param name="dest"></param>
    /// <param name="searchPattern"></param>
    /// <param name="option"></param>
    static void CopyDirectory(string source, string dest, string searchPattern = "*.lua", SearchOption option = SearchOption.AllDirectories)
    {                
        string[] files = Directory.GetFiles(source, searchPattern, option);

        for (int i = 0; i < files.Length; i++)
        {
            string str = files[i].Remove(0, source.Length);
            string path = dest + "/" + str;
            string dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            File.Copy(files[i], path, true);
        }        
    }
    /// <summary>
    /// luajit里面有一个build.bat批文件
    /// </summary>
    /// <param name="path"></param>
    /// <param name="tempDir"></param>
    static void CopyBuildBat(string path, string tempDir)
    {
		if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
		{
			File.Copy(path + "/Luajit64/Build.bat", tempDir + "/Build.bat", true);			
		}
		else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows)
        {
            if (IntPtr.Size == 4)
            {
                File.Copy(path + "/Luajit/Build.bat", tempDir + "/Build.bat", true);
            }
            else if (IntPtr.Size == 8)
            {
                File.Copy(path + "/Luajit64/Build.bat", tempDir + "/Build.bat", true);
            }
        }
#if UNITY_5_3_OR_NEWER        
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
#else
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
#endif        
        {
            //Debug.Log("iOS默认用64位，32位自行考虑");
            File.Copy(path + "/Luajit64/Build.bat", tempDir + "/Build.bat", true);
        }
        else
        {
            //这里的路径我进行了修改
            File.Copy(path + "/Assets/LuaFramework/Luajit/Build.bat", tempDir + "/Build.bat", true);
        }

    }
    /// <summary>
    /// 吧build.batcopy到streamingAssets的lua目录下面然后再执行
    /// </summary>
    [MenuItem("Lua/Build Lua files to Resources (PC)", false, 53)]
    public static void BuildLuaToResources()
    {
        ClearAllLuaFiles();
        string tempDir = CreateStreamDir("Lua");
        
        //Debug.LogError(tempDir);
        string destDir = Application.dataPath + "/Resources" + "/Lua";
        //Debug.LogError(destDir);
        string path = Application.dataPath.Replace('\\', '/');
        path = path.Substring(0, path.LastIndexOf('/'));
        CopyBuildBat(path, tempDir);
        //Debug.LogError(tempDir);
        CopyLuaBytesFiles(LuaConst.luaDir, tempDir, false);
        //Debug.LogError(tempDir);
        Process proc = Process.Start(tempDir + "/Build.bat");
        proc.WaitForExit();
        CopyLuaBytesFiles(tempDir + "/Out/", destDir, false, "*.lua.bytes");
        CopyLuaBytesFiles(LuaConst.toluaDir, destDir);
        //将资源目录StreamingAssets/lua的文件编译到Resources/lua      
        //Debug.LogError(tempDir);
        //Debug.LogError(destDir);
        Directory.Delete(tempDir, true);        
        AssetDatabase.Refresh();
    }
    /// <summary>
    /// 这里就相当于会存储进去（固定）
    /// </summary>
    [MenuItem("Lua/Build Lua files to Persistent (PC)", false, 54)]
    public static void BuildLuaToPersistent()
    {
        ClearAllLuaFiles();
        string tempDir = CreateStreamDir("Lua");        
        string destDir = Application.persistentDataPath + "/" + GetOS() + "/Lua/";
        //Debug.LogError(GetOS());
        string path = Application.dataPath.Replace('\\', '/');
        path = path.Substring(0, path.LastIndexOf('/'));        
        CopyBuildBat(path, tempDir);
        CopyLuaBytesFiles(LuaConst.luaDir, tempDir, false);
        Process proc = Process.Start(tempDir + "/Build.bat");
        proc.WaitForExit();        
        CopyLuaBytesFiles(LuaConst.toluaDir, destDir, false);

        path = tempDir + "/Out/";
        string[] files = Directory.GetFiles(path, "*.lua.bytes");
        int len = path.Length;

        for (int i = 0; i < files.Length; i++)
        {
            path = files[i].Remove(0, len);
            path = path.Substring(0, path.Length - 6);
            path = destDir + path;

            File.Copy(files[i], path, true);
        }

        Directory.Delete(tempDir, true);
        AssetDatabase.Refresh();
    }
    /// <summary>
    /// 核心是调用buildluaBundle,将代码进行编译，而不进行jit
    /// </summary>
    [MenuItem("Lua/Build bundle files not jit", false, 55)]
    public static void BuildNotJitBundles()
    {
        ClearAllLuaFiles();
        CreateStreamDir(GetOS());
        //他产生AOT代码需要先删除原本的文件，然后产生SreamingAssert文件夹
#if UNITY_4_6 || UNITY_4_7
        string tempDir = CreateStreamDir("Lua");
#else
        //E:/beifen2/LuaFramework_UGUI_V2/Assets/temp/Lua
        //Debug.LogError(tempDir);
        string tempDir = Application.dataPath + "/temp/Lua";
        //没有该目录就创建他
        if (!File.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }        
#endif
        //将lua，tolua文件都copy到tempDir目录
        CopyLuaBytesFiles(LuaConst.luaDir, tempDir);
        CopyLuaBytesFiles(LuaConst.toluaDir, tempDir);

        AssetDatabase.Refresh();
        List<string> dirs = new List<string>();
        //获得该目录下的所有文件夹索引
        GetAllDirs(tempDir, dirs);

#if UNITY_5 || UNITY_5_3_OR_NEWER
        //将\\ 替换成/ 
		for (int i = 0; i < dirs.Count; i++)
        {
            string str = dirs[i].Remove(0, tempDir.Length);
            BuildLuaBundle(str.Replace('\\', '/'), "Assets/temp/Lua");
        }
        //将lua文件编译到"Assets/temp/Lua"
        //看了一下第一个参数表示搜索文件夹名字，第二个参数表示Assert...路径
        BuildLuaBundle(null, "Assets/temp/Lua");
        //重新导入修改后的资源
        AssetDatabase.SaveAssets();     
        
        string output = string.Format("{0}/{1}", Application.streamingAssetsPath, GetOS());
        //E:/beifen2/LuaFramework_UGUI_V2/Assets/StreamingAssets/Android
        //Debug.LogError(output);
        //通过网络加载资源（这个来自类说明）
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.DeterministicAssetBundle, EditorUserBuildSettings.activeBuildTarget);

        //Directory.Delete(Application.dataPath + "/temp/", true);
#else
        for (int i = 0; i < dirs.Count; i++)
        {
            string str = dirs[i].Remove(0, tempDir.Length);
            BuildLuaBundle(str.Replace('\\', '/'), "Assets/StreamingAssets/Lua");
        }

        BuildLuaBundle(null, "Assets/StreamingAssets/Lua");
        Directory.Delete(Application.streamingAssetsPath + "/Lua/", true);
#endif
        AssetDatabase.Refresh();
    }

    [MenuItem("Lua/Build Luajit bundle files   (PC)", false, 56)]
    public static void BuildLuaBundles()
    {
        ClearAllLuaFiles();                
        CreateStreamDir(GetOS());

#if UNITY_4_6 || UNITY_4_7
        string tempDir = CreateStreamDir("Lua");
#else
        string tempDir = Application.dataPath + "/temp/Lua";

        if (!File.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }
#endif

        string path = Application.dataPath.Replace('\\', '/');
        path = path.Substring(0, path.LastIndexOf('/'));        
        CopyBuildBat(path, tempDir);
        CopyLuaBytesFiles(LuaConst.luaDir, tempDir, false);
        Process proc = Process.Start(tempDir + "/Build.bat");
        proc.WaitForExit();
        CopyLuaBytesFiles(LuaConst.toluaDir, tempDir + "/Out");

        AssetDatabase.Refresh();

        string sourceDir = tempDir + "/Out";
        List<string> dirs = new List<string>();        
        GetAllDirs(sourceDir, dirs);

#if UNITY_5 || UNITY_5_3_OR_NEWER
		for (int i = 0; i < dirs.Count; i++)
        {
            string str = dirs[i].Remove(0, sourceDir.Length);
            BuildLuaBundle(str.Replace('\\', '/'), "Assets/temp/Lua/Out");
        }

        BuildLuaBundle(null, "Assets/temp/Lua/Out");

        AssetDatabase.Refresh();
        string output = string.Format("{0}/{1}", Application.streamingAssetsPath, GetOS());
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.DeterministicAssetBundle, EditorUserBuildSettings.activeBuildTarget);
        Directory.Delete(Application.dataPath + "/temp/", true);
#else
        for (int i = 0; i < dirs.Count; i++)
        {
            string str = dirs[i].Remove(0, sourceDir.Length);
            BuildLuaBundle(str.Replace('\\', '/'), "Assets/StreamingAssets/Lua/Out");
        }

        BuildLuaBundle(null, "Assets/StreamingAssets/Lua/Out/");
        Directory.Delete(tempDir, true);
#endif
        AssetDatabase.Refresh();
    }

    [MenuItem("Lua/Clear all Lua files", false, 57)]
    public static void ClearLuaFiles()
    {
        ClearAllLuaFiles();
    }


    [MenuItem("Lua/Gen BaseType Wrap", false, 101)]
    static void GenBaseTypeLuaWrap()
    {
        if (!beAutoGen && EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }
        //baseType存放的位置
        string dir = CustomSettings.toluaBaseType;
        //不存在就创建
        if (!File.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        allTypes.Clear();
        ToLuaExport.allTypes.AddRange(baseType);
        List<BindType> btList = new List<BindType>();
        
        for (int i = 0; i < baseType.Count; i++)
        {
            btList.Add(new BindType(baseType[i]));
        }
        //测试重复使用的结果
        //btList.Add(btList[0]);
        //去重复喔
        GenBindTypes(btList.ToArray(), false);
        //将他变成bindType类型数组，在ToluaExport里面进行处理
        BindType[] list = allTypes.ToArray();

        for (int i = 0; i < list.Length; i++)
        {
            ToLuaExport.Clear();
            ToLuaExport.className = list[i].name;
            ToLuaExport.type = list[i].type;
            ToLuaExport.isStaticClass = list[i].IsStatic;
            ToLuaExport.baseType = list[i].baseType;
            ToLuaExport.wrapClassName = list[i].wrapName;
            ToLuaExport.libClassName = list[i].libName;
            ToLuaExport.Generate(dir);
        }
        
        Debug.Log("Generate base type files over");
        allTypes.Clear();
        AssetDatabase.Refresh();
    }
    /// <summary>
    /// 这个create也是离谱，他是提示你去点那个   Gen BaseType Wrap first！
    /// </summary>
    /// <param name="path"></param>
    /// <param name="name"></param>
    static void CreateDefaultWrapFile(string path, string name)
    {
        StringBuilder sb = new StringBuilder();
        path = path + name + ".cs";
        sb.AppendLineEx("using System;");
        sb.AppendLineEx("using LuaInterface;");
        sb.AppendLineEx();
        sb.AppendLineEx("public static class " + name);
        sb.AppendLineEx("{");
        sb.AppendLineEx("\tpublic static void Register(LuaState L)");
        sb.AppendLineEx("\t{");        
        sb.AppendLineEx("\t\tthrow new LuaException(\"Please click menu Lua/Gen BaseType Wrap first!\");");
        sb.AppendLineEx("\t}");
        sb.AppendLineEx("}");

        using (StreamWriter textWriter = new StreamWriter(path, false, Encoding.UTF8))
        {
            textWriter.Write(sb.ToString());
            textWriter.Flush();
            textWriter.Close();
        }
    }
    
    [MenuItem("Lua/Clear BaseType Wrap", false, 102)]
    static void ClearBaseTypeLuaWrap()
    {
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "System_ObjectWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "System_DelegateWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "System_StringWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "System_EnumWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "System_TypeWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "System_Collections_IEnumeratorWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "UnityEngine_ObjectWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "LuaInterface_EventObjectWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "LuaInterface_LuaMethodWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "LuaInterface_LuaPropertyWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "LuaInterface_LuaFieldWrap");
        CreateDefaultWrapFile(CustomSettings.toluaBaseType, "LuaInterface_LuaConstructorWrap");        

        Debug.Log("Clear base type wrap files over");
        AssetDatabase.Refresh();
    }

    [MenuItem("Lua/Enable Lua Injection &e", false, 102)]
    static void EnableLuaInjection()
    {
        bool EnableSymbols = false;
        if (UpdateMonoCecil(ref EnableSymbols) != -1)
        {
            BuildTargetGroup curBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string existSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(curBuildTargetGroup);
            if (!existSymbols.Contains("ENABLE_LUA_INJECTION"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(curBuildTargetGroup, existSymbols + ";ENABLE_LUA_INJECTION");
            }

            AssetDatabase.Refresh();
        }
    }

#if ENABLE_LUA_INJECTION
    [MenuItem("Lua/Injection Remove &r", false, 5)]
#endif
    static void RemoveInjection()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("警告", "游戏运行过程中无法操作", "确定");
            return;
        }

        BuildTargetGroup curBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string existSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(curBuildTargetGroup);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(curBuildTargetGroup, existSymbols.Replace("ENABLE_LUA_INJECTION", ""));
        Debug.Log("Lua Injection Removed!");
    }

    public static int UpdateMonoCecil(ref bool EnableSymbols)
    {
        string appFileName = Environment.GetCommandLineArgs()[0];
        //Debug.LogError("1"+appFileName);
        string appPath = Path.GetDirectoryName(appFileName);
        //Debug.LogError("1" + appFileName);
        string directory = appPath + "/Data/Managed/";
        if (UnityEngine.Application.platform == UnityEngine.RuntimePlatform.OSXEditor)
        {
            directory = appPath.Substring(0, appPath.IndexOf("MacOS")) + "Managed/";
        }
        string suitedMonoCecilPath = directory +
#if UNITY_2017_1_OR_NEWER
            "Unity.Cecil.dll";
#else
            "Mono.Cecil.dll";
#endif
        string suitedMonoCecilMdbPath = directory +
#if UNITY_2017_1_OR_NEWER
            "Unity.Cecil.Mdb.dll";
#else
            "Mono.Cecil.Mdb.dll";
#endif
        string suitedMonoCecilPdbPath = directory +
#if UNITY_2017_1_OR_NEWER
            "Unity.Cecil.Pdb.dll";
#else
            "Mono.Cecil.Pdb.dll";
#endif
        string suitedMonoCecilToolPath = directory + "Unity.CecilTools.dll";

        if (!File.Exists(suitedMonoCecilPath)
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
            && !File.Exists(suitedMonoCecilMdbPath)
            && !File.Exists(suitedMonoCecilPdbPath)
#endif
        )
        {
            EnableSymbols = false;
            Debug.Log("Haven't found Mono.Cecil.dll!Symbols Will Be Disabled");
            return -1;
        }

        bool bInjectionToolUpdated = false;
        string injectionToolPath = CustomSettings.injectionFilesPath + "Editor/";
        string existMonoCecilPath = injectionToolPath + Path.GetFileName(suitedMonoCecilPath);
        string existMonoCecilPdbPath = injectionToolPath + Path.GetFileName(suitedMonoCecilPdbPath);
        string existMonoCecilMdbPath = injectionToolPath + Path.GetFileName(suitedMonoCecilMdbPath);
        string existMonoCecilToolPath = injectionToolPath + Path.GetFileName(suitedMonoCecilToolPath);

        try
        {
            bInjectionToolUpdated = TryUpdate(suitedMonoCecilPath, existMonoCecilPath) ? true : bInjectionToolUpdated;
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_3_OR_NEWER
            bInjectionToolUpdated = TryUpdate(suitedMonoCecilPdbPath, existMonoCecilPdbPath) ? true : bInjectionToolUpdated;
            bInjectionToolUpdated = TryUpdate(suitedMonoCecilMdbPath, existMonoCecilMdbPath) ? true : bInjectionToolUpdated;
#endif
            TryUpdate(suitedMonoCecilToolPath, existMonoCecilToolPath);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            return -1;
        }
        EnableSymbols = true;

        return bInjectionToolUpdated ? 1 : 0;
    }

    static bool TryUpdate(string srcPath, string destPath)
    {
        if (GetFileContentMD5(srcPath) != GetFileContentMD5(destPath))
        {
            File.Copy(srcPath, destPath, true);
            return true;
        }

        return false;
    }

    static string GetFileContentMD5(string file)
    {
        if (!File.Exists(file))
        {
            return string.Empty;
        }

        FileStream fs = new FileStream(file, FileMode.Open);
        System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] retVal = md5.ComputeHash(fs);
        fs.Close();

        StringBuilder sb = StringBuilderCache.Acquire();
        for (int i = 0; i < retVal.Length; i++)
        {
            sb.Append(retVal[i].ToString("x2"));
        }
        return StringBuilderCache.GetStringAndRelease(sb);
    }
}
