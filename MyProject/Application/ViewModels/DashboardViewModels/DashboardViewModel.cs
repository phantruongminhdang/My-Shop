using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.ViewModels.DashboardViewModels
{
    public class DashboardViewModel
    {
        public int NewUser { get; set; }
        public int NewOrder { get; set;}
        public double TotalOrderIncome { get; set; }
        public double TotalServiceIncome { get; set; }
        public int CurrentServiceOngoing { get; set; }
        public List<OrderCircleGraph> OrderCircleGraphs { get; set; } = default!;
        public List<ServiceOrderCircleGraph> ServiceOrderCircleGraphs { get; set; } = default!;
    }
    public class OrderCircleGraph
    {
        public string CategoryName { get; set; } = default!;
        public double Percent { get; set; }
    }
    public class ServiceOrderCircleGraph
    {
        public string ServiceName { get; set; } = default!;
        public double Percent { get; set; }
    }
}
