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
using CloudWebPortal.Models;
using Aneka.Security;

namespace CloudWebPortal.Logic
{
    public class AnekaUsersManagement
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();
        private Aneka.UI.Security.SecurityManager securityManager = null;

        public AnekaUsersManagement(Uri serviceUri, UserCredentials userCredentials)
        {
            securityManager = new Aneka.UI.Security.SecurityManager(serviceUri, userCredentials);
        }

        /// <summary>
        /// Create a new Aneka cloud/master user for a specific cloud
        /// </summary>
        /// <param name="cloudUserAccount">The new  account information to add</param>
        /// <param name="cloudID">The cloud ID to add this user to</param>
        public void createNewUser(CloudUserAccount cloudUserAccount, int cloudID)
        {
            // Check to see if a non-empty username was specified
            if (string.IsNullOrEmpty(cloudUserAccount.Username) == true)
            {
                throw new Exception("Invalid User Name: The user name cannot be empty or contain only white-spaces");
            }

            // Validate password
            if (cloudUserAccount.Password.Length < 6)
            {
                throw new Exception("Invalid Password: The password must be at least six characters long");
            }

            List<string> groups = new List<string>();

            // [DK] NOTE: Do we need to have a selection list of
            //            groups in this form?

            UserCredentials credentials = new UserCredentials(cloudUserAccount.Username, cloudUserAccount.Password, cloudUserAccount.Username, cloudUserAccount.useThisAccountForReporting ? "This account will be used for reporting" : "Normal account", groups);
            securityManager.CreateUser(credentials);

            CloudUserAccount CloudUserAccountfromDB = db.CloudUserAccounts.Find(cloudUserAccount.CloudUserAccountId);
            Cloud cloudFromDB = db.Clouds.Find(cloudID);
            CloudUserAccountfromDB.Clouds.Add(cloudFromDB);
            db.SaveChanges();
        }

        /// <summary>
        /// Get all the users from a cloud
        /// </summary>
        /// <returns>A list of users</returns>
        public List<CloudUserAccount> getAllUsersFromTheCloud()
        {
            List<CloudUserAccount> cloudUserAccounts = new List<CloudUserAccount>();

            IList<UserCredentials> userList = securityManager.GetUserCredentials();

            foreach (UserCredentials credentials in userList)
            {
                cloudUserAccounts.Add(new CloudUserAccount{ Username = credentials.Username, Password = credentials.Password, useThisAccountForReporting = true});
            }

            return cloudUserAccounts;
        }
    }
}