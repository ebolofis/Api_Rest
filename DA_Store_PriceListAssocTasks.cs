using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_Store_PriceListAssocTasks : IDA_Store_PriceListAssocTasks
    {
        IDA_Store_PriceListAssocDT da_Store_PriceListAssocDT;

        public DA_Store_PriceListAssocTasks(IDA_Store_PriceListAssocDT _da_Store_PriceListAssocDT)
        {
            this.da_Store_PriceListAssocDT = _da_Store_PriceListAssocDT;
        }

        /// <summary>
        /// Επιστρέφει όλες τις  pricelist ανα κατάστημα
        /// </summary>
        /// <returns>DAStore_PriceListAssocModel</returns>
        public List<DAStore_PriceListAssocModel> GetDAStore_PriceListAssoc(DBInfoModel dbInfo)
        {
           var psl= da_Store_PriceListAssocDT.GetDAStore_PriceListAssoc(dbInfo);
            if (psl == null) psl = new List<DAStore_PriceListAssocModel>();
            return psl;
        }
    }
}
