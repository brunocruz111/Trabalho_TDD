using System;

namespace Gamification.Domain.Exceptions
{
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }

    public class IneligibleException : DomainException
    {
        public IneligibleException(string message) : base(message) { }
    }

    public class AlreadyAwardedException : DomainException
    {
        public AlreadyAwardedException(string message) : base(message) { }
    }

    public class AtomicPersistenceException : DomainException
    {
        public AtomicPersistenceException(string message, Exception? inner = null) : base(message, inner) { }
    }
}