// *********************************************************************
// Created by: Latebound Constant Generator 1.2019.6.2 for XrmToolBox
// Author    : Jonas Rapp http://twitter.com/rappen
// Repo      : https://github.com/rappen/LateboundConstantGenerator
// Source Org: https://mmdev.crm.dynamics.com/
// Filename  : C:\Users\user\Desktop\xx\Property.cs
// Created   : 2019-11-01 18:07:25
// *********************************************************************

namespace XrmExtensions
{
    public static class Property
    {
        public const string EntityName = "ims_property";
        public const string EntityCollectionName = "ims_properties";
        public const string PrimaryKey = "ims_propertyid";
        public const string PrimaryName = "ims_name";
        public const string Address = "ims_address";
        public const string AddressLine1 = "ims_addressline1";
        public const string AddressLine2 = "ims_addressline2";
        public const string City = "ims_city";
        public const string CreatedBy = "createdby";
        public const string CreatedByDelegate = "createdonbehalfby";
        public const string CreatedOn = "createdon";
        public const string createdbyname = "createdbyname";
        public const string createdbyyominame = "createdbyyominame";
        public const string createdonbehalfbyname = "createdonbehalfbyname";
        public const string createdonbehalfbyyominame = "createdonbehalfbyyominame";
        public const string ImportSequenceNumber = "importsequencenumber";
        public const string ims_lastlomortgagedbyname = "ims_lastlomortgagedbyname";
        public const string ims_lastlomortgagedbyyominame = "ims_lastlomortgagedbyyominame";
        public const string ims_mortgagestatusname = "ims_mortgagestatusname";
        public const string LastLOMortgagedBy = "ims_lastlomortgagedby";
        public const string ModifiedByDelegate = "modifiedonbehalfby";
        public const string ModifiedBy = "modifiedby";
        public const string ModifiedOn = "modifiedon";
        public const string modifiedbyname = "modifiedbyname";
        public const string modifiedbyyominame = "modifiedbyyominame";
        public const string modifiedonbehalfbyname = "modifiedonbehalfbyname";
        public const string modifiedonbehalfbyyominame = "modifiedonbehalfbyyominame";
        public const string MortgageStatus = "ims_mortgagestatus";
        public const string Owner = "ownerid";
        public const string owneridname = "owneridname";
        public const string owneridtype = "owneridtype";
        public const string owneridyominame = "owneridyominame";
        public const string OwningBusinessUnit = "owningbusinessunit";
        public const string OwningTeam = "owningteam";
        public const string OwningUser = "owninguser";
        public const string RecordCreatedOn = "overriddencreatedon";
        public const string State = "ims_state";
        public const string statecodename = "statecodename";
        public const string Status = "statecode";
        public const string StatusReason = "statuscode";
        public const string statuscodename = "statuscodename";
        public const string TimeZoneRuleVersionNumber = "timezoneruleversionnumber";
        public const string UTCConversionTimeZoneCode = "utcconversiontimezonecode";
        public const string VersionNumber = "versionnumber";
        public const string Zip = "ims_zip";
        public enum MortgageStatus_OptionSet
        {
            UnderMortgage = 176390000,
            AppliedforMortgage = 176390001
        }
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
