﻿using System;
using System.Web;
using IronJS.Runtime.Js;
using IronJS.Runtime.Utils;
using IronJS.Runtime.Js.Descriptors;

namespace IronJS.Runtime.Builtins
{
    class Global_obj_decodeURIComponent : NativeFunction
    {
        public Global_obj_decodeURIComponent(Context context)
            : base(context)
        {
            Set("length", new UserProperty(this, 1.0D));
        }

        public override object Call(IObj that, object[] args)
        {
            if (!HasArgs(args) || args[0] == null || args[0] is Undefined)
                throw new ArgumentException();

            return HttpUtility.UrlDecode(JsTypeConverter.ToString(args[0]));
        }
    }
}