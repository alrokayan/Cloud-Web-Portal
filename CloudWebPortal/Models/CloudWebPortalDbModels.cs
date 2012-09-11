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
using System.ComponentModel.DataAnnotations;
using DataAnnotationsExtensions;
using Aneka.PAL.Management.Model;
using Aneka.UI.Configuration;

namespace CloudWebPortal.Models
{
    public class WebPortalLoginCredential
    {
        public virtual int WebPortalLoginCredentialId { get; set; }

        [Required]
        [Display(Name = "User name")]
        public virtual string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public virtual string Password { get; set; }

        [Display(Name = "Widgets on the dashboard")]
        public virtual ICollection<Widget> Widgets { get; set; }
    }

    public class Widget
    {
        public Widget() { Width = 6; }

        public virtual int WidgetId { get; set; }

        [Required]
        [Display(Name = "Controller Name")]
        public virtual string ControllerName { get; set; }

        [Required]
        [Display(Name = "Action Name")]
        public virtual string ActionName { get; set; }

        [Display(Name = "Action Id Number (like user ID or cloud ID)")]
        public virtual int ActionId { get; set; }

        [Required]
        [Display(Name = "Widget Width")]
        [Range(1, 12, ErrorMessage = "the width must be between 1 and 12")]
        public virtual int Width { get; set; }
    }

    public class CloudWebPortalManagement
    {
        public bool loginToCloudWebPortal(string username, string password)
        {
            var db = new CloudWebPortalDbContext();
            var login = from loginEntity in db.WebPortalLoginCredentials
                        where loginEntity.Username == username && loginEntity.Password == password
                        select loginEntity;

            foreach (var l in login)
                return true;
                
            return false;
        }
    }
}