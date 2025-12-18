using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NSH.Models
{
    public class DashboardViewModel
    {
        public List<RequestModel> MyRequests { get; set; }
        public List<RequestModel> AcceptedRequests { get; set; }
    }
}