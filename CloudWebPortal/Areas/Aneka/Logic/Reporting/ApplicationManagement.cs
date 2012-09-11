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
using Aneka;
using Aneka.Accounting;
using Aneka.Security;

namespace CloudWebPortal.Logic.Reporting
{
    public static class appQureyType
    {
        public enum Type
        {
            ALL = 10,
            NonSuccessfulOnly = 20
        }
    }


    public class ApplicationManagement
    {
        private UserCredentials credentials;
        private global::Aneka.Entity.ServiceAddress serviceAddress;

        public ApplicationManagement(String username, String password, String masterHostIP, int masterPortNumber)
        {
            credentials = new UserCredentials(username, password);
            serviceAddress = new global::Aneka.Entity.ServiceAddress(ContainersManagement.GetContainerUri(masterHostIP, masterPortNumber), "AccountingService");
        }

        /// <summary>
        /// This function will get all the active applications in the Aneka
        /// </summary>
        /// <returns>A list of all active applications</returns>
        public List<IAccountable> getActiveApplications()
        {
            List<IAccountable> activeView = null; ;
            try
            {
                //create a query active application message
                QueryActiveApplicationMessage queryMessage = new QueryActiveApplicationMessage(credentials.Username, serviceAddress.Name);
                queryMessage.SecurityToken = credentials.ToByte();

                Message retMsg = MessageDispatcher.SendMessage("AccountingManager", serviceAddress.Uri, queryMessage);

                //check the query response
                QueryActiveApplicationResponseMessage response = retMsg as QueryActiveApplicationResponseMessage;

                if (response != null)
                {
                    //get the list of active application views
                    activeView = response.Applications;
                }
            }
            catch (Exception ex)
            {

            }
            return activeView;
        }

        /// <summary>
        /// This function will get all the historical applications from Aneka
        /// </summary>
        /// <param name="data">The type of data to get from Aneka</param>
        /// <param name="fromDate">Limit the result From This Date</param>
        /// <param name="toDate">Limit the result To This Date</param>
        /// <returns></returns>
        public List<IAccountable> getDoneApplications(appQureyType.Type data, DateTime? fromDate, DateTime? toDate)
        {
            List<IAccountable> appViewListResult = new List<IAccountable>();
            try
            {
                List<IAccountable> appViewList = null;

                global::Aneka.Accounting.AccountingManager manager = new AccountingManager(serviceAddress);
                AccountingQueryRequest request;
                if (fromDate == null || toDate == null)
                {
                    request = new AccountingQueryRequest(credentials.Username, QueryMode.USER, 1, 1);
                }
                else
                {
                    request = new AccountingQueryRequest((DateTime)fromDate, (DateTime)toDate);
                }

                //query via the accounting manager
                appViewList = manager.Query(credentials, typeof(global::Aneka.Entity.ApplicationView), request);

                if (data == appQureyType.Type.NonSuccessfulOnly)
                {
                    foreach (global::Aneka.Entity.ApplicationView app in appViewList)
                    {
                        if (app.Total != app.Completed)
                            appViewListResult.Add(app);
                    }
                }
                if (data == appQureyType.Type.ALL)
                    appViewListResult = appViewList;
            }
            catch (Exception)
            {
                throw;
            }

            return appViewListResult;
        }

        /// <summary>
        /// This function to force close an active application
        /// </summary>
        /// <param name="ApplicationMasterService">The master service name</param>
        /// <param name="ApplicationID">Application ID</param>
        public void AbortApplication(String ApplicationMasterService, String ApplicationID)
        {
            try
            {
                global::Aneka.Entity.ApplicationAbortMessage abortMsg = new global::Aneka.Entity.ApplicationAbortMessage("AccountingManager", ApplicationMasterService, ApplicationID);
                abortMsg.SecurityToken = credentials.ToByte();

                //send the abort message
                Message msg = MessageDispatcher.SendMessage(
                    "AccountingManager",
                    this.serviceAddress.Uri,
                    abortMsg);

                // Console.WriteLine(errorMsg.ToString());

            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message);
            }
        }
    }
}