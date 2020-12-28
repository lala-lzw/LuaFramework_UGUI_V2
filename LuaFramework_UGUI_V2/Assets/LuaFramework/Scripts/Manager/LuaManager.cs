using UnityEngine;
using System.Collections;
using LuaInterface;

namespace LuaFramework {
    public class LuaManager : Manager {
        //开一个状态机,在后面我们可以看到
        //有一个将该状态机当中最主要的状态机的语句
        private LuaState lua;
        //资源加载类
        private LuaLoader loader;
        private LuaLooper loop = null;

        // Use this for initialization
        void Awake() {
            loader = new LuaLoader();
            lua = new LuaState();
            this.OpenLibs();
            //清空栈顶
            lua.LuaSetTop(0);
            //luabingder还需要进行细究
            LuaBinder.Bind(lua);
            //代理类初始化
            DelegateFactory.Init();

            LuaCoroutine.Register(lua, this);
        }

        public void InitStart() {
            //初始化路径
            InitLuaPath();
            //初始化绑定
            InitLuaBundle();
            this.lua.Start();    //启动LUAVM
            this.StartMain();
            this.StartLooper();
        }
        //lualooper的作用是什么呢，这个我们可以去探究一下
        //这里想放一下
        void StartLooper() {
            loop = gameObject.AddComponent<LuaLooper>();
            loop.luaState = lua;
        }

        //cjson 比较特殊，只new了一个table，没有注册库，这里注册一下
        protected void OpenCJson() {
            lua.LuaGetField(LuaIndexes.LUA_REGISTRYINDEX, "_LOADED");
            lua.OpenLibs(LuaDLL.luaopen_cjson);
            lua.LuaSetField(-2, "cjson");

            lua.OpenLibs(LuaDLL.luaopen_cjson_safe);
            lua.LuaSetField(-2, "cjson.safe");
        }
        /// <summary>
        /// startmain里面的东西目前不知道在干嘛
        /// </summary>
        void StartMain() {
            lua.DoFile("Main.lua");
            //mian.lua里面有一个print提醒logic start
            LuaFunction main = lua.GetFunction("Main");
            //标准的执行函数
            main.Call();
            main.Dispose();
            main = null;    
        }
        
        /// <summary>
        /// 初始化加载第三方库
        /// </summary>
        void OpenLibs() {
            lua.OpenLibs(LuaDLL.luaopen_pb);      
            lua.OpenLibs(LuaDLL.luaopen_sproto_core);
            lua.OpenLibs(LuaDLL.luaopen_protobuf_c);
            lua.OpenLibs(LuaDLL.luaopen_lpeg);
            lua.OpenLibs(LuaDLL.luaopen_bit);
            lua.OpenLibs(LuaDLL.luaopen_socket_core);

            this.OpenCJson();
        }

        /// <summary>
        /// 初始化Lua代码加载路径
        /// 通过app安装路径加上app名字
        /// 将总共四个路径加到资源寻找路径里面去
        /// 是那些路径我们可以便利一次，或者通过报错看出来】
        /// 我们采用便利的方式
        ///  ./?.lua
        ///  C:/Program Files/Unity/Editor/lua/?.lua
        ///   C:/Program Files/Unity/Editor/lua/?/init.lua
        ///   C:/Program Files (x86)/Lua/5.1/lua/?.luac
        ///    E:/beifen2/LuaFramework_UGUI_V2/Assets/LuaFramework/ToLua/Lua/?.lua
        ///    E:/beifen2/LuaFramework_UGUI_V2/Assets/LuaFramework/Lua/?.lua
        ///    
        /// </summary>
        void InitLuaPath() {
            if (AppConst.DebugMode) {
               
                string rootPath = AppConst.FrameworkRoot;
                //Debug.LogError(rootPath);
                lua.AddSearchPath(rootPath + "/Lua");
                lua.AddSearchPath(rootPath + "/Lua/Logic");

                lua.AddSearchPath(rootPath + "/ToLua/Lua");
            } else {
                string rootPath = AppConst.FrameworkRoot;
                //Debug.LogError(rootPath);
                lua.AddSearchPath(rootPath + "/Lua/Logic");
                lua.AddSearchPath(Util.DataPath + "lua");
            }
        }

        /// <summary>
        /// 初始化LuaBundle
        /// </summary>
        void InitLuaBundle() {
           //这里的加载属于将基本的他自带的案例使用到的组件
           //给加载好了
           //我们自带的资源可以在packger.cs里面的
            //HandleExampleBundle使用类似方式进行添加
            //并且记得将你大宝的unity3d添加注册到customSetting
            //里面不然要么打包了也找不到，要么没有进行打包
            //具体是哪一个我没有细究
            //具体customsetting路径是customTypeList
            //哪里还有注释说
            //在这里添加你要注册到lua的类型列表
            if (loader.beZip) {
                loader.AddBundle("lua/lua.unity3d");
                loader.AddBundle("lua/lua_math.unity3d");
                loader.AddBundle("lua/lua_system.unity3d");
                loader.AddBundle("lua/lua_system_reflection.unity3d");
                loader.AddBundle("lua/lua_unityengine.unity3d");
                loader.AddBundle("lua/lua_common.unity3d");
                loader.AddBundle("lua/lua_logic.unity3d");
                loader.AddBundle("lua/lua_view.unity3d");
                loader.AddBundle("lua/lua_controller.unity3d");
                loader.AddBundle("lua/lua_misc.unity3d");

                loader.AddBundle("lua/lua_protobuf.unity3d");
                loader.AddBundle("lua/lua_3rd_cjson.unity3d");
                loader.AddBundle("lua/lua_3rd_luabitop.unity3d");
                loader.AddBundle("lua/lua_3rd_pbc.unity3d");
                loader.AddBundle("lua/lua_3rd_pblua.unity3d");
                loader.AddBundle("lua/lua_3rd_sproto.unity3d");
            }
        }
        //dofile 就是执行该函数
        public void DoFile(string filename) {
            lua.DoFile(filename);
        }

        // Update is called once per frame
        /// <summary>
        /// 这个就相当于call，endcall，pcall
        /// 起始可以联想update，fixdupdate，
        /// 具体的联系，我们可以待会儿在建立一下
        /// </summary>
        /// <param name="funcName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [System.Obsolete]
        public object[] CallFunction(string funcName, params object[] args) {
            LuaFunction func = lua.GetFunction(funcName);
            if (func != null) {
                return func.LazyCall(args);
            }
            return null;
        }
        /// <summary>
        /// 暂时当作是垃圾回收算法喔，
        /// 这里提一嘴喔，lua1的回收机制采用mark-sweep算法，
        /// 原本是两色，引用就不回收，繁殖回收，
        /// 新对象的时候，会出现问题，你标记为白色则没有遍历其是否有引用就会瘦
        /// 黑色则没有被扫描就不回收，都是不对的
        /// 现在是三色的新对象白色，初始化后就是灰色，。。
        /// </summary>
        public void LuaGC() {
            lua.LuaGC(LuaGCOptions.LUA_GCCOLLECT);
        }
         /// <summary>
         /// 关闭就是关状态机等东西
         /// </summary>
        public void Close() {
            loop.Destroy();
            loop = null;

            lua.Dispose();
            lua = null;
            loader = null;
        }
    }
}