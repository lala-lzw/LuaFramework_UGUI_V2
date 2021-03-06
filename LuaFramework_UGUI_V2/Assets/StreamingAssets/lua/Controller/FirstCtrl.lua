---
--- Generated by EmmyLua(https://github.com/EmmyLua)
--- Created by caohua.
--- DateTime: 2020/12/10 13:54
---

FirstCtrl = {};
local this = FirstCtrl;

local message;
local transform;
local gameObject;

--构建函数--
function FirstCtrl.New()
    logWarn("FirstCtrl.New--->>");
    return this;
end

function FirstCtrl.Awake()
    logWarn("FirstCtrl.Awake--->>");
    panelMgr:CreatePanel('First', this.OnCreate);
end

--启动事件--
function FirstCtrl.OnCreate(obj)
    gameObject = obj;
    transform = gameObject.transform;
    log("创建       ");
    behaviour = gameObject:GetComponent('LuaBehaviour');
    behaviour:AddClick(FirstPanel.btnClose,function()
        log("你点击了关闭");
    end);
    logWarn("Start lua--->>"..gameObject.name);
    log("11111111111111111111111111111111111111");
    message = gameObject:GetComponent('LuaBehaviour');
    --message:AddClick(MessagePanel.btnClose, this.OnClick);
    resMgr:LoadPrefab("prefabs.unity3d",{"ImgOrc"},function( prefabs)
        log(prefabs.Length);
        log(prefabs[0].name);
        local go = newObject(prefabs[0]);
        go.transform:SetParent(transform);
        log(transform.name);
        go.transform.localPosition  = Vector3.zero;
        go.transform.localScale = Vector3.one;
    end);
    log("3333333333333333333333333333333333333333333333333333333333333333333333333333");


end

--单击事件--
function FirstCtrl.OnClick(go)
    destroy(gameObject);
end

--关闭事件--
function FirstCtrl.Close()
    panelMgr:ClosePanel(CtrlNames.message);
end