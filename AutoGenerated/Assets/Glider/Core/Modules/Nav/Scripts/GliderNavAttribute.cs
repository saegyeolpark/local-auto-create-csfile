using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Glider.Core.Nav
{
    [AttributeUsage(AttributeTargets.Method)]
    public class NavMethodAttribute : Attribute
    {
        public Type[] parameters;

        public NavMethodAttribute()
        {
        }

        public NavMethodAttribute(params Type[] parameters)
        {
            this.parameters = parameters;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class NavControllerAttribute : Attribute
    {
        public Type type;

        public NavControllerAttribute(Type type)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class NavFieldAttribute : Attribute
    {
        public NavFieldAttribute()
        {
        }
    }
}