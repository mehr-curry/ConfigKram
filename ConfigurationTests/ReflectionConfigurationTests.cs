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
            var configuration = new ReflectionAdapter(new DotNetConfigurationStore() {FileName = "ConfigKramLoadTest.config"});
            var test = new TestConfigurationObject {TestString = "", TestInt = 0, TestNullableInt = 1};
            configuration.Load(test);
            Assert.Equal("default", test.TestString);
            Assert.Equal(10, test.TestInt);
            Assert.Equal(null, test.TestNullableInt);
        }
        
        [Fact]
        public void SaveTest()
        {
            var configuration = new ReflectionAdapter(new DotNetConfigurationStore() {FileName = "ConfigKramSaveTest.config"});
            var test = new TestConfigurationObject {TestString = nameof(SaveTest), TestInt = -1, TestNullableInt = null};
            configuration.Save(test);
            
            test = new TestConfigurationObject();
            configuration.Load(test);
            
            Assert.Equal(nameof(SaveTest), test.TestString);
            Assert.Equal(-1, test.TestInt);
            Assert.Equal(null, test.TestNullableInt);
        }
    }
}