using System;
using System.Collections.Generic;
using System.Linq;
using Configuration.EF;
using Moq;
using Xunit;

namespace Configuration.EfTests
{
    public class EfConfigurationStoreTests
    {
        [Fact]
        public void EfConfigurationStoreGetValuesTest()
        {
            var moqDbConfiguration = new Mock<IConfigurationDbContext>();
            moqDbConfiguration.SetupGet(o => o.ConfigurationEntries).Returns(
                new List<ConfigurationEntry>
                {
                    new ConfigurationEntry{Name="TestString", Value="FromDb", Section = "UnitTest"},
                    new ConfigurationEntry{Name="TestInt", Value="2", Section = "UnitTest"},
                    new ConfigurationEntry{Name="TestNullableInt", Section = "UnitTest"},
                });
            
            var store = new DbContextConfigurationStore(moqDbConfiguration.Object);
            
            var result = store.GetValues("UnitTest");
            
            Assert.Contains(result, i => i.Key == "TestString" && (string)i.Value == "FromDb");
            Assert.Contains(result, i => i.Key == "TestInt" && (string)i.Value == "2");
            Assert.Contains(result, i => i.Key == "TestNullableInt" && (string)i.Value == null);

        }
    }
}