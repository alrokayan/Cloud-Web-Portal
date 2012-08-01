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

namespace CloudWebPortal.Logic.Reporting.Helpers
{
    /// <summary>
    /// Class <i><b>Node</b></i>. Represents a node in the network with performance
    /// data. This class is used to keep track of the list of unique nodes with 
    /// performance data within a certain time slot.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// A <see langword="string"/> representing a uri to 
        /// remote node.
        /// </summary>
        private string nodeUri;

        /// <summary>
        /// Gets or sets a <see langword="string"/> representing the
        /// uri to a remote node.
        /// </summary>
        public string NodeUri
        {
            get
            {
                return this.nodeUri;
            }
            set
            {
                this.nodeUri = value;
            }
        }

        /// <summary>
        /// The CPU usage (%) of the node.       
        /// </summary>
        private double cpuUsage;

        /// <summary>
        /// Gets or sets the CPU usage (%) of the node.       
        /// </summary>
        public double CpuUsage
        {
            get
            {
                return this.cpuUsage;
            }
            set
            {
                this.cpuUsage = value;
            }
        }

        /// <summary>
        /// The cpu capacity of the node.
        /// </summary>
        private double cpuCapacity;

        /// <summary>
        /// Gets or sets the cpu capacity of the node.
        /// </summary>
        public double CpuCapacity
        {
            get
            {
                return this.cpuCapacity;
            }
            set
            {
                this.cpuCapacity = value;
            }
        }

        /// <summary>
        /// The memory capacity of the node.
        /// </summary>
        private double memoryCapacity;

        /// <summary>
        /// Gets or sets the memory capacity of the node.
        /// </summary>
        public double MemoryCapacity
        {
            get
            {
                return this.memoryCapacity;
            }
            set
            {
                this.memoryCapacity = value;
            }
        }

        /// <summary>
        /// The storage capacity of the node.
        /// </summary>
        private double storageCapacity;

        /// <summary>
        /// Gets or sets the storage capacity of the node.
        /// </summary>
        public double StorageCapacity
        {
            get
            {
                return this.storageCapacity;
            }
            set
            {
                this.storageCapacity = value;
            }
        }

        /// <summary>
        /// Creates and instance of <see cref="T:Aneka.UI.Reporting.Node"/>
        /// and initializes it with the given node uri.
        /// </summary>
        /// <param name="nodeUri">The uri of the node to be represented.
        /// </param>
        public Node(string nodeUri)
        {
            this.nodeUri = nodeUri;
        }
    }
}