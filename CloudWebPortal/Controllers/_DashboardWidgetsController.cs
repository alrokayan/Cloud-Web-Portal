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
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CloudWebPortal.Models;

namespace CloudWebPortal.Controllers
{ 
    public class _DashboardWidgetsController : Controller
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();

        /// <summary>
        /// WIDGET: Detailed information about a specific Widget
        /// </summary>
        /// <param name="id">Widget ID</param>
        /// <returns></returns>
        [Authorize]
        public ViewResult Details(int id)
        {
            //Get the Widget entity from the database using the given ID
            Widget widget = db.Widgets.Find(id);

            //Pass this entity to the view
            return View(widget);
        }

        /// <summary>
        /// WIDGET: A form to create a new Widget
        /// </summary>
        /// <param name="id">WebPortalLoginCredential ID: This ID is an optional parameter to make a specific web portal user the default one in the users dropdown list</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Create(int? id)
        {
            //Pass a dropdown list of portal users to assign this new widget to.
            ViewBag.PortalUsers = new SelectList(db.WebPortalLoginCredentials.ToList(), "WebPortalLoginCredentialId", "Username", id);
            ViewBag.ActionId = 0;

            return PartialView();
        }


        /// <summary>
        /// WIDGET: This is a form to create a widget to the logged in user, and filling the form with Action Name and Controller Name of the selected widget.
        /// This widget will be called when the user clicked on the green plus button on the top right corner of each widget.
        /// </summary>
        /// <param name="username">Loggedin Username</param>
        /// <param name="actionName">Selected Widget Action Name</param>
        /// <param name="controllerName">Selected Widget Controller Name</param>
        /// <param name="width">The suggested width</param>
        /// <param name="actionId">The action ID if there is any - Optional</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult CreateForLoggedInUser(string username, string actionName, string controllerName, int width, int? actionId, string areaName)
        {
            //Get the WebPortalLoginCredential entity by looking up for the user using its username
            var LoggedInUser = db.WebPortalLoginCredentials.ToList().Where(x => x.Username == System.Web.HttpContext.Current.User.Identity.Name).First();
            
            //Pass a dropdown list of users to the view.
            ViewBag.PortalUsers = new SelectList(db.WebPortalLoginCredentials.ToList(), "WebPortalLoginCredentialId", "Username", LoggedInUser.WebPortalLoginCredentialId);

            //Pass the Action Name to the view.
            ViewBag.ActionName = actionName;
            //Pass the Controller Name to the View
            ViewBag.ControllerName = controllerName;
            //Pass the width to the View
            ViewBag.Width = width;
            //Pass the Action ID to the View.
            if (actionId.HasValue)
                ViewBag.ActionId = actionId;
            else
                ViewBag.ActionId = 0;

            ViewBag.AreaName = areaName;

            return PartialView("Create");
        } 

        /// <summary>
        /// This is the post action for creating a Widget
        /// </summary>
        /// <param name="widget"></param>
        /// <param name="PortalUsers"></param>
        /// <param name="returnView"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult CreatePost(Widget widget, int? PortalUsers, string returnView)
        {
            if (!PortalUsers.HasValue)
                ModelState.AddModelError(string.Empty, "Please choose a user");

            if (ModelState.IsValid)
            {
                db.Widgets.Add(widget);
                db.SaveChanges();
                db.WebPortalLoginCredentials.Find(PortalUsers).Widgets.Add(widget);
                db.SaveChanges();
                return RedirectToAction("Dashboard", "Home"); 
            }

            ViewBag.PortalUsers = new SelectList(db.WebPortalLoginCredentials.ToList(), "WebPortalLoginCredentialId", "Username", PortalUsers);
            return View("~/Views/Home/Dashboard.cshtml", widget);
        }
        
        /// <summary>
        /// WIDGET: Edit a specific Widget by getting it’s ID
        /// </summary>
        /// <param name="id">Widget ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Edit(int id)
        {
            //Get the Widget entity from the database.
            Widget widget = db.Widgets.Find(id);

            //Pass the whole entity to the view so the user can edit it.
            return View(widget);
        }

        /// <summary>
        /// This is the post action to edit a Widget
        /// </summary>
        /// <param name="widget"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult EditPost(Widget widget)
        {
            if (ModelState.IsValid)
            {
                db.Entry(widget).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Dashboard", "Home");
            }
            return View("~/Views/Home/Dashboard.cshtml", widget);
        }

        /// <summary>
        /// WIDGET: A confirmation message to delete a Widget
        /// </summary>
        /// <param name="id">Widget ID</param>
        /// <returns></returns>
        [Authorize]
        public ActionResult Delete(int id)
        {
            //Get the Widget entity from the database using the given ID
            Widget widget = db.Widgets.Find(id);

            //Pass this entity to the view
            return View(widget);
        }

        /// <summary>
        /// This is the post action to delete a Widget
        /// </summary>
        /// <param name="WidgetId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost, ActionName("DeletePost")]
        public ActionResult DeleteConfirmed(int WidgetId)
        {
            Widget widget = db.Widgets.Find(WidgetId);
            db.Widgets.Remove(widget);
            db.SaveChanges();
            return RedirectToAction("Dashboard", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}