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

namespace CloudWebPortal.Logic.Reporting.Helpers
{
    /// <summary>
    /// Class <i><b>ReportingUtil</b></i>. This class provides helper methods useful 
    /// for reporting purposes. These include mathematical operations on a list of 
    /// numbers, operations for data presentation, and threading.
    /// </summary>
    public sealed class ReportingUtil
    {
        #region Constants

        /// <summary>
        /// Represents the label in a line chart for a Time (x) axis
        /// </summary>
        public static readonly string LabelTime = "Time";

        /// <summary>
        /// Represents the lable in a bar chart for the User (x) axis
        /// </summary>
        public static readonly string LableUsers = "Users";

        /// <summary>
        /// Represents the lable in a bar chart for the Application (x) axis
        /// </summary>
        public static readonly string LabelApplications = "Applications";

        /// <summary>
        /// Represents the lable in a bar chart for Application costs
        /// </summary>
        public static readonly string LabelCost = "Cost ($)";

        /// <summary>
        /// Represents the lable for CPU Utilization axis
        /// </summary>
        public static readonly string LabelCPUUtilzation = "CPU Utilization (%)";

        /// <summary>
        /// Represents the label for Bandwidth axis
        /// </summary>
        public static readonly string LabelBandwidth = "Bandwidth";

        /// <summary>
        /// The string 'Kilobytes'
        /// </summary>
        public static readonly string LabelKilobytes = "Kilobytes";

        /// <summary>
        /// The string 'Megabytes'
        /// </summary>
        public static readonly string LabelMegabytes = "Megabytes";

        /// <summary>
        /// The string 'Gigabytes'
        /// </summary>
        public static readonly string LabelGigaBytes = "Gigabytes";

        /// <summary>
        /// The string 'GB'
        /// </summary>
        public static readonly string LabelGB = "GB";

        /// <summary>
        /// The string 'Ghz'
        /// </summary>
        public static readonly string LabelGHz = "GHz";

        /// <summary>
        /// The string 'Querying...'
        /// </summary>
        public static readonly string LabelQuerying = "Querying...";

        /// <summary>
        /// The string 'Done.'
        /// </summary>
        public static readonly string LabelDone = "Done.";

        /// <summary>
        /// The string 'Failed.'
        /// </summary>
        public static readonly string LabelFailed = "Failed.";

        /// <summary>
        /// Number of bytes in a Byte (yes, a little strange but added
        /// here for consistency)
        /// </summary>
        public static readonly long OneByte = 1;

        /// <summary>
        /// Number of bytes in a KiloByte
        /// </summary>
        public static readonly long OneKiloByte = OneByte * 1024;

        /// <summary>
        /// Number of bytes in a MegaByte
        /// </summary>
        public static readonly long OneMegaByte = OneKiloByte * 1024;

        /// <summary>
        /// Number of bytes in a GigaByte
        /// </summary>
        public static readonly long OneGigaByte = OneMegaByte * 1024;

        /// <summary>
        /// The granularity of a time axis in seconds. Note that this number
        /// must be larger that the Hearbet Interval, in order to ensure that
        /// <see cref="T:Aneka.Runtime.PerformanceInfo"/> is received from all
        /// nodes at least once. 
        /// </summary>
        public static readonly int ChartTimeGranularity = 60;

        #endregion

        #region Numerical Helpers

        /// <summary>
        /// Finds the maximim in the list of <see langword="double"/>s.
        /// </summary>
        /// <param name="numList">The list of <see langword="double"/>s.
        /// to search in</param>
        /// <returns>The maximum <see langword="double"/> in the list
        /// </returns>
        public static double Max(IList<double> numList)
        {
            double max = 0;

            for (int x = 0; x < numList.Count; x++)
            {
                if (numList[x] > max)
                {
                    max = numList[x];
                }
            }
            return max;
        }

