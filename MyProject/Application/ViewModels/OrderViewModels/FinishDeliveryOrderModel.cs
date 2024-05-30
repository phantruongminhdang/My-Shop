using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.ViewModels.OrderViewModels
{
    public class FinishDeliveryOrderModel
    {
        public List<IFormFile>? Image { get; set; } = default!;
    }
}
