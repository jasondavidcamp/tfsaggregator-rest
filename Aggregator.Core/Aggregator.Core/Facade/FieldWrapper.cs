using System;

using Aggregator.Core.Interfaces;

using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Aggregator.Core.Facade
{
    internal class FieldWrapper : IFieldExposed
    {
        private readonly WorkItemFieldState field;

        private readonly WorkItemWrapper owner;

        public FieldWrapper(WorkItemFieldState field, WorkItemWrapper owner)
        {
            this.field = field ?? throw new ArgumentNullException(nameof(field));
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public string Name => this.field.Name;

        public string ReferenceName => this.field.ReferenceName;

        public object Value
        {
            get
            {
                return this.field.CurrentValue;
            }

            set
            {
                this.owner.SetFieldValue(this.field.ReferenceName, value);
            }
        }

        public Field TfsField => null;

        public FieldStatus Status => this.field.Status;

        public object OriginalValue => this.field.OriginalValue;

        public Type DataType => this.field.DataType;
    }
}
