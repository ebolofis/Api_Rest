using log4net;
using Newtonsoft.Json;
using Symposium.Helpers;
using Symposium.Helpers.Classes;
using Symposium.Models.Enums;
using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.Models.Models.WebGoodysOrders;
using Symposium.WebApi.DataAccess.Interfaces.DT;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_OrdersTasks : IDA_OrdersTasks
    {
        IDA_OrdersDT ordersDT;
        IDA_LoyaltyDT loyaltyDT;
        IDA_OrderStatusTasks orderStatusTasks;
        IDA_StoresTasks storeTasks;
        IDA_Store_PriceListAssocTasks pricelistAssocTask;
        IDA_CustomerTasks customerTask;
        IDA_ShortagesTasks shortagesTasks;
        IDA_GeoPolygonsTasks geoPolygonsTasks;
        LocalConfigurationHelper configHlp;
        IProductDT productDT;

        protected ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        decimal zero = 0.005m;

        public DA_OrdersTasks(IDA_OrdersDT _ordersDT, 
            IDA_LoyaltyDT _loyaltyDT,
            IDA_OrderStatusTasks _orderStatusTasks, 
            IDA_StoresTasks storeTasks, 
            IDA_Store_PriceListAssocTasks pricelistAssocTask, 
            IDA_CustomerTasks customerTask,
            IDA_ShortagesTasks shortagesTasks,
            IDA_GeoPolygonsTasks geoPolygonsTasks,
            LocalConfigurationHelper configHlp,
            IProductDT productDT)
        {
            this.ordersDT = _ordersDT;
            this.loyaltyDT = _loyaltyDT;
            this.orderStatusTasks = _orderStatusTasks;
            this.storeTasks = storeTasks;
            this.pricelistAssocTask = pricelistAssocTask;
            this.customerTask = customerTask;
            this.shortagesTasks = shortagesTasks;
            this.configHlp = configHlp;
            this.geoPolygonsTasks = geoPolygonsTasks;
            this.productDT = productDT;

        }


        /// <summary>
        /// Return the status of an Order
        /// </summary>
        /// <param name="dbInfo">db</param>
        /// <param name="Id">Order Id</param>
        /// <returns></returns>
        public OrderStatusEnum GetStatus(DBInfoModel dbInfo, long Id)
        {
            configHlp.CheckDeliveryAgent();
            return ordersDT.GetStatus(dbInfo, Id);
        }

        /// <summary>
        /// Get All Orders
        /// </summary>
        /// <returns></returns>
        public List<DA_OrderModelExt> GetAllOrders(DBInfoModel dbInfo)
        {
            return ordersDT.GetAllOrders(dbInfo);
        }

        /// <summary>
        /// Get Orders By Date
        /// </summary>
        /// <returns></returns>
        public List<DA_OrderModelExt> GetOrdersByDate(DBInfoModel dbInfo, string SelectedDate)
        {
            return ordersDT.GetOrdersByDate(dbInfo, SelectedDate);
        }

        /// <summary>
        /// Get Customer Recent Orders
        /// </summary>
        /// <param name="CustomerId"></param>
        /// <param name="top"></param>
        /// param name="historicOnly">return only historic orders (excluded status: Canceled =5,Complete = 6, Returned=7)</param>
        /// <returns>Επιστρέφει τις τελευταίες παραγγελίες ενός πελάτη</returns>
        public List<DA_OrderModel> GetOrders(DBInfoModel dbInfo, long id, int top, GetOrdersFilterEnum filter = GetOrdersFilterEnum.All)
        {
            if (top <= 0) top = 5;
            configHlp.CheckDeliveryAgent();
            return ordersDT.GetOrders(dbInfo, id, top, filter);
        }

        /// <summary>
        /// Get A Specific Order
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>Order + details + ShippingAddress</returns>
        public DA_ExtOrderModel GetOrderById(DBInfoModel dbInfo, long Id)
        {
            configHlp.CheckDeliveryAgent();
            return ordersDT.GetOrderById(dbInfo, Id);
        }

        /// <summary>
        /// Get A Specific Order based on ExtId1 (Efood order id). Return DA_Orders.Id. 
        /// If ExtId1 not found return 0;
        /// </summary>
        /// <param name="Store">db</param>
        /// <param name="ExtId1">Efood order id</param>
        /// <returns>Order id</returns>
        public long GetOrderByExtId1(DBInfoModel dbInfo, string ExtId1)
        {
            configHlp.CheckDeliveryAgent();
            return ordersDT.GetOrderByExtId1(dbInfo, ExtId1);
        }
        

        /// <summary>
        /// Get a specific order
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="orderId"></param>
        /// <returns>Order without details</returns>
        public DA_OrderModel GetSingleOrderById(DBInfoModel dbInfo, long orderId)
        {
            return ordersDT.GetSingleOrderById(dbInfo, orderId);
        }

        /// <summary>
        /// Update payment id and possibly status of order. Insert new order status in DA_OrderStatus
        /// </summary>
        /// <param name="dbInfo"></param>
        /// <param name="order"></param>
        /// <param name="model"></param>
        /// <returns>order id</returns>
        public long UpdatePaymentIdAndStatus(DBInfoModel dbInfo, DA_OrderModel order, ExternalPaymentModel model)
        {
            long orderId = 0;
            OrderStatusEnum firstOrderStatus = order.Status;
            DateTime now = DateTime.Now;
            order.PaymentId = model.PaymentId;
            order.IsSend++;
            if (firstOrderStatus == OrderStatusEnum.OnHold)
            {
                order.Status = OrderStatusEnum.Received;
                order.StatusChange = now;
            }
            orderId = ordersDT.UpdateSingleOrder(dbInfo, order);
            logger.Info($"ORDER UPDATED.  OrderId : {order.Id.ToString()}, ExtId1:{order.ExtId1 ?? "<null>"}, Customer: {order.CustomerId }, ShipAdd: {order.ShippingAddressId},  Origin: {order.Origin}, Type: {order.OrderType}, Status: {order.Status}, Store: {order.StoreId},IsDelay: {order.IsDelay}, Total: {order.Total}");

            if (firstOrderStatus == OrderStatusEnum.OnHold)
            {
                DA_OrderStatusModel orderStatus = new DA_OrderStatusModel();
                orderStatus.OrderDAId = orderId;
                orderStatus.Status = (short) OrderStatusEnum.Received;
                orderStatus.StatusDate = now;
                long orderStatusId = orderStatusTasks.AddNewModel(dbInfo, orderStatus);
            }
            return orderId;
        }

        /// <summary>
        /// Search for Orders
        /// </summary>
        /// <param name="Model">Filter Model</param>
        /// <returns>Επιστρέφει τις παραγγελίες βάση κριτηρίων</returns>
        public List<DA_OrderModel> SearchOrders(DBInfoModel dbInfo, DA_SearchOrdersModel Model)
        {
            configHlp.CheckDeliveryAgent();
            return ordersDT.SearchOrders(dbInfo, Model);
        }

        /// <summary>
        /// Mεταβάλλει το DA_orders. StatusChange και εισάγει νέα εγγραφή στον DA_OrderStatus
        /// </summary>
        /// <param name="Id">Order Id</param>
        /// <returns></returns>
        public long UpdateStatus(DBInfoModel dbInfo, long Id, OrderStatusEnum Status)
        {
            if(Status == OrderStatusEnum.Canceled || Status == OrderStatusEnum.Returned)
            {
                loyaltyDT.DeleteGainPoints(dbInfo, Id,0);
            }
            return ordersDT.UpdateStatus(dbInfo, Id, Status);
        }

        /// <summary>
        /// Get Customer Recent Remarks
        /// </summary>
        /// <param name="CustomerId"></param>
        /// <param name="top"></param>
        /// <returns>Επιστρέφει τις τελευταίες παραγγελίες ενός πελάτη Remarks != null</returns>
        public List<DA_OrderModel> GetRemarks(DBInfoModel dbInfo, long Id, int top)
        {
            return ordersDT.GetRemarks(dbInfo, Id, top);
        }

        /// <summary>
        /// Get Order Status For Specific Order by OrderId
        /// </summary>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        public List<DA_OrderStatusModel> GetOrderStatusTimeChanges(DBInfoModel dbInfo, long OrderId)
        {
            return ordersDT.GetOrderStatusTimeChanges(dbInfo, OrderId);
        }

        /// <summary>
        /// Update Customer Remarks
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        public long UpdateRemarks(DBInfoModel dbInfo, UpdateRemarksModel Model)
        {
            return ordersDT.UpdateRemarks(dbInfo, Model);
        }

        /// <summary>
        /// Add new Order 
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        public long InsertOrder(DBInfoModel dbInfo, DA_OrderModel Model)
        {
            return ordersDT.InsertOrder(dbInfo, Model);
        }

        /// <summary>
        /// Update an Order 
        /// </summary>
        /// <param name="dbInfo">db</param>
        /// <param name="Model">order model</param>
        /// <param name="hasChanges">true if at least one item/extras have changed</param>
        /// <returns></returns>
        public long UpdateOrder(DBInfoModel dbInfo, DA_OrderModel Model, bool hasChanges)
        {
            
            return ordersDT.UpdateOrder(dbInfo, Model, hasChanges);
        }

        /// <summary>
        /// check if prices are correct, otherwise throw exception.
        /// For items:
        ///       Total = Price * Qnt - Discount. (Extra's Price is not included into item's Total)    
        ///       NetAmount = Total/(1+RateVat/100)     
        ///       TotalVat = Total - NetAmount  
        ///  For Extras:
        ///       NetAmount = (item.Qnt * extra.Qnt * extra.Price)/(1+RateVat/100)  
        ///       TotalVat = (item.Qnt * extra.Qnt * extra.Price) - NetAmount
        /// </summary>
        /// <param name="Model">order model</param>
        /// <returns></returns>
        public void CheckPrices( DA_OrderModel Model)
        {
            decimal total = 0 - Model.Discount;
            decimal totalVat = 0;

            foreach (var item in Model.Details)
            {
                total = total + item.Total;
                totalVat = totalVat + item.TotalVat;
                if (Math.Abs(item.Total - ((item.Price * item.Qnt) -item.Discount))>= zero) throw new BusinessException("Wrong Total for item '"+item.Description+ "'. Should be: Total = Price * Qnt - Discount. Extra's Price is not included into item's Total");
                if (Math.Abs(item.NetAmount - (item.Total/(1 + item.RateVat / 100))) >= zero) throw new BusinessException("Wrong NetAmount for item '" + item.Description + "'. Should be: NetAmount = Total/(1+RateVat/100)");
                if (Math.Abs(item.TotalVat - (item.Total - item.NetAmount)) >= zero) throw new BusinessException("Wrong TotalVat for item '" + item.Description + "'. Should be: TotalVat = Total - NetAmount");
                if (item.Discount> Math.Abs(item.Price * item.Qnt)) throw new BusinessException("Discount should be smaller than Price * Quantity for item '"+item.Description+"'");
                if (item.Extras != null)
                {
                    foreach (var extra in item.Extras)
                    {
                        decimal ttl = item.Qnt * extra.Qnt * extra.Price;
                        decimal netAmount = ttl / (1 + extra.RateVat / 100);
                        decimal extraTotalVat = ttl - netAmount;
                        total = total + ttl;
                        if (Math.Abs(extra.NetAmount - netAmount) >= zero) throw new BusinessException("Wrong NetAmount for extra '" + extra.Description + "'. Should be: NetAmount = (item.Qnt * extra.Qnt * extra.Price)/(1+RateVat/100)");
                        if (Math.Abs(extra.TotalVat - extraTotalVat) >= zero) throw new BusinessException("Wrong TotalVat for extra '" + extra.Description + "'. Should be: TotalVat = (item.Qnt * extra.Qnt * extra.Price) - NetAmount");
                    }
                }
            }
            if (Math.Abs(Model.Total - total)>= zero)
            {
                if(Model.Origin==3)//e-food, set as Total the sum of products...
                {
                   // Model.LogicErrors = Model.LogicErrors + " - " + string.Format(Symposium.Resources.Errors.WRONGDATOTALEFOOD, total.ToString("C"), Model.Total.ToString("C"));
                    Model.Total = total;
                    Model.Price = Model.Total + Model.Discount;
                }
                else
                    throw new BusinessException(string.Format(Symposium.Resources.Errors.WRONGDATOTAL, total.ToString("C"), Model.Total.ToString("C")));
            }
           
            if (Math.Abs(Model.Total - (Model.Price-Model.Discount))>=zero) throw new BusinessException("Wrong Total. Should be: Total = Price - Discount");

        }

        /// <summary>
        /// Check shortages for the order. If a product is in shortage then throw exception.
        /// </summary>
        /// <param name="dbInfo">db</param>
        /// <param name="Model">Order</param>
        public void CheckShoratges(DBInfoModel dbInfo,DA_OrderModel Model)
        {
            if (Model.IgnoreShortages) return;
            List<DA_ShortagesExtModel> shortages = shortagesTasks.GetShortagesByStore(dbInfo, Model.StoreId);
            foreach(var item in Model.Details)
            {
                if (shortages.FirstOrDefault(x => x.ProductId == item.ProductId)!=null)
                {
                    throw new BusinessException("Product '" + item.Description + "' is not available (Check shop's Shortages)");
                }
            }
        }


        /// <summary>
        /// various validations for a DA-order  
        /// </summary>
        /// <param name="Model">order model</param>
        /// <returns></returns>
        public void OrderValidations(DBInfoModel dbInfo, DA_OrderModel Model)
        {
          
            if(Model.AccountType!=1 && Model.AccountType != 4 && Model.AccountType != 6)
                throw new BusinessException("AccountType only accepts values: cash=1, credit/debit card=4, voucher=6");

            if (Model.InvoiceType != 1 && Model.InvoiceType != 7)
                throw new BusinessException("InvoiceType only accepts values: receipt=1, invoice =7");

            if (Model.OrderType != OrderTypeStatus.Delivery && Model.OrderType != OrderTypeStatus.TakeOut && Model.OrderType != OrderTypeStatus.DineIn)
                throw new BusinessException("OrderType only accepts values: delivery=20, takeout=21, dinein=22");

            if (Model.Origin <0 ||  Model.Origin > 7)
                throw new BusinessException("Origin only accepts values 0-7");


            if (Model.InvoiceType == 7)//timologio
            {
                if(Model.BillingAddressId == null || Model.BillingAddressId <= 0)
                    throw new BusinessException("No billing Address is provided");

                DACustomerModel customer = customerTask.GetCustomer(dbInfo, Model.CustomerId);
                if(customer.BillingAddressesId != Model.BillingAddressId)
                    throw new BusinessException("Invalid BillingAddressId (customer.BillingAddressesId != Order.BillingAddressId)");

                if (string.IsNullOrWhiteSpace(customer.VatNo))
                    throw new BusinessException("Empty VatNo (ΑΦΜ)");

                if (string.IsNullOrWhiteSpace(customer.Doy))
                    throw new BusinessException("Empty ΔΟΥ");

                if (string.IsNullOrWhiteSpace(customer.JobName))
                    throw new BusinessException("Empty JobName");

                if (string.IsNullOrWhiteSpace(customer.PhoneComp))
                    throw new BusinessException("Empty Company Phone");

                if (string.IsNullOrWhiteSpace(customer.Proffesion))
                    throw new BusinessException("Empty Profession");
            }

            if (Model.OrderType == OrderTypeStatus.DineIn)
            {
                if (Model.StoreId == 0)
                    throw new BusinessException("Empty StoreId for DineIn");

                if (Model.TableId == null || Model.TableId == 0)
                    throw new BusinessException("Empty TableId for DineIn");
            }

        }

        /// <summary>
        /// check if store has the proper status and set the proper value to isDelay (χρονοκαθηστέριση)
        /// </summary>
        /// <param name="dbInfo">db</param>
        /// <param name="Model">order model</param>
        public void CheckStoreAvailabilityDelay(DBInfoModel dbInfo,DA_OrderModel Model)
        {
          DA_StoreModel storeModel= storeTasks.GetStoreById(dbInfo, Model.StoreId);
          if (storeModel == null) throw new BusinessException(string.Format(Symposium.Resources.Errors.STOREIDAUTHFAILDED,(Model.StoreId.ToString()??"<null>")));
          if(storeModel.StoreStatus==DAStoreStatusEnum.Closed) throw new BusinessException(Symposium.Resources.Errors.STORECLOSE);
          if (Model.OrderType == OrderTypeStatus.Delivery && storeModel.StoreStatus == DAStoreStatusEnum.TakeoutOnly) throw new BusinessException(Symposium.Resources.Errors.STORENODELIVERY);//no delivery
          if (Model.OrderType == OrderTypeStatus.TakeOut && storeModel.StoreStatus == DAStoreStatusEnum.DeliveryOnly) throw new BusinessException(Symposium.Resources.Errors.STORENOTAKEOUT);//no take out

            if (Model.OrderType == OrderTypeStatus.Delivery)//delivery
            {
                if ((Model.EstBillingDate ?? DateTime.Now).Subtract(Model.OrderDate).TotalMinutes > storeModel.DeliveryTime+1)
                    Model.IsDelay = true;
                else
                    Model.IsDelay = false;
            }

            if (Model.OrderType == OrderTypeStatus.TakeOut)//takeout
            {
                if ((Model.EstTakeoutDate ?? DateTime.Now).Subtract(Model.OrderDate).TotalMinutes > storeModel.TakeOutTime + 1)
                    Model.IsDelay = true;
                else
                    Model.IsDelay = false;
            }

        }

        /// <summary>
        /// check if the correct price-lists are chosen
        /// </summary>
        /// <param name="dbInfo">db</param>
        /// <param name="Model">order model</param>
        public void CheckPricelist(DBInfoModel dbInfo, DA_OrderModel Model)
        {

            if (Model.Origin == 3) return; // for e-food order do not check price-list (someone else have already done).

            List<DAStore_PriceListAssocModel> storePricelists= pricelistAssocTask.GetDAStore_PriceListAssoc(dbInfo).FindAll(x=>x.DAStoreId==Model.StoreId);
            if(storePricelists==null || storePricelists.Count==0) throw new BusinessException(Symposium.Resources.Errors.PRICELISTSTORENOFOUND);
            List<DAStore_PriceListAssocModel> pl=null;
            if (Model.OrderType == OrderTypeStatus.Delivery)//delivery
               pl= storePricelists.FindAll(x => x.PriceListType == DAPriceListTypes.ForDelivery);
            if (Model.OrderType == OrderTypeStatus.TakeOut)//takeout
                pl = storePricelists.FindAll(x => x.PriceListType == DAPriceListTypes.ForTakeOut);
            if (Model.OrderType == OrderTypeStatus.DineIn)//dinein
                pl = storePricelists.FindAll(x => x.PriceListType == DAPriceListTypes.ForDineIn);

            if (pl==null) pl = new List<DAStore_PriceListAssocModel>();

            foreach (var item in Model.Details)
            {
              if(pl.FirstOrDefault(x=>x.PriceListId==item.PriceListId)==null)
                throw new BusinessException(string.Format(Symposium.Resources.Errors.INVALIDPRICELISTFORITEM, item.Description));
            }
        }


        /// <summary>
        /// Check store and polygon validity
        /// </summary>
        /// <param name="Model">DA_OrderModel</param>
        public void CheckStorePolygon(DBInfoModel dbInfo, DA_OrderModel Model)
        {
            CheckStoreIdCode(Model);
            if(Model.StoreId==0)  Model.StoreId = (long) storeTasks.GetStoreIdFromCode(dbInfo, Model.StoreCode);

            if (!Model.CheckShippingAddress)
            {
                Model.GeoPolygonId = 0;
                logger.Warn($" Skipped store polygon check. Store Id from client: {Model.StoreId} ");
                return;
            }
            if (Model.OrderType == OrderTypeStatus.TakeOut || Model.OrderType == OrderTypeStatus.DineIn)
            {
                Model.GeoPolygonId = 0;
                logger.Warn($" Skipped store polygon check. Order was take out or dine in. Store Id from client: {Model.StoreId} ");
                return;
            }

            if (Model.ShippingAddressId > 0 && Model.ShippingAddressId!=null)
            {
                DA_GeoPolygonsBasicModel polygon = geoPolygonsTasks.SelectPolygonByAddressId(dbInfo, Model.ShippingAddressId??0);
                Model.GeoPolygonId = polygon.Id; // <--- SET the POLYGON ID --<<<<

                if (polygon.StoreId < 0 && Model.OrderType == OrderTypeStatus.Delivery)//delivery with inactive polygon
                {
                    logger.Error($"Polygon {polygon.Id} is inactive for address id {Model.ShippingAddressId}. Store Id: {-polygon.StoreId}");
                    throw new BusinessException(Symposium.Resources.Errors.INACTIVEPOLYGON);
                }
                if (polygon.StoreId == 0) //store not found
                {
                    logger.Error($">>--> No Store found for address with id {Model.ShippingAddressId}");
                    throw new BusinessException(Symposium.Resources.Errors.STORENOTFOUND);
                }
                if (polygon.StoreId != Model.StoreId) //Wrong StoreId from client
                {
                    if (Model.OrderType == OrderTypeStatus.Delivery)//delivery
                    {
                        logger.Error($" Client send Wrong StoreId. Store Id from client: {Model.StoreId}, Calculated Store Id: {polygon.StoreId}.");
                        throw new BusinessException(Symposium.Resources.Errors.WRONGSTOREID);
                    }
                    else //takeout
                    {
                        Model.GeoPolygonId = 0;
                        logger.Warn($" Client send wrong storeId. Store Id from client: {Model.StoreId}, Calculated Store Id: {polygon.StoreId} (Take out order). ");
                    }
                }
            }
            if ((Model.ShippingAddressId <= 0 || Model.ShippingAddressId==null) && Model.OrderType == OrderTypeStatus.Delivery)
            {
                throw new BusinessException("For delivery orders Address is required.");
            }
        }


        /// <summary>
        /// Ακύρωση παραγγελίας από όλους εκτός από το κατάστημα. 
        /// </summary>
        /// <param name="Id">Order Id</param>
        /// <returns></returns>
        public long CancelOrder(DBInfoModel dbInfo, long Id, OrderStatusEnum[] cancelStasus, bool isSend = true)
        {
            long k= ordersDT.CancelOrder(dbInfo, Id, cancelStasus, isSend);
            loyaltyDT.DeleteGainPoints(dbInfo, Id,0);
            return k;
        }

        /// <summary>
        /// Ακύρωση παραγγελίας από το κατάστημα MONO.  
        /// </summary>
        /// <param name="Id">Order Id</param>
        /// <param name="StoreId"></param>
        /// <returns></returns>
        public long StoreCancelOrder(DBInfoModel dbInfo, long Id, long StoreId, OrderStatusEnum[] cancelStasus)
        {
            long k = ordersDT.StoreCancelOrder(dbInfo, Id, StoreId, cancelStasus);
            loyaltyDT.DeleteGainPoints(dbInfo, Id,0);
            return k;
        }

        /// <summary>
        /// We Check if we have changes in any of the items or extras of an order
        /// </summary>
        /// <param name="Model"></param>
        /// <returns>True or False</returns>
        public bool CheckOrderItemsForChanges(DBInfoModel dbInfo, DA_OrderModel Model)
        {
            bool hasChanges = false;
            DA_ExtOrderModel oldModel = new DA_ExtOrderModel();
            oldModel = ordersDT.GetOrderById(dbInfo, Model.Id);

            hasChanges = CompareOrderDetails(oldModel.OrderModel.Details, Model.Details);
            if (!hasChanges)
            {
                hasChanges = CompareOrderDetails(Model.Details, oldModel.OrderModel.Details);
            }

            return hasChanges;
        }

        private bool CompareOrderDetails(List<DA_OrderDetails> listA, List<DA_OrderDetails> listB)
        {
            foreach (DA_OrderDetails detail in listB)
            {
                var checkDetails = listA.Find(
                    f => f.DAOrderId == detail.DAOrderId && f.ProductId == detail.ProductId && f.Description == detail.Description &&
                         f.PriceListId == detail.PriceListId && f.Price == detail.Price && f.Qnt == detail.Qnt && f.Discount == detail.Discount && f.Total == detail.Total &&
                         f.TotalVat == detail.TotalVat && f.RateVat == detail.RateVat && f.RateTax == detail.RateTax && f.TotalTax == detail.TotalTax && f.NetAmount == detail.NetAmount &&
                         f.Id == detail.Id && f.ItemRemark == detail.ItemRemark
                    );
                if (checkDetails == null)
                {
                   return true;
                }

                if (checkDetails.Extras == null) checkDetails.Extras = new List<DA_OrderDetailsExtrasModel>();

                if (detail.Extras != null)
                {
                    foreach (DA_OrderDetailsExtrasModel extra in detail.Extras)
                    {
                        var checkExtras = checkDetails.Extras.Find(
                            f => f.OrderDetailId == extra.OrderDetailId && f.ExtrasId == extra.ExtrasId && f.Description == extra.Description && f.Qnt == extra.Qnt &&
                                 f.Price == extra.Price && f.TotalVat == extra.TotalVat && f.RateVat == extra.RateVat && f.TotalTax == extra.TotalTax && f.RateTax == extra.RateTax &&
                                 f.NetAmount == extra.NetAmount && f.Id == extra.Id
                            );
                        if (checkExtras == null)
                        {
                            return true;
                        }

                    }

                }
            }
            return false;
        }

        /// <summary>
        /// Επιλογή Των Order Status που επιτρέπεται η ακύρωση Παραγγελίας.
        /// </summary>
        /// <returns>List of Status</returns>
        public List<int> StatusForCancel(DBInfoModel dbInfo, int[] cancelStasus)
        {
            List<int> status = new List<int>();
            foreach(int s in cancelStasus)
            {
                status.Add(s);
            }
            return status;
        }

        /// <summary>
        /// return the number of orders in DB for a specific store  
        /// </summary>
        /// <param name="dbInfo">connection string</param>
        /// <param name="StoreId">store id</param>
        /// <returns></returns>
        public int GetStoreOrderNo(DBInfoModel dbInfo, long StoreId)
        {
            return ordersDT.GetStoreOrderNo(dbInfo,StoreId);
        }

        /// <summary>
        /// Delete's an DA_Order Record
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="DAOrderId"></param>
        /// <param name="ErrorMess"></param>
        /// <returns></returns>
        public bool DeleteOrders(DBInfoModel Store, long DAOrderId, out string ErrorMess)
        {
            return ordersDT.DeleteOrders(Store, DAOrderId, out ErrorMess);
        }

        /// <summary>
        /// Update DAOrders Set Error Message
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="DAOrderId"></param>
        /// <param name="ErrorMess"></param>
        public void SetErrorMessageToDAOrder(DBInfoModel Store, long DAOrderId, string ErrorMess)
        {
            ordersDT.SetErrorMessageToDAOrder(Store, DAOrderId, ErrorMess);
        }

        /// <summary>
        /// Post Web Goodys Orders model from ATCOM/OMNIREST
        /// </summary>
        /// <returns></returns>
        public bool PostWebGoodysOrder(DBInfoModel dbInfo, WebGoodysOrdersModel Model)
        {
            try
            {
                string ModelToJson = JsonConvert.SerializeObject(Model);
                logger.Info("Post WebGoodysOrder Json :" + ModelToJson);
            }
            catch(Exception ex)
            {
                logger.Error("Post WebGoodysOrder Json ERROR" + ex.ToString());
                return false;
            }
            return true;
        }


        /// <summary>
        /// At least one of Store Id and Store Code should have value. Otherwise throw exception.
        /// </summary>
        /// <param name="model"></param>
        public void CheckStoreIdCode(DA_OrderModel model)
        {
            if (model.StoreId == 0 && String.IsNullOrWhiteSpace(model.StoreCode))
                throw new BusinessException("At least one of StoreId and StoreCode should have value.");
        }

        /// <summary>
        /// find Ids for Product Codes and Extras Codes
        /// </summary>
        /// <param name="dbInfo">db</param>
        /// <param name="Model">DA_OrderModel</param>
        public void MatchIdFromCode(DBInfoModel dbInfo, DA_OrderModel Model)
        {
            foreach(var item in Model.Details)
            {
                if (item.ProductId == 0 && String.IsNullOrWhiteSpace(item.ProductCode))
                    throw new BusinessException("Product Code or Product Id should have value");

                if (item.Extras == null) item.Extras = new List<DA_OrderDetailsExtrasModel>();

                if (item.ProductId == 0 && !String.IsNullOrWhiteSpace(item.ProductCode))
                {
                    ProductExtModel productDb = productDT.GetProductExt(dbInfo, item.ProductCode);
                    if (productDb == null) throw new BusinessException($"Product with Code {item.ProductCode} not found");
                    item.ProductId = productDb.Id;
                    foreach (var extras in item.Extras)
                    {
                        if (String.IsNullOrWhiteSpace(extras.ExtrasCode)) throw new BusinessException($"Extra with empty code into Product with Code {item.ProductCode}");
                        long? id = productDb.Extras.Where(x => x.Code == extras.ExtrasCode).Select(x => x.Id).FirstOrDefault();
                        if (id == null || id == 0) throw new BusinessException($"Extras with code {extras.ExtrasCode} not found for Product with Code {item.ProductCode}.");
                        extras.ExtrasId = (long)id;
                    }
                }
                else
                {
                    foreach (var extras in item.Extras)
                    {
                        if (extras.ExtrasId == 0 && String.IsNullOrWhiteSpace(extras.ExtrasCode)) throw new BusinessException($"Extra with empty code and id into Product with Id {item.ProductId}");
                    }
                }
                    
            }
        }

        /// <summary>
        /// Returns an order from orderno
        /// </summary>
        /// <param name="Store"></param>
        /// <param name="DAOrderNo"></param>
        /// <returns></returns>
        public DA_OrderModel GetOrderByOrderNo(DBInfoModel dbInfo, long DAOrderNo)
        {
            return ordersDT.GetOrderByOrderNo(dbInfo, DAOrderNo);
        }

        /// <summary>
        /// Update's ExtId1 with Omnirest External system OrderNo 
        /// </summary>
        /// <param name="OrderNo"></param>
        /// <returns></returns>
        public bool UpdateDA_OrderExtId1(DBInfoModel dbInfo, long DA_OrderId, long OrderNo)
        {
            return ordersDT.UpdateDA_OrderExtId1(dbInfo, DA_OrderId, OrderNo);
        }
    }
}
