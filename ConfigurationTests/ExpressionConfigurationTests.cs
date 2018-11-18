using Configuration;
using Xunit;

namespace ConfigurationTests
{
    public class ExpressionConfigurationTests
    {
        [Fact]
        public void LoadTest()
        {
            var configuration = new ExpressionAdapter(new DotNetConfigurationStore() { FileName = "ConfigKramLoadTest.config" });
            var test = configuration.Load<TestConfigurationObject>();
            Assert.Equal("default", test.TestString);
            Assert.Equal(10, test.TestInt);
            Assert.Null(test.TestNullableInt);
        }
        [Fact]
        public void LoadIntoTest()
        {
            var configuration = new ExpressionAdapter(new DotNetConfigurationStore() {FileName = "ConfigKramLoadTest.config"});
            var test = new TestConfigurationObject {TestString = "", TestInt = 0, TestNullableInt = 1};
            configuration.LoadInto(test);
            Assert.Equal("default", test.TestString);
            Assert.Equal(10, test.TestInt);
            Assert.Null(test.TestNullableInt);
        }
        
        [Fact]
        public void SaveTest()
        {
            var configuration = new ExpressionAdapter(new DotNetConfigurationStore() {FileName = "ConfigKramSaveTest.config"});
            var test = new TestConfigurationObject {TestString = nameof(SaveTest), TestInt = -1, TestNullableInt = null};
            configuration.Save(test);
            
            test = new TestConfigurationObject();
            configuration.LoadInto(test);
            
            Assert.Equal(nameof(SaveTest), test.TestString);
            Assert.Equal(-1, test.TestInt);
            Assert.Null(test.TestNullableInt);
        }
    }
}