--主入口函数。从这里开始lua逻辑
--山色空蒙雨亦奇
require 'View/MyTemp/Mytemp'
require 'Network'
function Main()
	local LuaHelper = LuaFramework.LuaHelper;
	local networkMgr = LuaHelper.GetNetManager();
	local AppConst = LuaFramework.AppConst;

	AppConst.SocketPort = 7777;
	AppConst.SocketAddress = "127.0.0.1";
	networkMgr:SendConnect();
	Event.AddListener(Protocal.Connect,Network.OnConnect);
	Event.AddListener(Protocal.Message,Network.OnMessage);
	networkMgr:SendConnect();
	--[[
	print('main start');

	 --gameobj = UnityEngine.GameObject.Find('Canvas').gameObject;
	--if gameobj == nil then
	--	print("gameobj is null");
	--else
	--	print("gameobj not null"..gameobj.name);
	--end
	--LuaHelper = LuaFramework.LuaHelper;
	--resMgr = LuaHelper.GetResManager();
	--resMgr:LoadPrefab('Shop',{'ShopPanel'},OnLoadFinlish);
	--UpdateBeat:Add(MyUpdate,self);
	]]
end
local go;
function OnLoadFinlish(objs)
	 go = UnityEngine.GameObject.Instantiate(objs[0]);
	--gameobj = go.parent;
	--print(gameobj.name.."123123123123123123123123");
	go.transform.SetParent( go.transform,gameobj.transform);
	go.transform.localPosition = Vector3.zero;
    --go.transform.localScale = go.transform.localScale*0.01;
	go.transform.localScale = Vector3(1,1,1);
	--这里是用来给物体添加lua脚本的
	local myTemp1 = LuaComponent.Add(go,Mytemp);
	myTemp1.name = "MyTemp";
	--LuaFramework.Util.log("Finsh");
end
function MyUpdate()
	LuaFramework.Util.Log("执行");
	local lala = UnityEngine.KeyCode;
	local Input = UnityEngine.Input;
	local horizontal = Input.GetAxis("Horizontal");
	local vertical = Input.GetAxis("Vertical");
	asd = Vector3.New(horizontal ,vertical ,0);
	if go  == nil then
		print("go is nil")
	else
		go.transform.localPosition = go.transform.localPosition + asd;
	end
end
function assert()

end
--场景切换通知
function OnLevelWasLoaded(level)
	collectgarbage("collect")
	Time.timeSinceLevelLoad = 0
end

function OnApplicationQuit()
end