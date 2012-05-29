using System;
using System.Collections.Generic;
using System.Text;

namespace RobChartier.WMISystem {
    public class Query {
        public static System.Collections.Generic.List<System.Management.ManagementObject> PerformQuery(string Query) {
            System.Collections.Generic.List<System.Management.ManagementObject> list = new List<System.Management.ManagementObject>();
            try {
                System.Management.ManagementObjectSearcher searcher;
                System.Management.ObjectQuery query = new System.Management.ObjectQuery(Query);                
                searcher = new System.Management.ManagementObjectSearcher(query);
                foreach (System.Management.ManagementObject obj in searcher.Get()) {
                    list.Add(obj);
                }
            } catch (Exception) { }
            return list;
        }
    }
}