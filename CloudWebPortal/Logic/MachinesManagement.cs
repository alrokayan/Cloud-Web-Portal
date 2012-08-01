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
using Aneka.PAL.Management.Model;
using Aneka.PAL.Management.Impl;
using CloudWebPortal.Models;
using System.Runtime.Serialization;
using log4net;

// Configure log4net using the .config file
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "Web.config", Watch = true)]

namespace CloudWebPortal.Logic
{
    public class MachinesManagement
    {
        private CloudWebPortalDbContext db = new CloudWebPortalDbContext();

        /// <summary>
        /// Get the latest Machine Status, and update it in the database
        /// </summary>
        /// <param name="machineID">Machine ID</param>
        /// <param name="login">Machine Login Credential</param>
        /// <returns>Machine Status</returns>
        public DaemonProbeResult UpdateMachineStatus(int machineID, MachineLoginCredential login)
        {
            DaemonProbeResult probeResult = new DaemonProbeResult(DaemonProbeStatus.Unknown,null);
            CloudWebPortal.Models.Machine machine = db.Machines.Find(machineID);
            if (pingMachine(machine.IP))
            {
                Aneka.PAL.Management.Model.Machine daemonMachine = ToAnekaPALMachineWithLogin(machine, login);
                probeResult = ProbeMachine(daemonMachine);

                machine.IP = daemonMachine.Address;
                machine.StatusEnum = probeResult.Status;

                db.SaveChanges();
            }
            else
            {
                machine.StatusEnum = DaemonProbeStatus.NetworkNotReachable;
                db.SaveChanges();
            }
            return probeResult;
        }

        /// <summary>
        /// Ping a Machine
        /// </summary>
        /// <param name="hostName">IP address or host name</param>
        /// <returns></returns>
        public bool pingMachine(string hostName)
        {
            bool result = true;
            try
            {
                // Ping machine to see if it is reachable.
                bool success = NetworkUtil.Ping(hostName);
                if (success == false)
                {
                    result = false;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.InnerException.Message;
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Probe a  machine
        /// </summary>
        /// <param name="daemonMachine">Daemon Machine entity</param>
        /// <returns></returns>
        public DaemonProbeResult ProbeMachine(Aneka.PAL.Management.Model.Machine daemonMachine)
        {
            DaemonProbeResult result = new DaemonProbeResult(DaemonProbeStatus.Unknown, null);

            if (daemonMachine != null)
            {
                // set the probe status to unknown
                daemonMachine.ProbeResult = result;

                // check service status
                if (daemonMachine.UserAccount != null)
                {
                    // Probe the target machine..
                    DaemonManager daemonManager = new DaemonManager();
                    result = daemonManager.Probe(daemonMachine);
                    
                    //The machine been pinged and it is reachable of course,
                    //so the statues can't be "NetworkNotReachable"!!!
                    //If DaemonManager.Probe returns "NetworkNotReachable"
                    //it is most likely that it is a credential problem, but maybe not.
                    //So the DaemonManager.Probe must be re-implemented to be more accurate.
                    if(result.Status == DaemonProbeStatus.NetworkNotReachable)
                        result =  new DaemonProbeResult(DaemonProbeStatus.BadCredentials, null);
                }
                else
                    result = new DaemonProbeResult(DaemonProbeStatus.BadCredentials, null);

            }

            //set the probe status of the machine
            return result;
            }


        /// <summary>
        /// Resolve the url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string ResolveURL(string url)
        {
            string resolvedUrl = null;
            try
            {
                Uri uri = new Uri(url);
                string hostname = uri.Host;

                //get the IP by the hostname
                string ip = NetworkUtil.GetIPByHostName(hostname);

                if (!string.IsNullOrEmpty(ip))
                {
                    resolvedUrl = string.Format("{0}://{1}:{2}{3}", uri.Scheme, ip, uri.Port, uri.PathAndQuery);
                }
            }
            catch (Exception ex)
            {

            }
            return resolvedUrl;
        }

        /// <summary>
        /// Convert the Machine entity from CloudWebPortal.Models.Machine to Aneka.PAL.Management.Model.Machine by passing the Machine Login Credential
        /// </summary>
        /// <param name="machine">CloudWebPortal.Models.Machine</param>
        /// <param name="login">Machine Login Credential</param>
        /// <returns>Aneka.PAL.Management.Model.Machine</returns>
        public Aneka.PAL.Management.Model.Machine ToAnekaPALMachineWithLogin(CloudWebPortal.Models.Machine machine, MachineLoginCredential login)
        {
            Aneka.PAL.Management.Model.Machine result = new Aneka.PAL.Management.Model.Machine(machine.IP);
            result.DaemonUri = ResolveURL(string.Format("tcp://{0}:{1}/daemon", result.Address, machine.Daemon.Port));
            if (login != null)
                result.UserAccount = new Aneka.PAL.Management.Model.UserAccount(login.Username, login.Password);
            result.Platform = machine.Platform.Platform;
            result.HomeDirectory = machine.Daemon.Directory + "\\LocalRepository\\Container";
            return result;
        }

        /// <summary>
        /// Convert the Machine entity from CloudWebPortal.Models.Machine to Aneka.PAL.Management.Model.Machine without passing the Machine Login Credential
        /// </summary>
        /// <param name="machine">CloudWebPortal.Models.Machine</param>
        /// <param name="login">Machine Login Credential</param>
        /// <returns>Aneka.PAL.Management.Model.Machine</returns>
        public Aneka.PAL.Management.Model.Machine ToAnekaPALMachine(CloudWebPortal.Models.Machine machine)
        {
            return ToAnekaPALMachineWithLogin(machine, getLogin(machine));
        }

        /// <summary>
        /// Get the Machine Login Credential from the Machine
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <returns>Machine Login Credential</returns>
        public MachineLoginCredential getLogin(CloudWebPortal.Models.Machine machine)
        {
            MachineLoginCredential login = null;
            var logins = db.MachineLoginCredentials.ToList();
            foreach (var loginEntity in logins)
            {
                foreach (var machineEntity in loginEntity.Machines)
                {
                    if (machineEntity.MachineId == machine.MachineId)
                        login = loginEntity;
                }
            }

            return login;
        }

        }


    
}