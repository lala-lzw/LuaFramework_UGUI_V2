---
--- Generated by EmmyLua(https://github.com/EmmyLua)
--- Created by caohua.
--- DateTime: 2020/12/10 19:43
---
---
--- Generated by EmmyLua(https://github.com/EmmyLua)
--- Created by caohua.
--- DateTime: 2020/12/10 13:50
---
local transform;
local gameObject;

HallPanel = {};
local this = HallPanel;

--启动事件--
function HallPanel.Awake(obj)
    gameObject = obj;
    transform = obj.transform;
    log("11111111111111111111111111111111111111"..transform.name );

    this.InitPanel();
    logWarn("Awake lua--->>"..gameObject.name);
end

--初始化面板--
function HallPanel.InitPanel()
    log("HallPanel_is_be_加载1111" );
    HallPanel.item =transform:Find("RankeItem").gameObject;
    log('???????????????');
    log('ssssssssssssssssss');
    gameObject:AddComponent(ParticleSystem);
    HallPanel.btn = transform:Find("ButtonHall").gameObject;
    HallPanel.btnShop = transform:Find("ButtonShop").gameObject;
    HallCtrl.OnCreate(gameObject);
    HallPanel.father = transform.parent;
    if HallPanel.btn  == nil then
        log("是空的");
    else
        log(HallPanel.btn.name.."不是空的");
    end

    --log("1111111122222222222211111111111111"..HallPanel.btn);
    --HallPanel.father = transform:Find("ButtonHall").gameObject;

    --HallPanel.btn = HallPanel.father.transform:Find("ButtonHall").gameObject;
    --if HallPanel.btn ==  nil then
        --log("123123123132123123123123123132123132");
    --else
        --log("333333333333333333333333333333333333"..HallPanel.father.name);
    --end
    --HallPanel.Btn = transform:Find("ButtonHall");
    --log(HallPanel.btn..“11111111111111111111112222”);
    --log("222222222222222222222222");
    --log(gameObject.name);
    --this.btnClose = transform:Find("LoginPanel").gameObject;
    --log("3333333333333333333333333");
    --LoginPanel.accountInput = transform:Find("AccountInput").gameObject;
    --LoginPanel.passwordInput = transform:Find("PasswordInput").gameObject;
    --LoginPanel.loginButton = transform:Find("LoginButton").gameObject;
    --asd = LoginPanel.loginButton:GetComponent("Button");
    --log("11111111111111111111111111111"..LoginPanel.loginButton);
    --LoginPanel.remberPassword = transform:Find("RemberPassword").gameObject;
    --LoginPanel.TestDotween1();
    --LoginPanel.TestDotween2();
end


--单击事件--
function HallPanel.OnDestroy()
    logWarn("OnDestroy---->>>");
end
