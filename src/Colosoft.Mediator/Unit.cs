using System;
using System.Threading.Tasks;

namespace Colosoft.Mediator
{
#pragma warning disable S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
    public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
#pragma warning restore S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
    {
        private static readonly Unit InnerValue = default;

        public static ref readonly Unit Value => ref InnerValue;

        public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(InnerValue);

        public int CompareTo(Unit other) => 0;

        int IComparable.CompareTo(object obj) => 0;

        public override int GetHashCode() => 0;

        public bool Equals(Unit other) => true;

        public override bool Equals(object obj) => obj is Unit;

#pragma warning disable IDE0060 // Remove unused parameter
        public static bool operator ==(Unit first, Unit second) => true;

        public static bool operator !=(Unit first, Unit second) => false;
#pragma warning restore IDE0060 // Remove unused parameter

        public override string ToString() => "()";
    }
}