---
--- Generated by EmmyLua(https://github.com/EmmyLua)
--- Created by caohua.
--- DateTime: 2020/12/10 13:50
---
local transform;
local gameObject;

FirstPanel = {};
local this = FirstPanel;

--启动事件--
function FirstPanel.Awake(obj)
    gameObject = obj;
    transform = obj.transform;
    --log("11111111111111111111111111111111111111"..transform.name );

    this.InitPanel();
    logWarn("Awake lua--->>"..gameObject.name);
end

--初始化面板--
function FirstPanel.InitPanel()
    --log("222222222222222222222222");

    this.btnClose = transform:Find("ButtonPrefab").gameObject;
end

--单击事件--
function FirstPanel.OnDestroy()
    logWarn("OnDestroy---->>>");
end

