require "3rd/pblua/login_pb"
require "3rd/pbc/protobuf"

local lpeg = require "lpeg"

local json = require "cjson"
local util = require "3rd/cjson/util"

local sproto = require "3rd/sproto/sproto"
local core = require "sproto.core"
local print_r = require "3rd/sproto/print_r"

require "Logic/LuaClass"
require "Logic/CtrlManager"
require "Common/functions"
require "Controller/PromptCtrl"
log('game')
--管理器--
Game = {};
local this = Game;

local game; 
local transform;
local gameObject;
local WWW = UnityEngine.WWW;

function Game.InitViewPanels()
	for i = 1, #PanelNames do
		require ("View/"..tostring(PanelNames[i]))
	end
end

--初始化完成，发送链接服务器信息--
function Game.OnInitOK()
    AppConst.SocketPort = 7777;
    AppConst.SocketAddress = "127.0.0.1";
    networkMgr:SendConnect();
    --logError("12123");
    --注册LuaView--
    this.InitViewPanels();
    CtrlManager.Init();
    local ctrl = CtrlManager.GetCtrl(CtrlNames.Hall);
    if ctrl ~= nil and AppConst.ExampleMode == 1 then
        --ctrl:Awake();
    end
    local objHallPanel = UnityEngine.GameObject.Find("Canvas").transform:GetChild(0 ).gameObject;
    HallPanel.Awake(objHallPanel);
    logWarn('LuaFramework InitOK--->>>');
end

--销毁--
function Game.OnDestroy()
	--logWarn('OnDestroy--->>>');
end