        /// <summary>
        /// Finds the maximum <see cref="T:System.Collections.Generic.KeyValuePair"/> in
        /// the given dictionary. The maximum is determined by the entry's 'Value' property.
        /// </summary>
        /// <param name="dictionary">The <see cref="T:System.Collections.Generic.IDictionary"/>
        /// to search for.</param>
        /// <returns>A <see cref="T:System.Collections.Generic.KeyValuePair"/> entry</returns>
        public static KeyValuePair<string, double> Max(IDictionary<string, double> dictionary)
        {
            KeyValuePair<string, double> maxEntry = new KeyValuePair<string,double>(null, 0);

            foreach (KeyValuePair<string, double> entry in dictionary)
            {
                if (entry.Value > maxEntry.Value)
                {
                    maxEntry = entry;
                }
            }

            return maxEntry;
        }   

        /// <summary>
        /// Sums the list of <see langword="double"/>s in the given list.
        /// </summary>
        /// <param name="numList">The list to be summed</param>
        /// <returns>The sum of the values the given list.</returns>
        public static double Sum(IList<double> numList)
        {
            double sum = 0;

            foreach (double num in numList)
            {
                sum += num;
            }

            return sum;
        }

        /// <summary>
        /// Gets the binary scale of the given number. In other words
        /// this method determines whether the give number is in the 
        /// order of Kilo, Mega or Giga bytes.
        /// </summary>
        /// <param name="num">The number whose scale is to be determined
        /// </param>
        /// <returns>The binary scale of the number</returns>
        public static long GetNumberScale(double num)
        {
            long order = 1;

            if ((num / (ReportingUtil.OneGigaByte)) > 1)
            {
                order = ReportingUtil.OneGigaByte;
            }
            else if ((num / (ReportingUtil.OneMegaByte)) > 1)
            {
                order = ReportingUtil.OneMegaByte;
            }
            else if ((num / ReportingUtil.OneKiloByte) > 1)
            {
                order = ReportingUtil.OneKiloByte;
            }
            else
            {
                order = ReportingUtil.OneByte;
            }

            return order;
        }

        #endregion Numerical Helpers

        #region Display Helpers

        /// <summary>
        /// Returns a suitable 'WorldMax' for display in charts. Setting
        /// the maxmum ordinate data value as WorldMax leads to a chart 
        /// where the peak falls on the top end of the chart. This method
        /// returns a WorldMax value that is 10% larger, resulting in the 
        /// peak falling just below the top end of the chart.
        /// </summary>
        /// <param name="max">The WorldMax value</param>
        /// <returns>A value that is 10% larger than WorldMax</returns>
        public static double GetDisplayableWorldMax(double max)
        {
            if (max == 0)
            {
                return 100;
            }
            return max + ((10.0 / 100) * max);
        }


        /// <summary>
        /// Appends the appropriate binary prefix name to the unit 
        /// 'bytes' depending on the sale of the number specfied by
        /// the parameter <paramref name="order"/>.
        /// </summary>
        /// <param name="order">The number for which the appropriate
        /// units is desired. Note that this method checks for exact
        /// sizes equal to a kilo, mega or giga byte</param>
        /// <returns>The appropriate binary prefix appended to 'bytes'
        /// </returns>
        public static string GetNumberScaleName(double order)
        {
            string name = null;

            if (order == ReportingUtil.OneKiloByte)
            {
                name = "Kilobytes";
            }
            else if (order == ReportingUtil.OneMegaByte)
            {
                name = "Megabytes";
            }
            else if (order == ReportingUtil.OneGigaByte)
            {
                name = "Gigabytes";
            }

            return name;
        }

        /// <summary>
        /// Returns and appropriate number-format for <see cref="T:NPlot.DateTimeAxis"/>.
        /// The number format is determined by the given date range and is optimized for
        /// display. 
        /// </summary>
        /// <param name="fromDate">The starting <see cref="T:System.DateTime"/> of the
        /// date range</param>
        /// <param name="toDate">The ending <see cref="T:System.DateTime"/> of the date
        /// range</param>
        /// <returns>A string representation of the number format.</returns>
        public static string GetDateTimeFormat(DateTime fromDate, DateTime toDate)
        {
            TimeSpan timeSpan = toDate.Subtract(fromDate);

            if (timeSpan.Days >= 1)
            {
                return "MM-dd HH:mm";
            }
            else
            {
                return "HH:mm";
            }
        }
        #endregion Display Helpers
    }
}
