// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;

namespace NuGet.Services.Staging.Manager.UnitTests
{
    public static class AttributeHelper
    {
        public static T GetControllerAttribute<T>(Controller controller) where T : Attribute
        {
            Type type = controller.GetType();
            object[] attributes = type.GetCustomAttributes(typeof(T), inherit: true);
            T attribute = attributes.Count() == 0 ? null : (T)attributes[0];
            return attribute;
        }

        public static T GetMethodAttribute<T>(Controller controller, string methodName, Type[] methodTypes) where T : Attribute
        {
            Type type = controller.GetType();
            MethodInfo method = methodTypes == null ? type.GetMethod(methodName) : type.GetMethod(methodName, methodTypes);
            object[] attributes = method.GetCustomAttributes(typeof(T), inherit: true);
            T attribute = attributes.Count() == 0 ? null : (T)attributes[0];
            return attribute;
        }

        public static bool HasServiceFilterAttribute<T>(Controller controller, string methodName, Type[] methodTypes) where T : IActionFilter
        {
            Type type = controller.GetType();
            MethodInfo method = methodTypes == null ? type.GetMethod(methodName) : type.GetMethod(methodName, methodTypes);
            object[] attributes = method.GetCustomAttributes(typeof(ServiceFilterAttribute), inherit: true);

            return attributes.Any(att => ((ServiceFilterAttribute) att).ServiceType == typeof(T));
        }
    }
}