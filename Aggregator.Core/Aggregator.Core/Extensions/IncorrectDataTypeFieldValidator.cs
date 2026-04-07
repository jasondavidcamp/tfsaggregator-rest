using Aggregator.Core.Context;

using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Aggregator.Core.Extensions
{
    internal class IncorrectDataTypeFieldValidator : BaseFieldValueValidator
    {
        internal IncorrectDataTypeFieldValidator(IRuntimeContext context)
            : base(context)
        {
        }

        public override bool ValidateFieldValue(Field field, object value)
        {
            if (value != null && value.GetType() != field.FieldDefinition.SystemType)
            {
                this.Logger.FieldValidationFailedInvalidDataType(
                    0,
                    field.ReferenceName,
                    field.FieldDefinition.SystemType,
                    value.GetType(),
                    value);

                return false;
            }

            return true;
        }
    }
}
