// *********************************************************************
// Created by: Latebound Constant Generator 1.2019.6.2 for XrmToolBox
// Author    : Jonas Rapp http://twitter.com/rappen
// Repo      : https://github.com/rappen/LateboundConstantGenerator
// Source Org: https://mmdev.crm.dynamics.com/
// Filename  : C:\Users\user\Desktop\Display Name\ErrorLog.cs
// Created   : 2019-11-02 18:25:18
// *********************************************************************

namespace XrmExtensions
{
    public static class ErrorLog
    {
        public const string EntityName = "ims_errorlog";
        public const string EntityCollectionName = "ims_errorlogs";
        public const string PrimaryKey = "ims_errorlogid";
        public const string PrimaryName = "ims_name";
        public const string CreatedBy = "createdby";
        public const string CreatedByDelegate = "createdonbehalfby";
        public const string CreatedOn = "createdon";
        public const string ErrorDetails = "ims_errordetails";
        public const string ImportSequenceNumber = "importsequencenumber";
        public const string ModifiedByDelegate = "modifiedonbehalfby";
        public const string ModifiedBy = "modifiedby";
        public const string ModifiedOn = "modifiedon";
        public const string Owner = "ownerid";
        public const string owneridtype = "owneridtype";
        public const string OwningBusinessUnit = "owningbusinessunit";
        public const string RecordCreatedOn = "overriddencreatedon";
        public const string Status = "statecode";
        public const string StatusReason = "statuscode";
        public const string TimeZoneRuleVersionNumber = "timezoneruleversionnumber";
        public const string UTCConversionTimeZoneCode = "utcconversiontimezonecode";
        public const string VersionNumber = "versionnumber";
        public enum owneridtype_OptionSet
        {
        }
        public enum Status_OptionSet
        {
            Active = 0,
            Inactive = 1
        }
        public enum StatusReason_OptionSet
        {
            Active = 1,
            Inactive = 2
        }
    }
}
