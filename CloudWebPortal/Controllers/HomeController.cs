/*
 * Copyright 2011,2012 CLOUDS Laboratory, The University of Melbourne
 *  
 *  This file is part of Cloud Web Portal (CWP).
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CloudWebPortal.Models;
using System.Web.Security;

namespace CloudWebPortal.Controllers
{
    public class HomeController : Controller
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();

        //
        // GET: /Home/

        [Authorize]
        public ActionResult Dashboard()
        {
            return View();
        }

        [Authorize]
        public ActionResult DashboardContent()
        {
            var LoggedInUser = db.WebPortalLoginCredentials.ToList().Where(x => x.Username == System.Web.HttpContext.Current.User.Identity.Name).First();

            return View(LoggedInUser.Widgets.ToList());
        }


        //
        // Get: /Home/LogOn

        public ActionResult LogOn()
        {
            return PartialView();
        }



        //
        // POST: /Home/LogOn

        [HttpPost]
        public ActionResult LogOn(WebPortalLoginCredential model, string returnUrl, int? keep_logged)
        {
            bool RememberMe = true;
            if (keep_logged == null) RememberMe = false;

            if (ModelState.IsValid)
            {
                CloudWebPortalManagement a = new CloudWebPortalManagement();
                if (a.loginToCloudWebPortal(model.Username, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.Username, RememberMe);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                            && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Dashboard", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
            }


            return PartialView(model);
        }

        [Authorize]
        public ActionResult LogOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("LogOn", "Home");
        }
    }
}
