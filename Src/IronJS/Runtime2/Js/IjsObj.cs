﻿using System.Collections.Generic;
using System.Collections;

namespace IronJS.Runtime2.Js
{
    public class IjsObj
    {
        public Dictionary<object, object> Properties =
            new Dictionary<object, object>();

        public void Set(object name, object value)
        {
            Properties[name] = value;
        }

        public object Get(object name)
        {
            object value;

            if (Properties.TryGetValue(name, out value))
                return value;

            return Undefined.Instance;
        }
    }
}
