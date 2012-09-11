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
using CloudWebPortal.Areas.Aneka.Models;

namespace CloudWebPortal.Controllers
{
    public class SharedController : Controller
    {
        private AnekaDbContext db = new AnekaDbContext();

        //
        // GET: /Shared/_IssuesList

        [Authorize]
        public ActionResult _IssuesList()
        {
            ViewBag.Machines = db.Machines.ToList();
            ViewBag.Clouds = db.Clouds.ToList();

            return View();
        }

        //
        // GET: /Shared/_ActivitiesList

        [Authorize]
        public ActionResult _ActivitiesList()
        {
            ViewBag.Machines = db.Machines.ToList();
            ViewBag.Clouds = db.Clouds.ToList();

            return View();
        }

    }
}
