#if NET_2_0
using System.Collections.Generic;
#endif
using log4net.Util;
using NUnit.Framework;

namespace log4net.Tests.Util
{
    [TestFixture]
    public class TypeToLoggerNameConverterTest
    {
        [Test]
        public void correct_value_is_returned_for_non_generics() { 
            Assert.That(
                TypeToLoggerNameConverter.GetLoggerName(typeof(System.Collections.IEnumerable)),
                Is.EqualTo("System.Collections.IEnumerable"));

            Assert.That(
                TypeToLoggerNameConverter.GetLoggerName(typeof(TestFixtureAttribute)),
                Is.EqualTo("NUnit.Framework.TestFixtureAttribute"));
        }

#if NET_2_0
        [Test]
        public void correct_value_is_returned_for_generics() { 
            Assert.That(
                TypeToLoggerNameConverter.GetLoggerName(typeof(Dictionary<string, IEnumerable<string>>)),
                Is.EqualTo("System.Collections.Generic.Dictionary"));
        }
#endif
    }
}