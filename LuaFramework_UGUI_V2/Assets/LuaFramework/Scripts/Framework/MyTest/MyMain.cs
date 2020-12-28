using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.LuaFramework.Scripts.Framework.MyTest
{
    class MyMain :View
    {
        /*
        private void Start()
        {
            AppFacade.Instance.RegisterCommand("TestMessage", typeof(TestCommand));
            AppFacade.Instance.SendMessageCommand("TestMessage","this_is_string");
        }
        */
         void Start()
        {
            List<string> regList = new List<string>();

            regList.Add("msg1");
            regList.Add("msg2");
            RegisterMessage(this, regList);
        }
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                facade.SendMessageCommand("msg1", null);
                //SendMessage("msg1", null);
            }
        }
        public override void OnMessage(IMessage message)
        {
            Debug.Log(" message  " + message.Name);
            base.OnMessage(message);
        }
    }
}
