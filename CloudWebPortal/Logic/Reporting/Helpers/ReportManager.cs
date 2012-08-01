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
using System.Text;
using Aneka.Accounting;
using Aneka.Entity;
using Aneka.Monitoring;
using Aneka.Security;
using Aneka.Reporting;
using Aneka;

namespace CloudWebPortal.Logic.Reporting.Helpers
{
    /// <summary>
    /// Class <i><b>ReportManager</b></i>. This class is a client side proxy for invoking 
    /// remote sericves such as the <see cref="T:Aneka.Reporting.ReportingService"/> and
    /// the <see cref="T:Aneka.Runtime.Security.SecurityService"/> and provides operations 
    /// for retrieving data on performance utilization, storage utilization and billing 
    /// for generating summaries, graphs and reports.
    /// </summary>
    public class ReportManager
    {
        /// <summary>
        /// The source name to use when communicating with the remote service.
        /// </summary>
        private static readonly string ClientName = "AnekaAnalytics";

        /// <summary>
        /// The address of the <see cref="T:Aneka.Runtime.ReportingService"/> 
        /// to communicate with.
        /// </summary>
        private ServiceAddress serviceAddress;

        /// <summary>
        /// The <see cref="T:Aneka.Security.UserCredentials"/> to use to 
        /// authenticate with <see cref="T:Aneka.Runtime.ReportingService"/> 
        /// service.
        /// </summary>
        private UserCredentials userCredentials;

        /// <summary>
        /// A byte array for sending authentication data 
        /// when communicating with a remote Aneka service.
        /// </summary>
        private byte[] securityToken;

        /// <summary>
        /// Creates an instance of the <see cref="T:Aneka.UI.Reporting.ReportManager"/>
        /// and initializes it with the given <see cref="T:Aneka.Entity.ServiceAddress"/>
        /// and <see cref="T:Aneka.Security.UserCredentials"/>.
        /// </summary>
        public ReportManager(ServiceAddress serviceAddress, UserCredentials userCredentials)
        {
            this.serviceAddress = serviceAddress;
            this.userCredentials = userCredentials;
            this.securityToken = userCredentials.ToByte();
        }

        /// <summary>
        /// Queries for reporting data of the specified <paramref name="type"/> within the specified period.
        /// </summary>
        /// <param name="reportingType">The <see cref="T:System.Type"/> of reporting data to query.</param>
        /// <param name="fromDate">The starting <see cref="T:System.DateTime"/> of data to query.</param>
        /// <param name="toDate">The ending <see cref="T:System.DateTime"/> of the data to query.</param>        
        /// <returns>An array of <see cref="T:Aneka.Reporting.ReportingData"/> instances</returns>
        public ReportingData[] QueryReportingData(Type reportingType, DateTime fromDate, DateTime toDate)
        {
            ReportingDataQueryRequestMessage queryMessage = new ReportingDataQueryRequestMessage(reportingType, fromDate, toDate);
            queryMessage.SecurityToken = this.securityToken;

            Message response = MessageDispatcher.SendMessage(ClientName, this.serviceAddress.Uri, queryMessage);

            if (response is ErrorMessage)
            {
                throw new ApplicationException("Failed to query data: " + ((ErrorMessage)response).Cause.Message);
            }
            else
            {
                return ((ReportingDataQueryResponseMessage)response).ReportingDataItems;
            }
        }

        /// <summary>
        /// Clears reporting data of the specified <paramref name="type"/> within the specified period.
        /// </summary>
        /// <param name="reportingType">The <see cref="T:System.Type"/> of the reporting data to clear.</param>
        /// <param name="fromDate">The starting <see cref="T:System.DateTime"/> of the data to clear.</param>
        /// <param name="toDate">The ending <see cref="T:System.DateTime"/> of the data to clear.</param>        
        public void ClearReportingData(Type reportingType, DateTime fromDate, DateTime toDate)
        {
            ReportingDataClearRequestMessage clearMessage = new ReportingDataClearRequestMessage(reportingType, fromDate, toDate);
            clearMessage.SecurityToken = this.securityToken;

            Message response = MessageDispatcher.SendMessage(ClientName, this.serviceAddress.Uri, clearMessage);

            if (response is ErrorMessage)
            {
                throw new ApplicationException("Failed to clear data: " + ((ErrorMessage)response).Cause.Message);
            }
        }

        /// <summary>
        /// Queries for application data for billing purposes. This method returns a list of 
        /// <see cref="T:Aneka.Entity.ApplicationView"/> instances containing user and cost
        /// information for generating bills.
        /// </summary>
        /// <param name="fromDate">The starting <see cref="T:System.DateTime"/> of period to query</param>
        /// <param name="toDate">The ending <see cref="T:System.DateTime"/> of the period to query</param>
        /// <returns>A list of <see cref="T:Aneka.Entity.ApplicationView"/>s ensapsulating 
        /// data on each of the applications completed within the specified period</returns>
        public IList<ApplicationView> QueryBillingData(DateTime fromDate, DateTime toDate)
        {
            IList<ApplicationView> applicationViews = new List<ApplicationView>();

            // Define new request to query all applications executed within 
            // the specified timeslot
            AccountingQueryRequest request = new AccountingQueryRequest(fromDate, toDate);

            // Compose query message 
            AccountingQueryMessage queryMessage = new QueryApplicationMessage(Constants.AccountingService, request);
            queryMessage.SecurityToken = this.securityToken;

            // Dispatch message and check result
            Message response = MessageDispatcher.SendMessage(ClientName, this.serviceAddress.Uri, queryMessage);

            if (response is ErrorMessage)
            {
                throw new ApplicationException("Failed to query utilization history: " + ((ErrorMessage)response).Cause);
            }
            else if (response is AccountingQueryResponseMessage)
            {
                applicationViews = ((AccountingQueryResponseMessage)response).DataList.ConvertAll<ApplicationView>(
                                    delegate(IAccountable accountable)
                                    {
                                        return (ApplicationView)accountable;
                                    });
            }

            return applicationViews;
        }

        /// <summary>
        /// Queries the list of <see cref="T:Aneka.Security.UserCredentials"/> instances for
        /// the given list of <paramref name="users"/>.
        /// </summary>
        /// <param name="users">The list of users whose <see cref="T:Aneka.Security.UserCredentials"/>
        /// is to be queried</param>
        /// <returns>A list of <see cref="T:Aneka.Security.UserCredentials"/> instances</returns>
        public IList<UserCredentials> QueryUserCredentials(IList<string> users)
        {
            List<UserCredentials> userCredentials = new List<UserCredentials>();

            QueryUserMessage queryUserMsg = new QueryUserMessage();
            queryUserMsg.SecurityToken = securityToken;
            queryUserMsg.Users = users;

            // Send query message
            Message response = MessageDispatcher.SendMessage(ClientName, this.serviceAddress.Uri, queryUserMsg);

            // Check query response
            if (response is QueryUserResponseMessage)
            {
                userCredentials = ((QueryUserResponseMessage)response).Credentials;
            }
            else if (response is ErrorMessage)
            {
                throw new ApplicationException("Error while quering user information. " + ((ErrorMessage)response).Cause.Message);
            }
            else
            {
                throw new ApplicationException("Failed to query user information");
            }

            return userCredentials;
        }
    }
}
