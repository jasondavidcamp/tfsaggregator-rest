using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Aggregator.Core.Context;
using Aggregator.Core.Extensions;
using Aggregator.Core.Interfaces;

namespace Aggregator.Core.Facade
{
    public class FieldCollectionWrapper : IFieldCollection
    {
        private readonly WorkItemWrapper owner;

        private readonly IRuntimeContext context;

        public FieldCollectionWrapper(WorkItemWrapper owner, IRuntimeContext context)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.context = context;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S3237:\"value\" parameters should be used", Justification = "Available for mock testing only")]
        public IField this[string name]
        {
            get
            {
                return this.ApplyDoubleFix(this.owner.GetOrCreateField(name));
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            set
            {
                throw new InvalidOperationException("Only used for mocking from unit tests");
            }
        }

        public IEnumerator<IField> GetEnumerator()
        {
            return this.owner
                .GetFields()
                .OrderBy(field => field.ReferenceName, StringComparer.OrdinalIgnoreCase)
                .Select(this.ApplyDoubleFix)
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private IField ApplyDoubleFix(WorkItemFieldState field)
        {
            IFieldExposed wrappedField = new FieldWrapper(field, this.owner);
            wrappedField = new DoubleFixFieldDecorator(wrappedField, this.context);

            if (this.context.Settings?.Debug == true)
            {
                wrappedField = new FieldValueValidationDecorator(wrappedField, this.context);
            }

            return wrappedField;
        }
    }
}
