using System.Linq;
using NUnit.Framework;

namespace Zenject.Tests
{
    public class BindIdsTests
    {
        [Test]
        public void Test_Unique()
        {
            var values = BindIdDict.Values;
            Assert.AreEqual(values.Count, values.Distinct().Count());
        }
    }
}