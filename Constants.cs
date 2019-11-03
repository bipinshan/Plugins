using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmExtensions
{
    public class Constants
    {
        public enum NewRecord { ValueIsNull, ValueIsNotNull, ValueIsDiffFromOldValue};

        #region Configurations
        public const string ValidationError_MandatoryCheck = "StagingValError_MandatoryCheck";
        public const string ValidationError_MaxLengthCheck = "StagingValError_MaxLengthCheck";
        public const string ValidationError_LookupUnresolved = "StagingValError_LookupUnresolved";
        public const string ErrorInSettingValue = "Staging_ErrorInSettingValue";
        public const string CreateErrorLog = "Staging_CreateErrorLog";
        #endregion
    }
}
