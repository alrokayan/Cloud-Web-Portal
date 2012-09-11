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

namespace CloudWebPortal.Areas.Aneka.Models
{
    

    public class Service
    {
        public Service() { isMasterOnly = false; }

        public virtual int ServiceId { get; set; }

        [Required]
        [Display(Name = "Service name")]
        public virtual string Name { get; set; }

        [Display(Name = "Is this service for master only?")]
        public virtual bool isMasterOnly { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Configuration Spring XML Segment")]
        public virtual string SpringSegmentXML { get; set; }

        [Display(Name = "Associated Workers")]
        public virtual ICollection<Worker> Workers { get; set; }

        [Display(Name = "Associated Masters")]
        public virtual ICollection<Master> Masters { get; set; }
    }

    public class CloudUserAccount
    {
        public virtual int CloudUserAccountId { get; set; }

        [Required]
        [Display(Name = "User name")]
        public virtual string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [MinLength(6, ErrorMessage = "Invalid Password: The password must be at least six characters long")]
        public virtual string Password { get; set; }

        [Required]
        [Display(Name = "Do you allow to use this account to display reporting and accounting in the portal?")]
        public virtual bool useThisAccountForReporting { get; set; }

        [Display(Name = "Associated Clouds")]
        public virtual ICollection<Cloud> Clouds { get; set; }
    }

    public class SoftwareAppliance
    {
        public virtual int SoftwareApplianceId { get; set; }

        [Required]
        [Display(Name = "Software appliance name")]
        public virtual string Name { get; set; }

        [Required]
        [Display(Name = "Software appliance vendor")]
        public virtual string Vendor { get; set; }

        [Required]
        [Display(Name = "Software appliance version number")]
        public virtual string Version { get; set; }

        [Display(Name = "Associated Machines")]
        public virtual ICollection<Machine> Machines { get; set; }
    }

    

    public class Worker
    {
        public Worker() { isQuarantined = false; }

        public virtual int WorkerId { get; set; }

        [Display(Name = "Worker display name")]
        public virtual string DisplayName { get; set; }

        [Required]
        [Display(Name = "Worker port number")]
        public virtual int Port { get; set; }

        [Display(Name = "Cost")]
        public virtual int Cost { get; set; }

        public virtual String AnekaContainerID { get; set; }

        public virtual Boolean isInstalled { get; set; }

        public virtual int Status { get; set; }

        public virtual ProbeStatus StatusEnum
        {
            get { return (ProbeStatus)Status; }
            set { Status = (int)value; }
        }

        public virtual bool isInProgress { get; set; }

        public virtual string ProgressMesage { get; set; }

        [Display(Name = "Do you want to quarantine this container?")]
        public virtual bool isQuarantined { get; set; }

        [Display(Name = "Associated Services")]
        public virtual ICollection<Service> Services { get; set; }
    }

    public class Master
    {

        public virtual int MasterId { get; set; }

        [Display(Name = "Master display name")]
        public virtual string DisplayName { get; set; }

        [Required]
        [Display(Name = "Master port number")]
        public virtual int Port { get; set; }

        [Display(Name = "Cost")]
        public virtual int Cost { get; set; }

        [Display(Name = "Master Failover Backup URI")]
        public virtual string MasterFailoverBackupURI { get; set; }

        public virtual String AnekaContainerID { get; set; }

        public virtual Boolean isInstalled { get; set; }

        public virtual int Status { get; set; }
        public virtual ProbeStatus StatusEnum
        {
            get { return (ProbeStatus)Status; }
            set { Status = (int)value; }
        }

        public virtual bool isInProgress { get; set; }

        public virtual string ProgressMesage { get; set; }

        [Display(Name = "Associated Services")]
        public virtual ICollection<Service> Services { get; set; }
    }

    public class Cloud
    {
        public virtual int CloudId { get; set; }

        [Required]
        [Display(Name = "Cloud Name")]
        public virtual string CloudName { get; set; }

        [Display(Name = "DB Connection String")]
        public virtual string DBConnectionString { get; set; }

        public virtual string SecuritySharedKey { get; set; }

        public virtual Master Master { get; set; }

        
        [Display(Name = "Associated Workers")]
        public virtual ICollection<Worker> Workers { get; set; }

        [Display(Name = "Associated User Accounts")]
        public virtual ICollection<CloudUserAccount> CloudUserAccounts { get; set; }
    }

    public class Daemon
    {
        public virtual int DaemonId { get; set; }

        [Required]
        [Display(Name = "Daemon port number")]
        public virtual int Port { get; set; }

        [Required]
        [Display(Name = "Daemon installation directory")]
        public virtual string Directory { get; set; }
    }

    public class Machine
    {
        public virtual int MachineId { get; set; }

        [Required]
        [Display(Name = "Machine Display Name")]
        public virtual string DisplayName { get; set; }

        [Required]
        [Display(Name = "IP address")]
        public virtual string IP { get; set; }

        [Display(Name = "Platform/OS")]
        public virtual MachinePlatform Platform { get; set; }

        [Display(Name = "Machine type")]
        public virtual MachineType Type { get; set; }

        public virtual Daemon Daemon { get; set; }

        [Display(Name = "Associated Workers")]
        public virtual ICollection<Worker> Workers { get; set; }

        [Display(Name = "Associated Masters")]
        public virtual ICollection<Master> Masters { get; set; }

        public virtual int Status { get; set; }

        public virtual DaemonProbeStatus StatusEnum
        {
            get { return (DaemonProbeStatus)Status; }
            set { Status = (int)value; }
        }

        public virtual bool isInProgress { get; set; }

        public virtual string ProgressMesage { get; set; }

        [Display(Name = "Associated Software Appliances")]
        public virtual ICollection<SoftwareAppliance> SoftwareAppliances { get; set; }
    }

    public class MachineLoginCredential
    {
        public virtual int MachineLoginCredentialId { get; set; }

        [Required]
        [Display(Name = "User name")]
        public virtual string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public virtual string Password { get; set; }

        [Display(Name = "Associated Machines")]
        public virtual ICollection<Machine> Machines { get; set; }
    }

    public class MachineType
    {
        public virtual int MachineTypeId { get; set; }

        public virtual string Type { get; set; }
    }

    public class MachinePlatform
    {
        public virtual int MachinePlatformId { get; set; }

        public virtual string Platform { get; set; }
    }

    public class ResourcePool
    {
        public virtual int ResourcePoolId { get; set; }

        [Required]
        [Display(Name = "Resource Pool Display Name")]
        public virtual string ResourcePoolDisplayName { get; set; }

        [Display(Name = "Associated Machines")]
        public virtual ICollection<Machine> Machines { get; set; }
    }
}