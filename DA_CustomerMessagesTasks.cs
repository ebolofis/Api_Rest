using log4net;
using Symposium.Models.Models;
using Symposium.Models.Models.DeliveryAgent;
using Symposium.Models.Models.TableReservations;
using Symposium.WebApi.DataAccess.Interfaces.DT.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.DeliveryAgent;
using Symposium.WebApi.MainLogic.Interfaces.Tasks.TableReservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symposium.WebApi.MainLogic.Tasks.DeliveryAgent
{
    public class DA_CustomerMessagesTasks : IDA_CustomerMessagesTasks
    {
        IDA_CustomerMessagesDT MessagesDT;
        IDA_OrdersDT ordersDT;
        IDA_CustomerDT customersDT;
        IDA_AddressesDT addressesDT;
        IEmailConfigTasks emailTasks;
        protected ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public DA_CustomerMessagesTasks(IDA_CustomerMessagesDT msgsdt, IDA_OrdersDT _ordersDT, IDA_CustomerDT _customersDT, IDA_AddressesDT _addressesDT, IEmailConfigTasks _emailTasks)
        {
            this.MessagesDT = msgsdt;
            this.ordersDT = _ordersDT;
            this.customersDT = _customersDT;
            this.addressesDT = _addressesDT;
            this.emailTasks = _emailTasks;
        }

        public List<DA_MainMessagesModel> GetAll(DBInfoModel dbInfo)
        {
            return MessagesDT.GetAll(dbInfo);
        }

        public List<DA_MessagesModel> Get_DA_MessageById(DBInfoModel DBInfo, long MessageId, long HeaderDetailId)
        {
            return MessagesDT.Get_DA_MessageById(DBInfo, MessageId, HeaderDetailId);
        }
        public List<DA_MessagesModel> GetMessageByMainMessageId(DBInfoModel DBInfo, long id)
        {
            return MessagesDT.GetMessageByMainMessageId(DBInfo, id);
        }
        public List<DA_MessagesDetailsModel> GetMessageDetailsByMainMessageId(DBInfoModel DBInfo, long id)
        {
            return MessagesDT.GetMessageDetailsByMainMessageId(DBInfo, id);
        }

        public List<MessagesLookup> GetMessagesLookups(DBInfoModel DBInfo, long MainDAMessagesID)
        {
            return MessagesDT.GetMessagesLookups(DBInfo, MainDAMessagesID);
        }
        public List<MessagesDetailLookup> GetMessagesDetailsLookups(DBInfoModel DBInfo, long HeaderId)
        {
            return MessagesDT.GetMessagesDetailsLookups(DBInfo, HeaderId);
        }

        public List<DA_CustomerMessagesModelExt> GetAll_DA_CustomerMessages(DBInfoModel DBInfo, long CustomerId)
        {
            return MessagesDT.GetAll_DA_CustomerMessages(DBInfo, CustomerId);
        }
        
        public long InsertMainMessage(DBInfoModel DBInfo, DA_MainMessagesModel Model)
        {
            return MessagesDT.InsertMainMessage(DBInfo, Model);
        }
        public long InsertMessage(DBInfoModel DBInfo, DA_MessagesModel Model)
        {
            return MessagesDT.InsertMessage(DBInfo, Model);
        }
        public long InsertMessageDetail(DBInfoModel DBInfo, DA_MessagesDetailsModel Model)
        {
            return MessagesDT.InsertMessageDetail(DBInfo, Model);
        }
        public long InsertDaCustomerMessage(DBInfoModel DBInfo, DA_CustomerMessagesModel Model)
        {
            long customerMessageId = MessagesDT.InsertDaCustomerMessage(DBInfo, Model);
            SendEmailsForCustomerMessage(DBInfo, Model);
            return customerMessageId;
        }

        public long UpdateMainMessage(DBInfoModel DBInfo, DA_MainMessagesModel Model)
        {
            return MessagesDT.UpdateMainMessage(DBInfo, Model);
        }
        public long UpdateMessage(DBInfoModel DBInfo, DA_MessagesModel Model)
        {
            return MessagesDT.UpdateMessage(DBInfo, Model);
        }
        public long UpdateMessageDetail(DBInfoModel DBInfo, DA_MessagesDetailsModel Model)
        {
            return MessagesDT.UpdateMessageDetail(DBInfo, Model);
        }

        public long DeleteMainMessage(DBInfoModel dbInfo, long Id)
        {
            return MessagesDT.DeleteMainMessage(dbInfo, Id);
        }
        public long DeleteMessage(DBInfoModel dbInfo, long Id)
        {
            return MessagesDT.DeleteMessage(dbInfo, Id);
        }
        public long DeleteMessageDetail(DBInfoModel dbInfo, long Id)
        {
            return MessagesDT.DeleteMessageDetail(dbInfo, Id);
        }

        public void CreateMessageOnOrderCreate(DBInfoModel DBInfo, long OrderId, long CustomerId, long StaffId)
        {
            List<DA_MessagesDetailsModel> messageDetailsForCreate = MessagesDT.GetOnCreateMessageDetails(DBInfo);
            if (messageDetailsForCreate.Count != 0)
            {
                foreach(DA_MessagesDetailsModel messageDetail in messageDetailsForCreate)
                {
                    DA_MessagesModel message = MessagesDT.GetMessageById(DBInfo, messageDetail.HeaderId);
                    DA_MainMessagesModel mainMessage = MessagesDT.GetMainMessageById(DBInfo, message.MainDAMessagesID);
                    DA_CustomerMessagesModel customerMessage = CreateCustomerMessage(mainMessage, message, messageDetail, null, OrderId, CustomerId, StaffId);
                    long customerMessageId = MessagesDT.InsertDaCustomerMessage(DBInfo, customerMessage);
                    SendEmailsForCustomerMessage(DBInfo, customerMessage);
                }
            }
            List<DA_MessagesModel> messagesForCreate = MessagesDT.GetOnCreateMessages(DBInfo);
            if (messagesForCreate.Count != 0)
            {
                foreach (DA_MessagesModel message in messagesForCreate)
                {
                    DA_MainMessagesModel mainMessage = MessagesDT.GetMainMessageById(DBInfo, message.MainDAMessagesID);
                    DA_CustomerMessagesModel customerMessage = CreateCustomerMessage(mainMessage, message, null, null, OrderId, CustomerId, StaffId);
                    long customerMessageId = MessagesDT.InsertDaCustomerMessage(DBInfo, customerMessage);
                    SendEmailsForCustomerMessage(DBInfo, customerMessage);
                }
            }
            return;
        }
        public void CreateMessageOnOrderUpdate(DBInfoModel DBInfo, long OrderId, long CustomerId, long StaffId)
        {
            List<DA_MessagesDetailsModel> messageDetailsForUpdate = MessagesDT.GetOnUpdateMessageDetails(DBInfo);
            if (messageDetailsForUpdate.Count != 0)
            {
                foreach (DA_MessagesDetailsModel messageDetail in messageDetailsForUpdate)
                {
                    DA_MessagesModel message = MessagesDT.GetMessageById(DBInfo, messageDetail.HeaderId);
                    DA_MainMessagesModel mainMessage = MessagesDT.GetMainMessageById(DBInfo, message.MainDAMessagesID);
                    DA_CustomerMessagesModel customerMessage = CreateCustomerMessage(mainMessage, message, messageDetail, null, OrderId, CustomerId, StaffId);
                    long customerMessageId = MessagesDT.InsertDaCustomerMessage(DBInfo, customerMessage);
                    SendEmailsForCustomerMessage(DBInfo, customerMessage);
                }
            }
            List<DA_MessagesModel> messagesForUpdate = MessagesDT.GetOnUpdateMessages(DBInfo);
            if (messagesForUpdate.Count != 0)
            {
                foreach (DA_MessagesModel message in messagesForUpdate)
                {
                    DA_MainMessagesModel mainMessage = MessagesDT.GetMainMessageById(DBInfo, message.MainDAMessagesID);
                    DA_CustomerMessagesModel customerMessage = CreateCustomerMessage(mainMessage, message, null, null, OrderId, CustomerId, StaffId);
                    long customerMessageId = MessagesDT.InsertDaCustomerMessage(DBInfo, customerMessage);
                    SendEmailsForCustomerMessage(DBInfo, customerMessage);
                }
            }
            return;
        }

        private DA_CustomerMessagesModel CreateCustomerMessage(DA_MainMessagesModel mainMessage, DA_MessagesModel message, DA_MessagesDetailsModel messageDetail, string messageText, long OrderId, long CustomerId, long StaffId)
        {
            DA_CustomerMessagesModel customerMessage = new DA_CustomerMessagesModel();
            customerMessage.MainDAMessageID = mainMessage.Id;
            customerMessage.MessageId = message.Id;
            if (messageDetail != null)
            {
                customerMessage.MessageDetailsId = messageDetail.Id;
            }
            customerMessage.MessageText = messageText;
            customerMessage.OrderId = OrderId;
            customerMessage.CustomerId = CustomerId;
            customerMessage.StaffId = StaffId;
            customerMessage.CreationDate = DateTime.Now;
            return customerMessage;
        }
        private void SendEmailsForCustomerMessage(DBInfoModel DBInfo, DA_CustomerMessagesModel customerMessage)
        {
            DACustomerModel customer = customersDT.GetCustomer(DBInfo, customerMessage.CustomerId);
            List<DA_AddressModel> addresses = addressesDT.getCustomerAddresses(DBInfo, customerMessage.CustomerId);
            DA_OrderModel order = ordersDT.GetOrderWithDetailsById(DBInfo, customerMessage.OrderId ?? 0);
            string emailBody = CreateEmailBody(customerMessage.MessageText, customer, addresses, order);
            //List<string> emailRecipients = new List<string>();
            DA_MainMessagesModel mainMessage = MessagesDT.GetMainMessageById(DBInfo, customerMessage.MainDAMessageID);
            if (mainMessage != null && mainMessage.Email != null)
            {
                string[] emailsMainMessage = mainMessage.Email.Split(';');
                foreach (string emailRecipient in emailsMainMessage)
                {
                    SendEmailFromCustomerMessage(DBInfo, emailRecipient, mainMessage.Description, emailBody);
                }
            }
            DA_MessagesModel message = MessagesDT.GetMessageById(DBInfo, customerMessage.MessageId);
            if (message != null && message.Email != null)
            {
                string[] emailsMessage = message.Email.Split(';');
                foreach (string emailRecipient in emailsMessage)
                {
                    SendEmailFromCustomerMessage(DBInfo, emailRecipient, message.Description, emailBody);
                }
            }
            DA_MessagesDetailsModel messageDetail = MessagesDT.GetMessageDetailById(DBInfo, customerMessage.MessageDetailsId ?? 0);
            if (messageDetail != null && messageDetail.Email != null)
            {
                string[] emailsMessageDetail = messageDetail.Email.Split(';');
                foreach (string emailRecipient in emailsMessageDetail)
                {
                    SendEmailFromCustomerMessage(DBInfo, emailRecipient, messageDetail.Description, emailBody);
                }
            }
            return;
        }
        private string CreateEmailBody(string customerMessageText, DACustomerModel customer, List<DA_AddressModel> addresses, DA_OrderModel order)
        {
            string body = "";
            body += customerMessageText;
            body += "<br>";
            string customerText = customer.ToString();
            body += "<br>";
            body += customerText;
            body += "<br>";
            foreach (DA_AddressModel address in addresses)
            {
                string addressText = address.ToString();
                body += "<br>";
                body += addressText;
            }
            body += "<br>";
            if (order != null)
            {
                string orderText = order.ToString();
                body += "<br>";
                body += orderText;
                if (order.Details != null && order.Details.Count > 0)
                {
                    foreach (DA_OrderDetails orderDetail in order.Details)
                    {
                        string orderDetailText = orderDetail.ToString();
                        body += "<br>";
                        body += "&nbsp;&nbsp;&nbsp;" + orderDetailText;
                        if (orderDetail.Extras != null && orderDetail.Extras.Count > 0)
                        {
                            foreach (DA_OrderDetailsExtrasModel orderDetailExtra in orderDetail.Extras)
                            {
                                string orderDetailExtraText = orderDetailExtra.ToString();
                                body += "<br>";
                                body += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + orderDetailExtraText;
                            }
                        }
                    }
                }
            }
            body += "<br>";
            return body;
        }
        private void SendEmailFromCustomerMessage(DBInfoModel DBInfo, string email, string subject, string body)
        {
            EmailSendModel emailModel = new EmailSendModel();
            emailModel.To = new List<string>();
            emailModel.To.Add(email);
            emailModel.Subject = subject;
            emailModel.Body = body;
            try
            {
                emailTasks.SendEmail(DBInfo, emailModel);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to send email: " + ex.ToString());
            }
            return;
        }

    }
}
