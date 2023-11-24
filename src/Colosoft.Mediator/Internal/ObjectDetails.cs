using System;
using System.Collections.Generic;

namespace Colosoft.Mediator.Internal
{
    internal class ObjectDetails : IComparer<ObjectDetails>
    {
        public string Name { get; }

        public string AssemblyName { get; }

        public string Location { get; }

        public object Value { get; }

        public Type Type { get; }

        public bool IsOverridden { get; set; }

        public ObjectDetails(object value)
        {
            this.Value = value;
            this.Type = this.Value.GetType();
            var exceptionHandlerType = value.GetType();

            this.Name = exceptionHandlerType.Name;
            this.AssemblyName = exceptionHandlerType.Assembly.GetName().Name;
            this.Location = exceptionHandlerType.Namespace?.Replace($"{this.AssemblyName}.", string.Empty);
        }

        public int Compare(ObjectDetails x, ObjectDetails y)
        {
            if (x == null)
            {
                return 1;
            }

            if (y == null)
            {
                return -1;
            }

            return this.CompareByAssembly(x, y) ?? this.CompareByNamespace(x, y) ?? this.CompareByLocation(x, y);
        }

        private int? CompareByAssembly(ObjectDetails x, ObjectDetails y)
        {
            if (x.AssemblyName == this.AssemblyName && y.AssemblyName != this.AssemblyName)
            {
                return -1;
            }

            if (x.AssemblyName != this.AssemblyName && y.AssemblyName == this.AssemblyName)
            {
                return 1;
            }

            if (x.AssemblyName != this.AssemblyName && y.AssemblyName != this.AssemblyName)
            {
                return 0;
            }

            return null;
        }

        private int? CompareByNamespace(ObjectDetails x, ObjectDetails y)
        {
            if (this.Location is null || x.Location is null || y.Location is null)
            {
                return 0;
            }

            if (x.Location.StartsWith(this.Location, StringComparison.Ordinal) && !y.Location.StartsWith(this.Location, StringComparison.Ordinal))
            {
                return -1;
            }

            if (!x.Location.StartsWith(this.Location, StringComparison.Ordinal) && y.Location.StartsWith(this.Location, StringComparison.Ordinal))
            {
                return 1;
            }

            if (x.Location.StartsWith(this.Location, StringComparison.Ordinal) && y.Location.StartsWith(this.Location, StringComparison.Ordinal))
            {
                return 0;
            }

            return null;
        }

        private int CompareByLocation(ObjectDetails x, ObjectDetails y)
        {
            if (this.Location is null || x.Location is null || y.Location is null)
            {
                return 0;
            }

            if (this.Location.StartsWith(x.Location, StringComparison.Ordinal) && !this.Location.StartsWith(y.Location, StringComparison.Ordinal))
            {
                return -1;
            }

            if (!this.Location.StartsWith(x.Location, StringComparison.Ordinal) && this.Location.StartsWith(y.Location, StringComparison.Ordinal))
            {
                return 1;
            }

            if (x.Location.Length > y.Location.Length)
            {
                return -1;
            }

            if (x.Location.Length < y.Location.Length)
            {
                return 1;
            }

            return 0;
        }
    }
}