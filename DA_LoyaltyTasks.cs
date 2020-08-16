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
    public class DA_LoyaltyTasks : IDA_LoyaltyTasks
    {
        IDA_LoyaltyDT loyaltyDT;

        public DA_LoyaltyTasks(IDA_LoyaltyDT _loyaltyDT)
        {
            this.loyaltyDT = _loyaltyDT;
        }

        /// <summary>
        /// Get Loyalty Configuration Tables
        /// </summary>
        /// <returns>Επιστρέφει τα περιεχόμενα των πινάκων DA_Loyalty  εκτός του DA_LoyalPoints</returns>
        public DA_LoyaltyFullConfigModel GetLoyaltyConfig(DBInfoModel dbInfo)
        {
            return loyaltyDT.GetLoyaltyConfig(dbInfo);
        }

        /// <summary>
        /// Set Loyalty Configuration Tables
        /// </summary>
        /// <param name="Model">DA_LoyaltyFullConfigModel</param>
        /// <returns></returns>
        public long SetLoyaltyConfig(DBInfoModel dbInfo, DA_LoyaltyFullConfigModel Model)
        {
            return loyaltyDT.SetLoyaltyConfig(dbInfo, Model);
        }

        /// <summary>
        /// Insert Loyalty Gain Amount Range Model
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        public long InsertGainAmountRange(DBInfoModel dbInfo, DA_LoyalGainAmountRangeModel Model)
        {
            return loyaltyDT.InsertGainAmountRange(dbInfo, Model);
        }

        /// <summary>
        /// Delete Loyalty Gain Points Range Row By Id 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public long DeleteRangeRow(DBInfoModel dbInfo, long Id)
        {
            return loyaltyDT.DeleteRangeRow(dbInfo, Id);
        }

        /// <summary>
        /// Delte All Loyalty Gain Amount Range
        /// </summary>
        /// <returns></returns>
        public long DeleteGainAmountRange(DBInfoModel dbInfo)
        {
            return loyaltyDT.DeleteGainAmountRange(dbInfo);
        }

        /// <summary>
        /// Insert Redeem Free Product Model
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        public long InsertRedeemFreeProduct(DBInfoModel dbInfo, DA_LoyalRedeemFreeProductModel Model)
        {
            return loyaltyDT.InsertRedeemFreeProduct(dbInfo, Model);
        }

        /// <summary>
        /// Delete Loyalty Redeem Free Product Row By Id 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public long DeleteRedeemFreeProductRow(DBInfoModel dbInfo, long Id)
        {
            return loyaltyDT.DeleteRedeemFreeProductRow(dbInfo, Id);
        }

        /// <summary>
        /// Delte All Redeem Free Product
        /// </summary>
        /// <returns></returns>
        public long DeleteRedeemFreeProduct(DBInfoModel dbInfo)
        {
            return loyaltyDT.DeleteRedeemFreeProduct(dbInfo);
        }

        /// <summary>
        /// Find Total Loyalty Point of a Customer
        /// </summary>
        /// <param name="Id">Customer Id</param>
        /// <returns>Tο σύνολο των πόντων του πελάτη </returns>
        public int GetLoyaltyPoints(DBInfoModel dbInfo, long Id)
        {
            return loyaltyDT.GetLoyaltyPoints(dbInfo, Id);
        }

        /// <summary>
        /// Choose Loyalty Redeem Options
        /// </summary>
        /// <param name="Id">Customer Id</param>
        /// <param name="Amount">Order Total</param>
        /// <returns>Επιστρέφει λίστα με επιλογές  που έχει ο πελάτης(κατά τη διάρκεια της παραγγελίας του) να καταναλώσει τους  πόντους του</returns>
        public DA_LoyaltyRedeemOptionsModel GetLoyaltyRedeemOptions(DBInfoModel dbInfo, long Id, decimal Amount)
        {
            return loyaltyDT.GetLoyaltyRedeemOptions(dbInfo, Id, Amount);
        }

        /// <summary>
        /// υπολογισμός κερδισμένων πόντων σε παραγγελία 
        /// </summary>
        /// <param name="Model">order</param>
        /// <returns>gain points</returns>
        public int CalcPointsFromOrder(DBInfoModel dbInfo, DA_OrderModel Model)
        {
            return loyaltyDT.CalcPointsFromOrder(dbInfo, Model);
        }

        /// <summary>
        /// εισαγωγή Αρχικών πόντων στο table DA_LoyalPoints
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        public long InsertInitPoints(DBInfoModel dbInfo, DACustomerModel Model)
        {
            return loyaltyDT.InsertInitPoints(dbInfo, Model);
        }

        /// <summary>
        /// Validate Κατανάλωσης Πόντων του Πελάτη με Βάση τους πόντους που δίνει ο Client. 
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
       public void CheckRedeemPoints(DBInfoModel dbInfo, DA_OrderModel Model)
        {
             loyaltyDT.CheckRedeemPoints(dbInfo, Model);
        }

        /// <summary>
        /// Διαγραφή κερδισμένων πόντων από table DA_LoyalPoints βάση Customerid
        /// </summary>
        /// <param name="Id">Customer Id</param>
        /// <returns></returns>
        public long DeleteCustomerGainPoints(DBInfoModel dbInfo, long Id)
        {
            return loyaltyDT.DeleteCustomerGainPoints(dbInfo, Id);
        }

        /// <summary>
        /// Διαγραφή κερδισμένων πόντων από table DA_LoyalPoints βάση orderid
        /// </summary>
        /// <param name="Id">Order Id</param>
        /// <param name="StoreId">Id Καταστήματος (αν η κίνηση ΔΕΝ συσχετίζεται με παραγγελία που έγινε σε κατάστημα τότε StoreId=0) </param>
        /// <returns></returns>
        public long DeleteGainPoints(DBInfoModel dbInfo, long Id, long StoreId)
        {
            return loyaltyDT.DeleteGainPoints(dbInfo, Id, StoreId);
        }

        /// <summary>
        /// Διαγραφή πόντων από table DA_LoyalPoints βάση παλαιότητας
        /// </summary>
        /// <returns></returns>
        public long DeletePoints(DBInfoModel dbInfo)
        {
            return loyaltyDT.DeletePoints(dbInfo);
        }


        /// <summary>
        /// Add loyalty points (gained/redeemed) to tables DA_LoyalPoints and DA_Orders
        /// </summary>
        ///  <param name="dbInfo">dbInfo</param>
        /// <param name="OrderId"></param>
        /// <param name="CustomerId"></param>
        /// <param name="Points"> gain/redeem points </param>
        /// <param name="type">1= gain points. 2= redeem points </param>
        /// <param name="StoreId">Id Καταστήματος (αν η κίνηση ΔΕΝ συσχετίζεται με παραγγελία που έγινε σε κατάστημα τότε StoreId=0) </param>
        public void AddPoints(DBInfoModel dbInfo, long OrderId, long CustomerId, int Points, DateTime Date, int type, long StoreId)
        {
            loyaltyDT.AddPoints(dbInfo, OrderId, CustomerId, Points, Date, type, StoreId);
        }


        public void SavePointsFromLoyaltyAdmin(DBInfoModel DBInfo, DA_LoyalPointsModels model)
        {
            loyaltyDT.SavePointsFromLoyaltyAdmin(DBInfo, model);
        }
    }
}
