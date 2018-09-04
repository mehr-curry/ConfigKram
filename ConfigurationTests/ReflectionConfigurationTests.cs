using System;
using Configuration;
using Xunit;

namespace ConfigurationTests
{
    public class ReflectionConfigurationTests
    {
        [Fact]
        public void LoadTest()
        {
            var configuration = new ReflectionConfiguration("ConfigKram.config");
            var test = new TestConfigurationObject{TestString = "", TestInt = 0, TestNullableInt = 1};
            configuration.Load(test);
            Assert.Equal("default", test.TestString);
            Assert.Equal(10, test.TestInt);
            Assert.Equal(null, test.TestNullableInt);
        }
    }
}