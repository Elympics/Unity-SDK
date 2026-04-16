using System;
using System.Linq;
using NUnit.Framework;

#nullable enable

namespace Elympics.Tests
{
    internal static class CustomAsserts
    {
        public static Exception AssertThrowsAggregated(Type exceptionType, TestDelegate f, bool shouldAllowSubclasses = true, bool shouldBeTheOnlyException = true)
        {
            var aggregateException = Assert.Throws<AggregateException>(f);
            if (shouldBeTheOnlyException)
                Assert.That(aggregateException.InnerExceptions, Has.Count.EqualTo(1));
            else
                Assert.That(aggregateException.InnerExceptions, Has.Some.TypeOf(exceptionType));
            var exception = aggregateException.InnerExceptions.First(e => shouldBeTheOnlyException
                || (shouldAllowSubclasses ? e.GetType().IsSubclassOf(exceptionType) : e.GetType() == exceptionType));
            Assert.That(exception, shouldAllowSubclasses ? Is.InstanceOf(exceptionType) : Is.TypeOf(exceptionType));
            return exception!;
        }

        public static T AssertThrowsAggregated<T>(TestDelegate f, bool shouldBeTheOnlyException = true)
            where T : Exception =>
            (T)AssertThrowsAggregated(typeof(T), f, shouldBeTheOnlyException);
    }
}
