using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.LuaFramework.Scripts.Framework.MyTest
{
    class TestCommand:ControllerCommand
    {
        public override void Execute(IMessage message)
        {
            Debug.Log("name = " + message.Name);
            Debug.Log("type = " + message.Type);
            base.Execute(message);
        }
    }
}
