// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace Stage.Manager.UnitTests
{
    /// <summary>
    /// Taken from: https://datatellblog.wordpress.com/2015/05/05/unit-testing-asp-net-mvc-authorization/
    /// </summary>
    public static class AuthorizationTest
    {
        /// <summary>
        /// Check to see if a method allows anonymous access -
        /// 1. A method is anonymous if it is decorated with the AllowAnonymousAttribute attribute.
        /// 2. Or, a method is anonymous if neither the method nor controller are decorated with the AuthorizeAttribute attribute.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="methodName"></param>
        /// <param name="methodTypes">Optional</param>
        /// <returns>true is method is anonymous</returns>
        public static bool IsAnonymous(Controller controller, string methodName, Type[] methodTypes)
        {
            return AttributeHelper.GetMethodAttribute<AllowAnonymousAttribute>(controller, methodName, methodTypes) != null ||
                (AttributeHelper.GetControllerAttribute<AuthorizeAttribute>(controller) == null &&
                    AttributeHelper.GetMethodAttribute<AuthorizeAttribute>(controller, methodName, methodTypes) == null);

        }

        /// <summary>
        /// Check to see if a method requires authorization -
        /// 1. A method is authorized if it is decorated with the Authorize attribute.
        /// 2. Or, a method is authorized if the controller is decorated with the AuthorizeAttribute attribute, and
        /// the method is not decorated with the AllowAnonymousAttribute attribute.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="methodName"></param>
        /// <param name="methodTypes">Optional</param>
        /// <returns></returns>
        public static bool IsAuthorized(Controller controller, string methodName, Type[] methodTypes)
        {
            return AttributeHelper.GetMethodAttribute<AuthorizeAttribute>(controller, methodName, methodTypes) != null ||
                (AttributeHelper.GetControllerAttribute<AuthorizeAttribute>(controller) != null &&
                    AttributeHelper.GetMethodAttribute<AllowAnonymousAttribute>(controller, methodName, methodTypes) == null);
        }

        ///// <summary>
        ///// Check to see if a method requires authorization for the roles and users specified
        ///// </summary>
        ///// <param name="controller"></param>
        ///// <param name="methodName"></param>
        ///// <param name="methodTypes">Optional</param>
        ///// <param name="roles"></param>
        ///// <param name="users"></param>
        ///// <returns></returns>
        //public static bool IsAuthorized(Controller controller, string methodName, Type[] methodTypes, string[] roles, string[] users)
        //{
        //    if (roles == null && users == null)
        //        return IsAuthorized(controller, methodName, methodTypes);

        //    if (!IsAuthorized(controller, methodName, methodTypes))
        //        return false;

        //    AuthorizeAttribute controllerAttribute = GetControllerAttribute<AuthorizeAttribute>(controller);
        //    AuthorizeAttribute methodAttribute = GetMethodAttribute<AuthorizeAttribute>(controller, methodName, methodTypes);

        //    // Check to see if all roles are authorized
        //    if (roles != null)
        //    {
        //        foreach (string role in roles)
        //        {
        //            string lowerRole = role.ToLower();

        //            bool roleIsAuthorized =
        //                (controllerAttribute != null ?
        //                    controllerAttribute.Roles.ToLower().Split(',').Any(r => r == lowerRole) : false) ||
        //                (methodAttribute != null ?
        //                    methodAttribute.Roles.ToLower().Split(',').Any(r => r == lowerRole) : false);

        //            if (!roleIsAuthorized)
        //                return false;
        //        }
        //    }

        //    // Check to see if all users are authorized
        //    if (users != null)
        //    {
        //        foreach (string user in users)
        //        {
        //            string lowerUser = user.ToLower();

        //            bool userIsAuthorized =
        //                (controllerAttribute != null ?
        //                    controllerAttribute.Users.ToLower().Split(',').Any(u => u == lowerUser) : false) ||
        //                (methodAttribute != null ?
        //                    methodAttribute.Users.Split(',').Any(u => u.ToLower() == lowerUser) : false);

        //            if (!userIsAuthorized)
        //                return false;
        //        }
        //    }

        //    return true;
        //}
    }
}
