using log4net;
using Symposium.Helpers;
using Symposium.Helpers.Interfaces;
using Symposium.Models.Enums;
using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.Models.Models.ExternalDelivery;
using Symposium.WebApi.DataAccess.Interfaces.DT;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.ExternalDelivery;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_ClientJobsTasks : IDA_ClientJobsTasks
    {
        protected ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        IDA_ClientJobsDT dt;
        IGuestDT guestDB;
        ICustomJsonSerializers cjson;
        IForkeyTasks forkyTask;
        IAccountsDT accDT;
        IInvoiceTypesDT invTypeDT;
        IPosInfoDT posInfoDT;
        IPosInfoDetailDT posInfoDetDT;
        IStaffDT staffDT;
        ISalesTypeDT salesDT;
        IVatDT vatDT;
        IOrderDetailsDT orderDetailsDT;
        IInvoicesDT invoiceDT;
        ITransactionsTasks transactionTasks;
        IOrderDT OrderDT;
        IOrderDetailInvoicesTasks orderDetInvTask;
        IDepartmentDT departmentDT;
        IDelivery_CustomersShippingAddressDT delivShipDT;
        IDeliveryCustomersDT delivCustDT;

        public DA_ClientJobsTasks(IDA_ClientJobsDT dt, IGuestDT guestDB, ICustomJsonSerializers cjson,
            IForkeyTasks forkyTask, IAccountsDT accDT, IInvoiceTypesDT invTypeDT, IPosInfoDT posInfoDt,
            IPosInfoDetailDT posInfoDetDT, IStaffDT staffDT, ISalesTypeDT salesDT, IVatDT vatDT,
            IOrderDetailsDT orderDetailsDT, IInvoicesDT invoiceDT, ITransactionsTasks transactionTasks,
            IOrderDT OrderDT, IOrderDetailInvoicesTasks orderDetInvTask, IDepartmentDT departmentDT,
            IDelivery_CustomersShippingAddressDT delivShipDT, IDeliveryCustomersDT delivCustDT)
        {
            this.dt = dt;
            this.guestDB = guestDB;
            this.cjson = cjson;
            this.forkyTask = forkyTask;
            this.accDT = accDT;
            this.invTypeDT = invTypeDT;
            this.posInfoDT = posInfoDt;
            this.posInfoDetDT = posInfoDetDT;
            this.staffDT = staffDT;
            this.salesDT = salesDT;
            this.vatDT = vatDT;
            this.orderDetailsDT = orderDetailsDT;
            this.invoiceDT = invoiceDT;
            this.transactionTasks = transactionTasks;
            this.OrderDT = OrderDT;
            this.orderDetInvTask = orderDetInvTask;
            this.departmentDT = departmentDT;
            this.delivShipDT = delivShipDT;
            this.delivCustDT = delivCustDT;
        }

        /// <summary>
        /// Check's if the order from DA exists and returns last order status.
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="headOrder"></param>
        /// <param name="daStore"></param>
        /// <param name="lastStatus"></param>
        /// <returns></returns>
        public bool CheckIfDA_OrderExists(DBInfoModel dbInfo, DA_OrderModel headOrder, DA_StoreModel daStore, out OrderStatusEnum lastStatus)
        {
            return dt.CheckIfDA_OrderExists(dbInfo, headOrder, daStore, out lastStatus);
        }


        /// <summary>
        /// Checks A FullOrderWithTablesModel before post
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="receipt"></param>
        /// <param name="Order"></param>
        /// <param name="CustomerId"></param>
        /// <param name="extType"></param>
        /// <param name="ModifyOrder"></param>
        /// <param name="Error"></param>
        /// <param name="CheckReciptNo"></param>
        /// <returns></returns>
        public bool ValidateFullOrder(DBInfoModel dbInfo, FullOrderWithTablesModel receipt, DA_OrderModel Order, long? CustomerId, ExternalSystemOrderEnum extType,
            ModifyOrderDetailsEnum ModifyOrder, out string Error, bool CheckReciptNo = true)
        {
            bool ret = true;
            Error = "";
            if (Order.InvoiceType == 7)
            {
                long? custId = delivCustDT.GetCustomerIdByExtId(dbInfo, (CustomerId ?? 0).ToString(), extType);

                if (custId == null || custId == 0)
                {
                    if (ModifyOrder != ModifyOrderDetailsEnum.PayOffOnly)
                    {
                        ret = false;
                        logger.Info("There is no Selected Customer");
                        Error = "There is No Selected Customer!";
                    }
                }
                else if (string.IsNullOrEmpty(receipt.Invoice[0].InvoiceShippings[0].BillingName) || 
                         string.IsNullOrEmpty(receipt.Invoice[0].InvoiceShippings[0].BillingVatNo) || 
                         string.IsNullOrEmpty(receipt.Invoice[0].InvoiceShippings[0].BillingDOY) || 
                         string.IsNullOrEmpty(receipt.Invoice[0].InvoiceShippings[0].BillingJob))
                {
                    if (ModifyOrder != ModifyOrderDetailsEnum.PayOffOnly)
                    {
                        ret = false;
                        logger.Info("Some Invoice Fields are Empty");
                        Error = "Some Invoice Fields are Empty!";

                    }
                }
            }

            if (!ret)
                return ret;

            decimal recTot = 0;
            foreach (OrderDetailWithExtrasModel item in receipt.OrderDetails)
                recTot += item.OrderDetailInvoices.Sum(s => s.Total ?? 0);

            if (receipt.PdaModuleId != null)
            {
                if ((receipt.Discount ?? 0) > 0 && recTot > receipt.Total)
                    receipt.Invoice[0].Transactions.FirstOrDefault().Amount -= receipt.Discount ?? 0;
            }

            switch (ModifyOrder)
            {
                case ModifyOrderDetailsEnum.FromScratch:
                    break;
                case ModifyOrderDetailsEnum.FromOtherUnmodified:
                case ModifyOrderDetailsEnum.FromOtherUpated:
                    List<OrderDetailModel> orderDetails = AutoMapper.Mapper.Map<List<OrderDetailModel>>(receipt.OrderDetails);
                    InvoiceTypeModel invTp = invTypeDT.GetSingleInvoiceType(dbInfo, receipt.Invoice[0].InvoiceTypeId ?? 0);

                    if (invTp.Type != 2 && invTp.Type != 3 && invTp.Type != 8)
                    {
                        if (orderDetails.Any(a => a.PaidStatus > 0))
                        {
                            ret = false;
                            Error = string.Format("Items allready invoiced.Invoice Id {0} ", receipt.Id);
                        }
                    }
                    break;
                case ModifyOrderDetailsEnum.PayOffOnly:
                    InvoiceModel inv = receipt.Invoice[0];
                    if (inv != null)
                    {
                        List<TransactionsModel> transacts = transactionTasks.GetTransactionsByInvoiceId(dbInfo, inv.Id ?? 0);
                        decimal allreadyPaid = transacts.Sum(s => s.Amount);
                        if (recTot + allreadyPaid > inv.Total)
                        {
                            ret = false;
                            Error = string.Format("Payment exceeds invoice total. Invoice Total {0} Current PaidTotal {1}, New PaidTotal {2}",
                                receipt.Total, allreadyPaid, recTot);
                        }
                    }
                    break;
                default: break;
            }
            if (!ret)
                return ret;

            if (CheckReciptNo)
            {
                ret = receipt.ReceiptNo > 0;
                if (!ret)
                    Error = "Receipt Number is less than one(1)";
            }
            return ret;
        }


        /// <summary>
        /// Check Customer and Address and Phones if Exists and Insert's Or Update's Data
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="Customer"></param>
        /// <param name="Addresses"></param>
        /// <param name="OrderType"></param>
        /// <param name="Error"></param>
        /// <param name="guest"></param>
        /// <returns></returns>
        public DeliveryCustomerModel UpsertCustomer(DBInfoModel dbInfo, DACustomerModel Customer, List<DA_AddressModel> Addresses, int OrderType,
            out string Error, ref GuestModel guest)
        {
            return dt.UpsertCustomer(dbInfo, Customer, Addresses, OrderType, out Error, ref guest);
        }

        /// <summary>
        /// Return's an invoice for specific Rxternal type and External Key (Delivery Key)
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="ExternalType"></param>
        /// <param name="ExtKey"></param>
        /// <returns></returns>
        public InvoiceModel GetInvoiceFromDBForDelivery(DBInfoModel Store, ExternalSystemOrderEnum ExternalType, string ExtKey, bool forCancel)
        {
            return dt.GetInvoiceFromDBForDelivery(Store, ExternalType, ExtKey, forCancel);
        }


        /// <summary>
        /// Return's an order from db for specific extrnal key and type
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="ExtType"></param>
        /// <param name="ExtKey"></param>
        /// <returns></returns>
        public OrderModel GetOrderFromDBUsingExternalKey(DBInfoModel dbInfo, ExternalSystemOrderEnum ExtType, string ExtKey)
        {
            return dt.GetOrderFromDBUsingExternalKey(dbInfo, ExtType, ExtKey);
        }

        /// <summary>
        /// Return's Order Status
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        public int GetLastStatusForDeliverOrder(DBInfoModel dbInfo, long OrderId)
        {
            return dt.GetLastStatusForDeliverOrder(dbInfo, OrderId);
        }

        /// <summary>
        /// Get's Invoice Shipping for specific Invoice Id
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="InvoiceId"></param>
        /// <returns></returns>
        public InvoiceShippingDetailsModel GetInvoiceShippingForSpecificInvoice(DBInfoModel Store, long InvoiceId)
        {
            return dt.GetInvoiceShippingForSpecificInvoice(Store, InvoiceId);
        }


        /// <summary>
        /// Return's an order model to send to client to make new order
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="model"></param>
        /// <param name="customers"></param>
        /// <param name="extType"></param>
        /// <returns></returns>
        public DA_NewOrderModel ReturnOrderDetailExternalList(DBInfoModel dbInfo, DA_OrderModel model, List<DASearchCustomerModel> customers, 
            ExternalSystemOrderEnum extType, out string Error)
        {
            return dt.ReturnOrderDetailExternalList(dbInfo, model, customers, extType, out Error);
        }
    }
}
