using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Repository;
using UserModel;
using BusinessLayer.Common;

namespace BusinessLayer
{
    public interface IMailBusiness
    {
        AddUpdateDeleteResponseModel ComposeMail();
        AddUpdateDeleteResponseModel FetchSentMailList(SearchSortPagingModel model);
        AddUpdateDeleteResponseModel DeleteSentMailById(ComposeMailModel model);
    }
    public class MailBusiness: IMailBusiness
    {
        public AddUpdateDeleteResponseModel ComposeMail()
        {
            string filePath = string.Empty;
            using (TaleemIndiaDBEntities db = new TaleemIndiaDBEntities())
            {
                ComposeMailModel composeMailModel = new ComposeMailModel();
                string jsondata = HttpContext.Current.Request["jsondata"].ToString();
                composeMailModel = JsonConvert.DeserializeObject<ComposeMailModel>(jsondata);
                var httpRequest = HttpContext.Current.Request;
                HttpFileCollection files = httpRequest.Files;

                List<string> mailsTo = composeMailModel.MailTo.Split(',').ToList();
                ComposeMail composeMail = new ComposeMail();
                composeMail.MailTo = composeMailModel.MailTo;
                composeMail.Subject = composeMailModel.Subject;
                composeMail.Message = httpRequest.Unvalidated.Form["Message"];
                composeMail.EntryDate = DateTime.Now;
                if (files.Count > 0)
                {
                    composeMail.IsAttachment = true;
                }
                if (files.Count > 0)
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        HttpPostedFileBase pfb = new HttpPostedFileWrapper(files[i]);
                        if (pfb != null && pfb.ContentLength > 0)
                        {
                            var fileName = Path.GetFileName(pfb.FileName);
                            filePath = Path.Combine(HttpContext.Current.Server.MapPath("~/Uplaod-Documents/Mail/"), fileName);
                            pfb.SaveAs(filePath);
                            //composeMail.AttachmentPath = "Uplaod-Documents/Mail/" + fileName;
                        }
                    }
                }
                MailHelper _mailHelper = new MailHelper();
                bool isSent = _mailHelper.ComposeOrResend(mailsTo, composeMailModel.Subject, composeMail.Message, files);
                if (isSent)
                {
                    db.ComposeMails.Add(composeMail);
                    if (db.SaveChanges() > 0)
                    {
                        return new AddUpdateDeleteResponseModel { Message = "Message sent successfully.", Status = true };
                    }
                    return new AddUpdateDeleteResponseModel { Message = "Message sent Successfully, but record not saved.", Status = false };
                }
                return new AddUpdateDeleteResponseModel { Message = "Message send Failed.", Status = false };
            }
        }
        public AddUpdateDeleteResponseModel FetchSentMailList(SearchSortPagingModel model)
        {
            long totalRecordCount = 0;
            ComposeMailModel composeMailModel = new ComposeMailModel();
            List<ComposeMailModel> composeMailModelList = new List<ComposeMailModel>();
            using (TaleemIndiaDBEntities db = new TaleemIndiaDBEntities())
            {
                var data = db.ComposeMails.ToList();
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        composeMailModel.ComposeMailId = item.ComposeMailId;
                        composeMailModel.MailTo = item.MailTo;
                        composeMailModel.Subject = item.Subject;
                        composeMailModel.Message = item.Message;
                        composeMailModel.IsAttachment = item.IsAttachment;
                        composeMailModel.IsRead = item.IsRead;
                        composeMailModel.EntryDate = item.EntryDate;
                        composeMailModelList.Add(composeMailModel);
                        composeMailModel = new ComposeMailModel();
                    }
                }
                var composeMailList = composeMailModelList.AsEnumerable();
                totalRecordCount = composeMailList.LongCount();

                #region filters 
                if (!string.IsNullOrEmpty(model.Filter) && !string.IsNullOrWhiteSpace(model.Filter))
                {
                    composeMailList = composeMailList.Where(x =>
                        (x.ComposeMailId != 0 && x.ComposeMailId.ToString().ToLower().Contains(model.Filter.ToLower())) ||
                        (x.MailTo != null && x.MailTo.ToLower().Contains(model.Filter.ToLower())) ||
                        (x.Subject != null && x.Subject.ToString().ToLower().Contains(model.Filter.ToLower())) ||
                        (x.Message != null && x.Message.ToString().ToLower().Contains(model.Filter.ToLower())) ||
                        (x.EntryDate != null && x.EntryDate.ToString().Contains(model.Filter.ToLower()))
                    );
                }
                #endregion

                //count of record after filter   
                totalRecordCount = composeMailList.LongCount();

                #region Sorting  
                switch (model.SortBy)
                {
                    case "ComposeMailId":
                        composeMailList = model.SortOrder.Equals("asc") ? composeMailList.OrderByDescending(p => p.ComposeMailId) : composeMailList.OrderBy(p => p.ComposeMailId);
                        break;
                    case "MailTo":
                        composeMailList = model.SortOrder.Equals("asc") ? composeMailList.OrderByDescending(p => p.MailTo) : composeMailList.OrderBy(p => p.MailTo);
                        break;
                    case "Subject":
                        composeMailList = model.SortOrder.Equals("asc") ? composeMailList.OrderByDescending(p => p.Subject) : composeMailList.OrderBy(p => p.Subject);
                        break;
                    case "Message":
                        composeMailList = model.SortOrder.Equals("asc") ? composeMailList.OrderByDescending(p => p.Message) : composeMailList.OrderBy(p => p.Message);
                        break;
                    case "EntryDate":
                        composeMailList = model.SortOrder.Equals("asc") ? composeMailList.OrderByDescending(p => p.EntryDate) : composeMailList.OrderBy(p => p.EntryDate);
                        break;
                    default:
                        composeMailList = composeMailList.OrderByDescending(p => p.ComposeMailId);
                        break;
                }
                composeMailList = composeMailList.Skip(model.PageIndexSize * model.PageSize).Take(model.PageSize).ToList();
                return new AddUpdateDeleteResponseModel { Data = composeMailList, TotalCount = totalRecordCount, Status = true };
                #endregion
            }
        }
        public AddUpdateDeleteResponseModel DeleteSentMailById(ComposeMailModel model)
        {
            ResponseModel responseModel = new ResponseModel();
            using (TaleemIndiaDBEntities db = new TaleemIndiaDBEntities())
            {
                var data = db.ComposeMails.Where(x => x.ComposeMailId == model.ComposeMailId).FirstOrDefault();
                if (data == null)
                {
                    return new AddUpdateDeleteResponseModel { Message = "Oops! ! Something is wrong, try again.", Status = false };
                }
                else
                {
                    db.ComposeMails.Remove(data);
                    db.SaveChanges();
                    return new AddUpdateDeleteResponseModel { Message = "Mail deleted successfully.", Status = true };
                }
            }
        }
    }
}